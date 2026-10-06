/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Installation portable, téléchargements et modèles.
EN: Portable installation, downloads and models.

FR : Les commentaires structurants sont bilingues. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
EN: Structural comments are bilingual. API, class and protocol names remain in their
    technical form to keep the code readable and compatible.
*/

using System.Diagnostics;
using System.Net;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OpenCodeLocalAI;

public sealed class PortableInstaller
{
    private readonly AppSettings _s;
    private readonly Action<string,string> _log;
    private readonly HttpClient _http = CreateHttpClient();

    private int _lastProgressValue = -1;
    private string _lastProgressText = "";
    private DateTime _lastProgressUtc = DateTime.MinValue;

    public event Action<int,string>? ProgressChanged;

    public PortableInstaller(AppSettings settings, Action<string,string> log)
    {
        _s = settings;
        _log = log;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/154.0.0.0 Safari/537.36 DreamRaster/1.0");

        _http.DefaultRequestHeaders.Accept.ParseAdd("*/*");
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.None,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 20,
            EnableMultipleHttp2Connections = true,
            MaxConnectionsPerServer = 16,
            ConnectTimeout = TimeSpan.FromSeconds(30),
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        };

        return new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan,

            // Autorise la montée automatique vers HTTP/2 puis HTTP/3
            // quand le serveur et Windows/MsQuic le permettent.
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };
    }

    private void Progress(int value, string text)
    {
        value = Math.Clamp(value, 0, 100);
        var now = DateTime.UtcNow;

        var samePercent = value == _lastProgressValue;
        var tooSoon = (now - _lastProgressUtc) < TimeSpan.FromMilliseconds(180);

        if (value != 100 && samePercent && tooSoon)
            return;

        if (value != 100 &&
            samePercent &&
            text.Equals(_lastProgressText, StringComparison.Ordinal))
            return;

        _lastProgressValue = value;
        _lastProgressText = text;
        _lastProgressUtc = now;

        ProgressChanged?.Invoke(value, text);
    }

    public async Task InstallAllAsync(CancellationToken ct)
    {
        PortablePaths.EnsureLayout();

        if (!File.Exists(PortablePaths.Resolve(_s.OpenCodeExe)))
            await InstallOpenCodeAsync(ct);
        else
            _log("Install", "OpenCode portable déjà présent : conservé.");

        if (!File.Exists(PortablePaths.Resolve(_s.OllamaExe)))
            await InstallOllamaAsync(ct);
        else
            _log("Install", "Ollama portable déjà présent : conservé.");

        if (!File.Exists(PortablePaths.Resolve(_s.ComfyPython)) ||
            !File.Exists(PortablePaths.Resolve(_s.ComfyMain)))
            await InstallComfyAsync(ct);
        else
            _log("Install", "ComfyUI portable déjà présent : conservé.");

        // FR : Qwen3-VL est facultatif. Il sert aux fonctions de vision Ollama/OpenCode.
        // EN: Qwen3-VL is optional. It is only needed for Ollama/OpenCode vision features.
        if (_s.InstallVisionModel)
        {
            var manifest = Path.Combine(
                PortablePaths.Resolve(_s.OllamaModels),
                "manifests", "registry.ollama.ai", "library", "qwen3-vl", "8b");

            if (!File.Exists(manifest))
                await InstallQwenAsync(ct);
            else
                _log("Install", $"{_s.VisionModel} déjà présent : conservé.");
        }
        else
        {
            _log(
                "Install",
                "Qwen3-VL optionnel : téléchargement ignoré (désactivé dans Configuration).");
        }

        // "Installer / réparer tout" vérifie les fichiers existants par SHA256
        // avant de décider s'il faut télécharger quoi que ce soit.
        await InstallFluxModelsAsync(ct);
        await InstallVideoModelsAsync(ct);

        if (PortablePaths.GetFixedWebView2RuntimePath() is null)
            await InstallWebView2FixedAsync(ct);
        else
            _log("Install", "WebView2 Fixed Version portable déjà présent : conservé.");

        Progress(100, "Installation portable terminée.");
    }

    public async Task InstallOpenCodeAsync(CancellationToken ct)
    {
        Progress(2, "Téléchargement OpenCode…");
        var archive = Path.Combine(PortablePaths.DownloadsDir, "opencode.zip");
        await DownloadAsync(_s.OpenCodeZipUrl, archive, 2, 15, ct);

        var dest = Path.Combine(PortablePaths.Root, "bin", "opencode");
        await ResetDirectoryAsync(dest, ct);

        try
        {
            await ExtractZipAsync(archive, dest, 15, 16, ct);
        }
        catch (OperationCanceledException)
        {
            await TryDeleteDirectoryAsync(dest);
            throw;
        }

        var exe = Directory.GetFiles(dest, "opencode.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (exe is null) throw new InvalidOperationException("opencode.exe absent après extraction.");

        var expected = PortablePaths.Resolve(_s.OpenCodeExe);
        Directory.CreateDirectory(Path.GetDirectoryName(expected)!);
        if (!exe.Equals(expected, StringComparison.OrdinalIgnoreCase))
            File.Copy(exe, expected, true);

        _log("Install", "OpenCode portable installé : " + expected);
    }

    public async Task InstallOllamaAsync(CancellationToken ct)
    {
        Progress(16, "Téléchargement Ollama portable…");
        var archive = Path.Combine(PortablePaths.DownloadsDir, "ollama.zip");
        await DownloadAsync(_s.OllamaZipUrl, archive, 16, 30, ct);

        var dest = Path.Combine(PortablePaths.Root, "bin", "ollama");
        await ResetDirectoryAsync(dest, ct);

        try
        {
            await ExtractZipAsync(archive, dest, 30, 31, ct);
        }
        catch (OperationCanceledException)
        {
            await TryDeleteDirectoryAsync(dest);
            throw;
        }

        if (!File.Exists(PortablePaths.Resolve(_s.OllamaExe)))
            throw new InvalidOperationException("ollama.exe absent après extraction.");

        _log("Install", "Ollama portable installé.");
    }

    public async Task InstallComfyAsync(CancellationToken ct)
    {
        Progress(31, "Téléchargement ComfyUI portable…");
        var archivePath = Path.Combine(PortablePaths.DownloadsDir, "ComfyUI.7z");
        await DownloadAsync(_s.ComfyZipUrl, archivePath, 31, 52, ct);

        var dest = Path.Combine(PortablePaths.Root, "bin", "comfyui");
        await ResetDirectoryAsync(dest, ct);

        Progress(53, "Préparation de 7-Zip portable…");
        var sevenZip = Path.Combine(PortablePaths.RuntimeDir, "tools", "7zr.exe");
        if (!File.Exists(sevenZip))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(sevenZip)!);
            await DownloadAsync(
                _s.SevenZipUrl,
                sevenZip,
                53,
                54,
                ct);
        }

        if (!PortablePaths.IsInsidePack(sevenZip))
            throw new InvalidOperationException("7zr.exe hors du pack portable refusé.");

        Progress(55, "Extraction ComfyUI…");
        var psi = new ProcessStartInfo
        {
            FileName = sevenZip,
            Arguments = $"x \"{archivePath}\" -o\"{dest}\" -y",
            WorkingDirectory = Path.GetDirectoryName(sevenZip)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        using (var extract = Process.Start(psi)
               ?? throw new InvalidOperationException("Impossible de démarrer 7zr.exe."))
        {
            using var cancelExtraction = ct.Register(() =>
            {
                try
                {
                    if (!extract.HasExited)
                        extract.Kill(entireProcessTree: true);
                }
                catch { }
            });

            var stdoutTask = extract.StandardOutput.ReadToEndAsync();
            var stderrTask = extract.StandardError.ReadToEndAsync();

            try
            {
                await extract.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!extract.HasExited)
                        extract.Kill(entireProcessTree: true);
                }
                catch { }

                await TryDeleteDirectoryAsync(dest);
                throw;
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (extract.ExitCode != 0)
                throw new InvalidOperationException(
                    $"Extraction ComfyUI échouée (7zr code {extract.ExitCode}).\n{stderr}");

            var tail = string.Join(
                " | ",
                stdout.Split(
                        new[] { '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .TakeLast(3));

            _log(
                "7-Zip",
                string.IsNullOrWhiteSpace(tail)
                    ? "Extraction ComfyUI terminée."
                    : "Extraction terminée · " + tail);
        }

        if (!File.Exists(PortablePaths.Resolve(_s.ComfyPython)) ||
            !File.Exists(PortablePaths.Resolve(_s.ComfyMain)))
            throw new InvalidOperationException(
                "ComfyUI portable a été extrait mais Python/main.py sont introuvables.");

        _log("Install", "ComfyUI portable installé.");
    }

    public async Task InstallWebView2FixedAsync(CancellationToken ct)
    {
        if (_s.EnsureWebView2FixedDefaults())
        {
            SettingsStore.Save(_s);
            _log("Install", "Configuration WebView2 Fixed Version migrée vers les valeurs portables par défaut.");
        }
        var existing = PortablePaths.GetFixedWebView2RuntimePath();
        if (existing is not null)
        {
            _log("Install", "WebView2 Fixed Version portable déjà présent : " + existing);
            return;
        }

        if (string.IsNullOrWhiteSpace(_s.WebView2FixedArchiveUrl))
            throw new InvalidOperationException(
                "URL WebView2 Fixed Version non configurée.");

        if (string.IsNullOrWhiteSpace(_s.WebView2FixedVersion))
            throw new InvalidOperationException(
                "Version WebView2 Fixed Version non configurée.");

        var archiveName =
            $"Microsoft.WebView2.FixedVersionRuntime.{_s.WebView2FixedVersion}.x64.cab";
        var archive = Path.Combine(PortablePaths.DownloadsDir, archiveName);

        if (File.Exists(archive))
        {
            var cachedHash = await Sha256Async(archive, ct);
            if (!cachedHash.Equals(
                    _s.WebView2FixedSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                _log("Install !", "Archive WebView2 en cache invalide : suppression.");
                File.Delete(archive);
            }
            else
            {
                _log("Install", "Archive WebView2 Fixed Version déjà téléchargée : SHA256 OK.");
            }
        }

        if (!File.Exists(archive))
        {
            Progress(99, "Téléchargement WebView2 Fixed Version x64…");
            await DownloadAsync(
                _s.WebView2FixedArchiveUrl,
                archive,
                99,
                100,
                ct);
        }

        Progress(100, "Vérification SHA256 WebView2 Fixed Version…");
        var actualHash = await Sha256Async(archive, ct);
        if (!actualHash.Equals(
                _s.WebView2FixedSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            try { File.Delete(archive); } catch { }

            throw new InvalidOperationException(
                $"SHA256 invalide pour {archiveName}.\n" +
                $"Attendu : {_s.WebView2FixedSha256}\n" +
                $"Obtenu : {actualHash}");
        }

        _log("Install", "SHA256 OK : " + archiveName);

        var expandExe = Path.Combine(Environment.SystemDirectory, "expand.exe");
        if (!File.Exists(expandExe))
            throw new InvalidOperationException(
                "L'utilitaire Windows natif expand.exe est introuvable.");

        var temp = Path.Combine(
            PortablePaths.RuntimeDir,
            "webview2-fixed-extract");

        var destRoot = Path.Combine(
            PortablePaths.Root,
            "bin",
            "webview2-fixed");

        await ResetDirectoryAsync(temp, ct);

        try
        {
            Progress(100, "Extraction WebView2 Fixed Version…");

            var psi = new ProcessStartInfo
            {
                FileName = expandExe,
                Arguments = $"\"{archive}\" -F:* \"{temp}\"",
                WorkingDirectory = temp,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var extract = Process.Start(psi)
                ?? throw new InvalidOperationException(
                    "Impossible de démarrer expand.exe.");

            using var cancelExtraction = ct.Register(() =>
            {
                try
                {
                    if (!extract.HasExited)
                        extract.Kill(entireProcessTree: true);
                }
                catch { }
            });

            var stdoutTask = extract.StandardOutput.ReadToEndAsync();
            var stderrTask = extract.StandardError.ReadToEndAsync();

            await extract.WaitForExitAsync(ct);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (extract.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Extraction WebView2 échouée (expand.exe code {extract.ExitCode}).\n" +
                    stderr);
            }

            var runtimeExe = Directory.GetFiles(
                    temp,
                    "msedgewebview2.exe",
                    SearchOption.AllDirectories)
                .FirstOrDefault();

            if (runtimeExe is null)
                throw new InvalidOperationException(
                    "msedgewebview2.exe absent après extraction du runtime Fixed Version.");

            var sourceRuntime =
                Path.GetDirectoryName(runtimeExe)
                ?? throw new InvalidOperationException(
                    "Répertoire du runtime WebView2 invalide.");

            if (!PortablePaths.IsInsidePack(sourceRuntime))
                throw new InvalidOperationException(
                    "Runtime WebView2 extrait hors du pack portable : refusé.");

            await ResetDirectoryAsync(destRoot, ct);

            var destination = Path.Combine(
                destRoot,
                $"Microsoft.Web.WebView2.FixedVersionRuntime.{_s.WebView2FixedVersion}.x64");

            Directory.Move(sourceRuntime, destination);

            var installed = PortablePaths.GetFixedWebView2RuntimePath();
            if (installed is null)
                throw new InvalidOperationException(
                    "WebView2 Fixed Version extrait mais non détecté.");

            var installedExe = Path.Combine(installed, "msedgewebview2.exe");
            var version = FileVersionInfo.GetVersionInfo(installedExe).FileVersion;

            _log(
                "Install",
                $"WebView2 Fixed Version portable installé : {version ?? _s.WebView2FixedVersion} · {installed}");

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                var tail = string.Join(
                    " | ",
                    stdout.Split(
                            new[] { '\r', '\n' },
                            StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => x.Length > 0)
                        .TakeLast(2));

                if (!string.IsNullOrWhiteSpace(tail))
                    _log("WebView2", "Extraction terminée · " + tail);
            }
        }
        finally
        {
            await TryDeleteDirectoryAsync(temp);
        }
    }
    public async Task InstallQwenAsync(CancellationToken ct)
    {
        Progress(54, $"Installation {_s.VisionModel} dans Ollama portable…");

        var exe = PortablePaths.Resolve(_s.OllamaExe);
        if (!File.Exists(exe))
            throw new PortableComponentMissingException("Ollama", exe);

        const int tempPort = 11435;

        var port = await PortablePreflight.InspectPortAsync(tempPort);
        if (port.Open)
            throw new InvalidOperationException(
                $"Port temporaire {tempPort} occupé. Installation Qwen annulée.");

        var env = PortablePreflight.PortableEnvironment("ollama-install");
        env["OLLAMA_MODELS"] = PortablePaths.Resolve(_s.OllamaModels);
        env["OLLAMA_HOST"] = $"127.0.0.1:{tempPort}";
        env["OLLAMA_NO_CLOUD"] = "true";
        env["OLLAMA_NOHISTORY"] = "true";

        _log(
            "Ollama pull",
            $"Préparation {_s.VisionModel} · stockage : {env["OLLAMA_MODELS"]}");

        using var server = StartPortable(
            exe,
            "serve",
            Path.GetDirectoryName(exe)!,
            env);

        try
        {
            Progress(56, "Démarrage du serveur Ollama portable…");

            await WaitHttpAsync(
                $"http://127.0.0.1:{tempPort}/api/tags",
                TimeSpan.FromSeconds(30),
                ct);

            _log(
                "Ollama pull",
                $"Serveur portable prêt · 127.0.0.1:{tempPort}");

            Progress(
                58,
                $"Téléchargement {_s.VisionModel} · connexion à Ollama…");

            await PullOllamaModelViaApiAsync(tempPort, ct);
        }
        finally
        {
            try
            {
                if (!server.HasExited)
                    server.Kill(entireProcessTree: true);
            }
            catch { }
        }

        Progress(69, $"{_s.VisionModel} téléchargé et vérifié.");

        _log(
            "Install",
            _s.VisionModel + " installé dans models\\ollama.");
    }

    private async Task PullOllamaModelViaApiAsync(
        int port,
        CancellationToken ct)
    {
        var endpoint =
            $"http://127.0.0.1:{port}/api/pull";

        var payload = JsonSerializer.Serialize(new
        {
            name = _s.VisionModel,
            stream = true
        });

        using var request =
            new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(
                    payload,
                    Encoding.UTF8,
                    "application/json")
            };

        using var response =
            await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody =
                await response.Content.ReadAsStringAsync(ct);

            throw new InvalidOperationException(
                $"Ollama /api/pull HTTP {(int)response.StatusCode} : {errorBody}");
        }

        await using var stream =
            await response.Content.ReadAsStreamAsync(ct);

        using var reader =
            new StreamReader(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 64 * 1024,
                leaveOpen: false);

        string? currentDigest = null;
        string? lastStatus = null;

        long previousCompleted = 0;
        var previousSample = TimeSpan.Zero;

        var lastLogUtc = DateTime.MinValue;
        var lastLoggedPercent = -1;
        var watch = Stopwatch.StartNew();
        var receivedAny = false;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var line =
                await reader.ReadLineAsync(ct);

            if (line is null)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            receivedAny = true;

            using var doc =
                JsonDocument.Parse(line);

            var root =
                doc.RootElement;

            if (root.TryGetProperty("error", out var errorElement))
            {
                var error =
                    errorElement.GetString();

                if (!string.IsNullOrWhiteSpace(error))
                    throw new InvalidOperationException(
                        "Ollama : " + error);
            }

            var status =
                root.TryGetProperty("status", out var statusElement)
                    ? statusElement.GetString() ?? "Téléchargement"
                    : "Téléchargement";

            var digest =
                root.TryGetProperty("digest", out var digestElement)
                    ? digestElement.GetString()
                    : null;

            var total =
                root.TryGetProperty("total", out var totalElement) &&
                totalElement.TryGetInt64(out var totalValue)
                    ? totalValue
                    : 0L;

            var completed =
                root.TryGetProperty("completed", out var completedElement) &&
                completedElement.TryGetInt64(out var completedValue)
                    ? completedValue
                    : 0L;

            if (!string.Equals(
                    digest,
                    currentDigest,
                    StringComparison.Ordinal))
            {
                currentDigest = digest;
                previousCompleted = completed;
                previousSample = watch.Elapsed;
                lastLoggedPercent = -1;
            }

            var now = watch.Elapsed;
            var seconds =
                (now - previousSample).TotalSeconds;

            double? speedMiBps = null;

            if (seconds >= 0.35 &&
                completed >= previousCompleted)
            {
                speedMiBps =
                    ((completed - previousCompleted) / 1048576d) /
                    seconds;

                previousCompleted = completed;
                previousSample = now;
            }

            var percent =
                total > 0
                    ? Math.Clamp(
                        (int)Math.Round(
                            completed * 100d / total),
                        0,
                        100)
                    : -1;

            var digestShort =
                string.IsNullOrWhiteSpace(digest)
                    ? null
                    : digest.Length > 19
                        ? digest[..19]
                        : digest;

            var detail =
                BuildOllamaPullText(
                    status,
                    digestShort,
                    completed,
                    total,
                    percent,
                    speedMiBps);

            var installStage =
                status.Contains(
                    "success",
                    StringComparison.OrdinalIgnoreCase)
                    ? 69
                    : status.Contains(
                        "verifying",
                        StringComparison.OrdinalIgnoreCase)
                        ? 68
                        : status.Contains(
                            "writing manifest",
                            StringComparison.OrdinalIgnoreCase)
                            ? 67
                            : 58;

            Progress(
                installStage,
                $"{_s.VisionModel} · {detail}");

            var statusChanged =
                !string.Equals(
                    status,
                    lastStatus,
                    StringComparison.Ordinal);

            var percentChanged =
                percent >= 0 &&
                percent != lastLoggedPercent;

            var periodic =
                DateTime.UtcNow - lastLogUtc >=
                TimeSpan.FromSeconds(2);

            if (statusChanged ||
                percentChanged ||
                periodic)
            {
                _log(
                    "Ollama pull",
                    $"{_s.VisionModel} · {detail}");

                lastStatus = status;
                lastLogUtc = DateTime.UtcNow;

                if (percent >= 0)
                    lastLoggedPercent = percent;
            }
        }

        if (!receivedAny)
            throw new InvalidOperationException(
                "Ollama n'a retourné aucune information de progression.");

        _log(
            "Ollama pull",
            $"{_s.VisionModel} · téléchargement terminé.");
    }

    private static string BuildOllamaPullText(
        string status,
        string? digest,
        long completed,
        long total,
        int percent,
        double? speedMiBps)
    {
        var parts =
            new List<string>
            {
                status
            };

        if (!string.IsNullOrWhiteSpace(digest))
            parts.Add(digest);

        if (percent >= 0)
            parts.Add(percent + "%");

        if (total > 0)
            parts.Add($"{FormatBytes(completed)}/{FormatBytes(total)}");
        else if (completed > 0)
            parts.Add(FormatBytes(completed));

        if (speedMiBps is >= 0.01)
            parts.Add($"{speedMiBps.Value:N1} MiB/s");

        return string.Join(" · ", parts);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return bytes + " B";

        var kib = bytes / 1024d;
        if (kib < 1024)
            return $"{kib:N1} KiB";

        var mib = kib / 1024d;
        if (mib < 1024)
            return $"{mib:N1} MiB";

        return $"{mib / 1024d:N2} GiB";
    }

    public async Task InstallVideoModelsAsync(CancellationToken ct)
    {
        var root = PortablePreflight.GetComfyRoot(_s);
        var model = Path.Combine(
            root,
            "models",
            "diffusion_models",
            _s.VideoModel);
        var encoder = Path.Combine(
            root,
            "models",
            "text_encoders",
            _s.VideoTextEncoderModel);
        var vae = Path.Combine(
            root,
            "models",
            "vae",
            _s.VideoVaeModel);

        Directory.CreateDirectory(Path.GetDirectoryName(model)!);
        Directory.CreateDirectory(Path.GetDirectoryName(encoder)!);
        Directory.CreateDirectory(Path.GetDirectoryName(vae)!);

        await EnsureDownloadedAndVerifiedAsync(
            _s.VideoModelUrl, model, _s.VideoModelSha256, 2, 34, ct);
        await EnsureDownloadedAndVerifiedAsync(
            _s.VideoTextEncoderUrl, encoder, _s.VideoTextEncoderSha256, 34, 92, ct);
        await EnsureDownloadedAndVerifiedAsync(
            _s.VideoVaeUrl, vae, _s.VideoVaeSha256, 92, 100, ct);

        Progress(100, "Modèles vidéo Wan installés et vérifiés.");
    }

    public async Task EnsureExternalModelAsync(
        string url,
        string path,
        string expectedSha256,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(expectedSha256) ||
            expectedSha256.Length != 64 ||
            expectedSha256.Any(c => !Uri.IsHexDigit(c)))
        {
            throw new InvalidOperationException(
                "Le SHA256 du modèle officiel est invalide ou absent.");
        }

        await EnsureDownloadedAndVerifiedAsync(
            url,
            path,
            expectedSha256,
            0,
            100,
            ct);
    }

    public async Task<string> DownloadExternalModelAsync(
        string url,
        string path,
        string? expectedSha256,
        CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                "L'URL du modèle doit être une URL HTTP/HTTPS valide.");
        }

        var sha = expectedSha256?.Trim() ?? string.Empty;
        if (sha.Length > 0 &&
            (sha.Length != 64 ||
             sha.Any(c => !Uri.IsHexDigit(c))))
        {
            throw new InvalidOperationException(
                "Le SHA256 optionnel doit contenir exactement 64 caractères hexadécimaux.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (File.Exists(path))
            File.Delete(path);

        Progress(0, "Téléchargement modèle tiers : " + Path.GetFileName(path));
        await DownloadAsync(url, path, 0, 94, ct);

        Progress(95, "Calcul SHA256 : " + Path.GetFileName(path));
        var actual = await Sha256Async(path, ct);

        if (sha.Length > 0 &&
            !actual.Equals(sha, StringComparison.OrdinalIgnoreCase))
        {
            try { File.Delete(path); } catch { }

            throw new InvalidOperationException(
                $"SHA256 invalide pour {Path.GetFileName(path)}.\n" +
                $"Attendu : {sha}\nObtenu : {actual}");
        }

        _log(
            "Install",
            sha.Length > 0
                ? $"SHA256 OK · modèle tiers : {Path.GetFileName(path)}"
                : $"Modèle tiers téléchargé · SHA256 {actual} · {Path.GetFileName(path)}");

        Progress(100, "Modèle tiers téléchargé et vérifié.");
        return actual;
    }

    public async Task InstallFluxModelsAsync(CancellationToken ct)
    {
        var root = PortablePreflight.GetComfyRoot(_s);
        var flux = Path.Combine(root, "models", "diffusion_models", _s.FluxModel);
        var enc = Path.Combine(root, "models", "text_encoders", _s.TextEncoderModel);
        var vae = Path.Combine(root, "models", "vae", _s.VaeModel);

        Directory.CreateDirectory(Path.GetDirectoryName(flux)!);
        Directory.CreateDirectory(Path.GetDirectoryName(enc)!);
        Directory.CreateDirectory(Path.GetDirectoryName(vae)!);

        await EnsureDownloadedAndVerifiedAsync(
            _s.FluxModelUrl, flux, _s.FluxSha256, 70, 82, ct);
        await EnsureDownloadedAndVerifiedAsync(
            _s.TextEncoderUrl, enc, _s.TextEncoderSha256, 82, 96, ct);
        await EnsureDownloadedAndVerifiedAsync(
            _s.VaeUrl, vae, _s.VaeSha256, 96, 99, ct);
    }

    private async Task EnsureDownloadedAndVerifiedAsync(
        string url,
        string path,
        string sha,
        int start,
        int end,
        CancellationToken ct)
    {
        if (File.Exists(path))
        {
            Progress(start, "Vérification SHA256 : " + Path.GetFileName(path));
            var existing = await Sha256Async(path, ct);
            if (existing.Equals(sha, StringComparison.OrdinalIgnoreCase))
            {
                _log("Install", $"SHA256 OK · fichier existant conservé : {Path.GetFileName(path)}");
                return;
            }

            _log("Install ⚠", $"SHA256 invalide · retéléchargement : {Path.GetFileName(path)}");
            try { File.Delete(path); } catch { }
        }

        await DownloadAndVerifyAsync(url, path, sha, start, end, ct);
    }

    private async Task DownloadAndVerifyAsync(
        string url, string path, string sha, int start, int end, CancellationToken ct)
    {
        await DownloadAsync(url, path, start, end, ct);
        Progress(end, "Vérification SHA256 : " + Path.GetFileName(path));
        var actual = await Sha256Async(path, ct);
        if (!actual.Equals(sha, StringComparison.OrdinalIgnoreCase))
        {
            try { File.Delete(path); } catch { }
            throw new InvalidOperationException(
                $"SHA256 invalide pour {Path.GetFileName(path)}.\nAttendu : {sha}\nObtenu : {actual}");
        }
        _log("Install", $"SHA256 OK : {Path.GetFileName(path)}");
    }

    private async Task DownloadAsync(
        string url,
        string path,
        int startPercent,
        int endPercent,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException(
                "URL de téléchargement non configurée.");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var curlExe = GetWindowsCurlPath();

        if (curlExe is null)
        {
            _log("Install !", "curl.exe Windows introuvable.");
            await DownloadSequentialFallbackAsync(
                url, path, startPercent, endPercent, ct);
            return;
        }

        var probe = await ProbeRangeSupportAsync(url, ct);
        var connections = Math.Clamp(_s.DownloadConnections, 1, 16);
        var threshold =
            Math.Max(8L, _s.ParallelDownloadThresholdMiB) *
            1024L * 1024L;

        if (connections > 1 &&
            probe.SupportsRanges &&
            probe.Length is > 0 &&
            probe.Length.Value >= threshold)
        {
            try
            {
                // Toujours donner à curl l'URL d'origine.
                // curl suit lui-même les redirections avec --location.
                // Certaines URL finales signées de CDN peuvent être très longues
                // et déclencher curl (3) lorsqu'elles sont réinjectées.
                await DownloadWithMultiCurlAsync(
                    curlExe,
                    url,
                    path,
                    probe.Length.Value,
                    connections,
                    startPercent,
                    endPercent,
                    ct);

                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log(
                    "Install !",
                    "Multi-curl a échoué : " + ex.Message);

                await TryDeleteDirectoryAsync(
                    path + ".curlparts");

                _log(
                    "Install",
                    "Repli curl Windows mono-connexion.");
            }
        }

        await DownloadWithSingleCurlAsync(
            curlExe,
            url,
            path,
            startPercent,
            endPercent,
            ct);
    }

    private static string? GetWindowsCurlPath()
    {
        var candidate = Path.Combine(
            Environment.SystemDirectory,
            "curl.exe");

        return File.Exists(candidate)
            ? candidate
            : null;
    }

    private async Task<(bool SupportsRanges, long? Length, string? EffectiveUrl)>
        ProbeRangeSupportAsync(
            string url,
            CancellationToken ct)
    {
        try
        {
            using var request =
                new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Range =
                new RangeHeaderValue(0, 0);

            using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            var effectiveUrl =
                response.RequestMessage?.RequestUri?.ToString();

            if (response.StatusCode != HttpStatusCode.PartialContent)
            {
                return (
                    false,
                    response.Content.Headers.ContentLength,
                    effectiveUrl);
            }

            var range =
                response.Content.Headers.ContentRange;

            return (
                range?.HasLength == true,
                range?.Length,
                effectiveUrl);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return (false, null, null);
        }
    }

    private async Task DownloadWithMultiCurlAsync(
        string curlExe,
        string url,
        string path,
        long totalLength,
        int connections,
        int startPercent,
        int endPercent,
        CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(path)!;
        var fileName = Path.GetFileName(path);
        var partsDir = path + ".curlparts";

        Directory.CreateDirectory(partsDir);

        var segments =
            BuildCurlSegments(totalLength, connections);

        _log(
            "Install",
            $"MULTI-CURL WINDOWS ACTIF · {segments.Count} connexions · " +
            $"segments séparés · débit illimité · {fileName}");

        if (Uri.TryCreate(url, UriKind.Absolute, out var sourceUri))
        {
            _log(
                "Install",
                $"Source multi-curl : {sourceUri.Scheme}://{sourceUri.Host}");
        }

        _log(
            "Install",
            $"Racine portable : {PortablePaths.Root}");

        using var multiCts =
            CancellationTokenSource.CreateLinkedTokenSource(ct);

        var tasks = new List<Task>();

        for (var i = 0; i < segments.Count; i++)
        {
            var index = i;
            var segment = segments[i];
            var partPath =
                GetCurlPartPath(partsDir, index);

            var expected =
                segment.End - segment.Start + 1;

            if (File.Exists(partPath) &&
                new FileInfo(partPath).Length == expected)
            {
                continue;
            }

            try
            {
                if (File.Exists(partPath))
                    File.Delete(partPath);
            }
            catch { }

            tasks.Add(
                DownloadCurlRangeAsync(
                    curlExe,
                    url,
                    partPath,
                    segment.Start,
                    segment.End,
                    multiCts.Token));
        }

        var watch = Stopwatch.StartNew();
        var lastAt = TimeSpan.Zero;
        var lastProgressAt = TimeSpan.Zero;
        long lastBytes = 0;
        long lastProgressBytes = 0;
        double speedMiBps = 0;

        while (tasks.Any(t => !t.IsCompleted))
        {
            ct.ThrowIfCancellationRequested();

            if (tasks.Any(t => t.IsFaulted))
            {
                multiCts.Cancel();
                try { await Task.WhenAll(tasks); } catch { }

                var failure =
                    tasks
                        .FirstOrDefault(t => t.IsFaulted)?
                        .Exception?
                        .GetBaseException();

                throw new InvalidOperationException(
                    "Un segment multi-curl a échoué." +
                    (failure is null
                        ? string.Empty
                        : " " + failure.Message),
                    failure);
            }

            var current =
                GetDownloadedPartBytes(
                    partsDir,
                    segments.Count,
                    totalLength);

            if (current > lastProgressBytes)
            {
                lastProgressBytes = current;
                lastProgressAt = watch.Elapsed;
            }
            else if (watch.Elapsed - lastProgressAt >
                     TimeSpan.FromSeconds(45))
            {
                _log(
                    "Install !",
                    $"{fileName} · aucune progression multi-curl depuis 45 s ; " +
                    "bascule vers une connexion unique.");

                multiCts.Cancel();
                try { await Task.WhenAll(tasks); } catch { }

                throw new TimeoutException(
                    "Multi-curl sans progression depuis 45 secondes.");
            }

            var now = watch.Elapsed;
            var seconds =
                (now - lastAt).TotalSeconds;

            if (seconds >= 0.5)
            {
                speedMiBps = Math.Max(
                    0,
                    ((current - lastBytes) / 1048576d) /
                    seconds);

                lastBytes = current;
                lastAt = now;
            }

            var ratio =
                (double)current / totalLength;

            var pct = startPercent +
                      (int)((endPercent - startPercent) *
                            Math.Clamp(ratio, 0d, 1d));

            Progress(
                pct,
                $"{fileName} · " +
                $"{current / 1048576:N0}/{totalLength / 1048576:N0} MiB · " +
                $"{speedMiBps:N1} MiB/s · multi-curl x{segments.Count}");

            await Task.Delay(400, ct);
        }

        await Task.WhenAll(tasks);
        ct.ThrowIfCancellationRequested();

        for (var i = 0; i < segments.Count; i++)
        {
            var part =
                GetCurlPartPath(partsDir, i);

            var expected =
                segments[i].End -
                segments[i].Start + 1;

            if (!File.Exists(part) ||
                new FileInfo(part).Length != expected)
            {
                throw new InvalidOperationException(
                    $"Segment {i + 1}/{segments.Count} incomplet.");
            }
        }

        var assembling = path + ".assembling";

        try
        {
            if (File.Exists(assembling))
                File.Delete(assembling);

            var bufferSize =
                Math.Clamp(_s.DownloadBufferMiB, 1, 32) *
                1024 * 1024;

            await using var output = new FileStream(
                assembling,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize,
                FileOptions.Asynchronous |
                FileOptions.SequentialScan);

            for (var i = 0; i < segments.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var part =
                    GetCurlPartPath(partsDir, i);

                await using (var input = new FileStream(
                    part,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize,
                    FileOptions.Asynchronous |
                    FileOptions.SequentialScan))
                {
                    await input.CopyToAsync(
                        output,
                        bufferSize,
                        ct);
                }

                try { File.Delete(part); } catch { }
            }

            await output.FlushAsync(ct);
        }
        catch
        {
            try
            {
                if (File.Exists(assembling))
                    File.Delete(assembling);
            }
            catch { }

            throw;
        }

        if (!File.Exists(assembling) ||
            new FileInfo(assembling).Length != totalLength)
        {
            try
            {
                if (File.Exists(assembling))
                    File.Delete(assembling);
            }
            catch { }

            throw new InvalidOperationException(
                "Le fichier assemblé n'a pas la taille attendue.");
        }

        await MoveFileWithRetryAsync(
            assembling,
            path,
            overwrite: true,
            ct);

        try { Directory.Delete(partsDir, true); } catch { }

        Progress(
            endPercent,
            $"{fileName} · téléchargement terminé · multi-curl");
    }

    private async Task DownloadCurlRangeAsync(
        string curlExe,
        string url,
        string partPath,
        long start,
        long end,
        CancellationToken ct)
    {
        var psi =
            CreateCurlStartInfo(curlExe);

        psi.ArgumentList.Add("--range");
        psi.ArgumentList.Add($"{start}-{end}");
        psi.ArgumentList.Add("--output");
        psi.ArgumentList.Add(partPath);
        psi.ArgumentList.Add("--url");
        psi.ArgumentList.Add(url);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException(
                "Impossible de démarrer curl.exe Windows.");

        using var cancel = ct.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch { }
        });

        var stdoutTask =
            process.StandardOutput.ReadToEndAsync();

        var stderrTask =
            process.StandardError.ReadToEndAsync();

        var inactivity = Stopwatch.StartNew();
        long lastPartLength =
            File.Exists(partPath)
                ? new FileInfo(partPath).Length
                : 0L;

        while (!process.HasExited)
        {
            ct.ThrowIfCancellationRequested();

            long currentPartLength = lastPartLength;
            try
            {
                if (File.Exists(partPath))
                    currentPartLength =
                        new FileInfo(partPath).Length;
            }
            catch { }

            if (currentPartLength > lastPartLength)
            {
                lastPartLength = currentPartLength;
                inactivity.Restart();
            }
            else if (inactivity.Elapsed >
                     TimeSpan.FromSeconds(45))
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch { }

                try
                {
                    await process.WaitForExitAsync(
                        CancellationToken.None);
                }
                catch { }

                throw new TimeoutException(
                    $"curl {start}-{end} sans progression depuis 45 secondes.");
            }

            await Task.Delay(500, ct);
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            var detail = string.Join(
                " | ",
                (stderr + Environment.NewLine + stdout)
                    .Split(
                        new[] { '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries)
                    .TakeLast(3));

            throw new InvalidOperationException(
                $"curl {start}-{end} a échoué " +
                $"(code {process.ExitCode}). {detail}");
        }
    }

    private async Task DownloadWithSingleCurlAsync(
        string curlExe,
        string url,
        string path,
        int startPercent,
        int endPercent,
        CancellationToken ct)
    {
        var tempPath =
            path + ".curl.part";

        var fileName =
            Path.GetFileName(path);

        var expectedLength =
            await TryGetContentLengthAsync(url, ct);

        var psi =
            CreateCurlStartInfo(curlExe);

        psi.ArgumentList.Add("--continue-at");
        psi.ArgumentList.Add("-");
        psi.ArgumentList.Add("--output");
        psi.ArgumentList.Add(tempPath);
        psi.ArgumentList.Add("--url");
        psi.ArgumentList.Add(url);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException(
                "Impossible de démarrer curl.exe Windows.");

        using var cancel = ct.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch { }
        });

        var stdoutTask =
            process.StandardOutput.ReadToEndAsync();

        var stderrTask =
            process.StandardError.ReadToEndAsync();

        var watch = Stopwatch.StartNew();
        var lastAt = TimeSpan.Zero;
        long lastBytes =
            File.Exists(tempPath)
                ? new FileInfo(tempPath).Length
                : 0L;
        double speedMiBps = 0;

        _log(
            "Install",
            $"CURL WINDOWS ACTIF · mono-connexion · débit illimité · {fileName}");

        while (!process.HasExited)
        {
            ct.ThrowIfCancellationRequested();

            long current = 0;

            try
            {
                if (File.Exists(tempPath))
                    current =
                        new FileInfo(tempPath).Length;
            }
            catch { }

            var now = watch.Elapsed;
            var seconds =
                (now - lastAt).TotalSeconds;

            if (seconds >= 0.5)
            {
                speedMiBps = Math.Max(
                    0,
                    ((current - lastBytes) / 1048576d) /
                    seconds);

                lastBytes = current;
                lastAt = now;
            }

            if (expectedLength is > 0)
            {
                var ratio = Math.Clamp(
                    (double)current /
                    expectedLength.Value,
                    0d,
                    1d);

                var pct = startPercent +
                          (int)((endPercent - startPercent) * ratio);

                Progress(
                    pct,
                    $"{fileName} · " +
                    $"{current / 1048576:N0}/{expectedLength.Value / 1048576:N0} MiB · " +
                    $"{speedMiBps:N1} MiB/s · curl Windows");
            }

            await Task.Delay(400, ct);
        }

        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            var detail = string.Join(
                " | ",
                (stderr + Environment.NewLine + stdout)
                    .Split(
                        new[] { '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries)
                    .TakeLast(5));

            throw new InvalidOperationException(
                $"curl.exe a échoué (code {process.ExitCode}). {detail}");
        }

        if (!File.Exists(tempPath))
            throw new InvalidOperationException(
                "curl.exe indique un succès mais le fichier temporaire est absent.");

        await MoveFileWithRetryAsync(
            tempPath,
            path,
            overwrite: true,
            ct);

        Progress(
            endPercent,
            $"{fileName} · téléchargement terminé · curl Windows");
    }

    private static ProcessStartInfo CreateCurlStartInfo(
        string curlExe)
    {
        var psi = new ProcessStartInfo
        {
            FileName = curlExe,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        psi.ArgumentList.Add("--location");
        psi.ArgumentList.Add("--fail");
        psi.ArgumentList.Add("--silent");
        psi.ArgumentList.Add("--show-error");
        psi.ArgumentList.Add("--retry");
        psi.ArgumentList.Add("8");
        psi.ArgumentList.Add("--retry-delay");
        psi.ArgumentList.Add("1");
        psi.ArgumentList.Add("--retry-all-errors");
        psi.ArgumentList.Add("--connect-timeout");
        psi.ArgumentList.Add("15");
        psi.ArgumentList.Add("--user-agent");
        psi.ArgumentList.Add(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/154.0.0.0 Safari/537.36");

        return psi;
    }

    private async Task<long?> TryGetContentLengthAsync(
        string url,
        CancellationToken ct)
    {
        try
        {
            using var request =
                new HttpRequestMessage(HttpMethod.Head, url);

            using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            if (response.IsSuccessStatusCode &&
                response.Content.Headers.ContentLength is long length &&
                length > 0)
            {
                return length;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch { }

        return null;
    }

    private static List<(long Start, long End)>
        BuildCurlSegments(
            long totalLength,
            int connections)
    {
        var result =
            new List<(long Start, long End)>();

        var partSize =
            (long)Math.Ceiling(
                (double)totalLength / connections);

        long cursor = 0;

        while (cursor < totalLength)
        {
            var end = Math.Min(
                totalLength - 1,
                cursor + partSize - 1);

            result.Add((cursor, end));
            cursor = end + 1;
        }

        return result;
    }

    private static string GetCurlPartPath(
        string partsDir,
        int index)
        => Path.Combine(
            partsDir,
            $"part-{index:D2}.bin");

    private static long GetDownloadedPartBytes(
        string partsDir,
        int partCount,
        long max)
    {
        long total = 0;

        for (var i = 0; i < partCount; i++)
        {
            try
            {
                var part =
                    GetCurlPartPath(partsDir, i);

                if (File.Exists(part))
                    total += new FileInfo(part).Length;
            }
            catch { }
        }

        return Math.Clamp(total, 0, max);
    }

    private async Task DownloadSequentialFallbackAsync(
        string url,
        string path,
        int startPercent,
        int endPercent,
        CancellationToken ct)
    {
        var tmp = path + ".fallback";

        try
        {
            {
                using var request =
                    new HttpRequestMessage(HttpMethod.Get, url);

                using var response = await _http.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);

                response.EnsureSuccessStatusCode();

                var total =
                    response.Content.Headers.ContentLength;

                await using var input =
                    await response.Content.ReadAsStreamAsync(ct);

                await using var output = new FileStream(
                    tmp,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    4 * 1024 * 1024,
                    FileOptions.Asynchronous |
                    FileOptions.SequentialScan);

                var buffer =
                    new byte[8 * 1024 * 1024];

                long done = 0;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    var read =
                        await input.ReadAsync(
                            buffer,
                            ct);

                    if (read <= 0)
                        break;

                    await output.WriteAsync(
                        buffer.AsMemory(0, read),
                        ct);

                    done += read;

                    if (total is > 0)
                    {
                        var ratio =
                            (double)done /
                            total.Value;

                        var pct = startPercent +
                                  (int)((endPercent - startPercent) * ratio);

                        Progress(
                            pct,
                            $"{Path.GetFileName(path)} · " +
                            $"{done / 1048576:N0}/{total.Value / 1048576:N0} MiB · fallback");
                    }
                }

                await output.FlushAsync(ct);
            }

            await MoveFileWithRetryAsync(
                tmp,
                path,
                overwrite: true,
                ct);
        }
        catch
        {
            try
            {
                if (File.Exists(tmp))
                    File.Delete(tmp);
            }
            catch { }

            throw;
        }
    }

    private static async Task MoveFileWithRetryAsync(
        string source,
        string destination,
        bool overwrite,
        CancellationToken ct)
    {
        IOException? last = null;

        for (var attempt = 1;
             attempt <= 10;
             attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                File.Move(
                    source,
                    destination,
                    overwrite);
                return;
            }
            catch (IOException ex)
            {
                last = ex;

                if (attempt == 10)
                    break;

                await Task.Delay(
                    TimeSpan.FromMilliseconds(
                        150 * attempt),
                    ct);
            }
        }

        throw new IOException(
            $"Impossible de finaliser le fichier : {destination}",
            last);
    }

    private async Task ExtractZipAsync(
        string archivePath,
        string destination,
        int startPercent,
        int endPercent,
        CancellationToken ct)
    {
        Directory.CreateDirectory(destination);

        await using var input = new FileStream(
            archivePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var archive = new ZipArchive(input, ZipArchiveMode.Read);

        var files = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToArray();
        var done = 0;

        foreach (var entry in files)
        {
            ct.ThrowIfCancellationRequested();

            var target = Path.GetFullPath(
                Path.Combine(destination, entry.FullName));

            var destinationRoot =
                Path.GetFullPath(destination) + Path.DirectorySeparatorChar;

            if (!target.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Archive ZIP invalide : chemin sortant du dossier portable.");

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

            await using var source = entry.Open();
            await using var output = new FileStream(
                target,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                256 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            await source.CopyToAsync(output, 256 * 1024, ct);

            done++;
            if (done % 20 == 0 || done == files.Length)
            {
                var pct = files.Length == 0
                    ? endPercent
                    : startPercent + (int)((endPercent - startPercent) * ((double)done / files.Length));

                Progress(
                    pct,
                    $"Extraction {Path.GetFileName(archivePath)} · {done}/{files.Length} fichiers");
                await Task.Yield();
            }
        }
    }

    private static async Task ResetDirectoryAsync(
        string path,
        CancellationToken ct)
    {
        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            if (Directory.Exists(path))
                Directory.Delete(path, true);

            ct.ThrowIfCancellationRequested();
            Directory.CreateDirectory(path);
        }, ct);
    }

    private static async Task TryDeleteDirectoryAsync(string path)
    {
        try
        {
            await Task.Run(() =>
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            });
        }
        catch
        {
            // Nettoyage best-effort : ne jamais masquer l'annulation.
        }
    }

    private static Process StartPortable(
        string exe, string args, string cwd, IDictionary<string,string> env, bool redirect = false)
    {
        if (!PortablePaths.IsInsidePack(exe))
            throw new InvalidOperationException("Exécutable externe refusé : " + exe);

        var psi = new ProcessStartInfo(exe, args)
        {
            WorkingDirectory = cwd,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = redirect,
            RedirectStandardError = redirect
        };
        foreach (var kv in env) psi.Environment[kv.Key] = kv.Value;
        return Process.Start(psi) ?? throw new InvalidOperationException("Démarrage impossible : " + exe);
    }

    private static async Task WaitHttpAsync(string url, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var until = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < until)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var r = await http.GetAsync(url, ct);
                if (r.IsSuccessStatusCode) return;
            }
            catch { }
            await Task.Delay(400, ct);
        }
        throw new TimeoutException("Service portable non prêt : " + url);
    }

    private static async Task<string> Sha256Async(string path, CancellationToken ct)
    {
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, true);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(fs, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

}
