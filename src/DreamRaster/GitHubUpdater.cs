/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


Client de mise à jour basé sur GitHub Releases. Il ne touche qu'au pack portable.
*/
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace OpenCodeLocalAI;

/// <summary>

/// Définit record « GitHubUpdateInfo », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

/// </summary>
internal sealed record GitHubUpdateInfo(
    bool Available,
    Version CurrentVersion,
    Version LatestVersion,
    string Tag,
    string ReleaseUrl,
    string? AssetUrl,
    string? Notes);

/// <summary>

/// Définit class « GitHubUpdater », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

/// </summary>
internal sealed class GitHubUpdater : IDisposable
{
    /// <summary>
    /// Stocke « _settings », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private readonly AppSettings _settings;
    /// <summary>
    /// Stocke « _http », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private readonly HttpClient _http;

/// <summary>
/// Initialise le client de mise ? jour avec la configuration courante afin de r?soudre le d?p?t GitHub, comparer les versions et pr?parer les t?l?chargements autoris?s.
/// </summary>
    public GitHubUpdater(AppSettings settings)
    {
        _settings = settings;

        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(20) };
        _http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("DreamRaster", Application.ProductVersion));
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

/// <summary>
/// Retourne l?URL publique du d?p?t GitHub actuellement configur? ? partir du propri?taire et du nom de d?p?t valid?s.
/// </summary>
    public string RepositoryUrl
        => $"https://github.com/{Owner}/{Repository}";

/// <summary>
/// Extrait et m?morise le propri?taire GitHub du d?p?t configur? afin de construire les appels ? l?API Releases.
/// </summary>
    private string Owner
        => string.IsNullOrWhiteSpace(_settings.GitHubOwner)
            ? BrandInfo.DefaultGitHubOwner
            : _settings.GitHubOwner.Trim();

/// <summary>
/// Extrait et m?morise le nom du d?p?t GitHub utilis? pour rechercher les releases de DreamRaster.
/// </summary>
    private string Repository
        => string.IsNullOrWhiteSpace(_settings.GitHubRepository)
            ? BrandInfo.DefaultGitHubRepository
            : _settings.GitHubRepository.Trim();

    /// <summary>

    /// Vérifie l’état géré par <c>CheckAsync</c> et retourne un diagnostic exploitable.

    /// </summary>
    public async Task<GitHubUpdateInfo?> CheckAsync(CancellationToken ct)
    {
        var uri =
            $"https://api.github.com/repos/{Uri.EscapeDataString(Owner)}/{Uri.EscapeDataString(Repository)}/releases/latest";

        using var response =
            await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(ct);

        using var doc =
            await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var root = doc.RootElement;
        var tag = root.TryGetProperty("tag_name", out var tagEl)
            ? tagEl.GetString() ?? ""
            : "";

        var latest = ParseVersion(tag);
        var current = ParseVersion(Application.ProductVersion);
        string? assetUrl = null;

        if (root.TryGetProperty("assets", out var assets) &&
            assets.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.TryGetProperty("name", out var nameEl)
                    ? nameEl.GetString()
                    : null;

                if (!string.Equals(name, BrandInfo.ReleaseAssetName, StringComparison.OrdinalIgnoreCase))
                    continue;

                assetUrl = asset.TryGetProperty("browser_download_url", out var urlEl)
                    ? urlEl.GetString()
                    : null;
                break;
            }

            if (assetUrl is null)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var nameEl)
                        ? nameEl.GetString()
                        : null;

                    if (name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) != true)
                        continue;

                    assetUrl = asset.TryGetProperty("browser_download_url", out var urlEl)
                        ? urlEl.GetString()
                        : null;

                    if (assetUrl is not null)
                        break;
                }
            }
        }

        var releaseUrl = root.TryGetProperty("html_url", out var htmlEl)
            ? htmlEl.GetString() ?? RepositoryUrl + "/releases"
            : RepositoryUrl + "/releases";

        var notes = root.TryGetProperty("body", out var bodyEl)
            ? bodyEl.GetString()
            : null;

        return new GitHubUpdateInfo(
            latest > current,
            current,
            latest,
            tag,
            releaseUrl,
            assetUrl,
            notes);
    }

    /// <summary>

    /// Exécute le traitement <c>StageAndLaunchAsync</c> et conserve un état cohérent en cas de succès comme d’erreur.

    /// </summary>
    public async Task StageAndLaunchAsync(
        GitHubUpdateInfo update,
        IProgress<int>? progress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(update.AssetUrl))
            throw new InvalidOperationException(
                "La release GitHub ne contient aucun ZIP de mise à jour.");

        var updatesDir = Path.Combine(PortablePaths.RuntimeDir, "updates");
        var stageDir = Path.Combine(updatesDir, "stage");

        Directory.CreateDirectory(updatesDir);

        if (Directory.Exists(stageDir))
            Directory.Delete(stageDir, recursive: true);

        Directory.CreateDirectory(stageDir);

        var zipPath = Path.Combine(updatesDir, "release.zip");

        await DownloadAsync(update.AssetUrl, zipPath, progress, ct);

        ZipFile.ExtractToDirectory(zipPath, stageDir, overwriteFiles: true);

        var newExe =
            Directory.GetFiles(stageDir, BrandInfo.ExecutableName, SearchOption.AllDirectories)
                .FirstOrDefault();

        if (newExe is null)
            throw new InvalidOperationException(
                $"{BrandInfo.ExecutableName} absent de l'archive de mise à jour.");

        var targetExe =
            Environment.ProcessPath
            ?? Path.Combine(PortablePaths.Root, BrandInfo.ExecutableName);

        if (!PortablePaths.IsInsidePack(targetExe))
            throw new InvalidOperationException(
                "La cible de mise à jour n'est pas dans le pack portable.");

        var script = Path.Combine(updatesDir, "apply-update.cmd");

        await File.WriteAllTextAsync(
            script,
            BuildUpdateScript(),
            Encoding.ASCII,
            ct);

        var psi = new ProcessStartInfo
        {
            FileName = script,
            UseShellExecute = true,
            WorkingDirectory = updatesDir
        };

        psi.ArgumentList.Add(Environment.ProcessId.ToString());
        psi.ArgumentList.Add(newExe);
        psi.ArgumentList.Add(targetExe);

        var updaterProcess = Process.Start(psi);

        if (updaterProcess is null)
        {
            throw new InvalidOperationException(
                "Impossible de lancer le programme de mise à jour.");
        }
    }

    /// <summary>
    /// Télécharge un fichier de mise à jour depuis GitHub en flux continu,
    /// écrit son contenu sur disque et publie la progression lorsque la taille
    /// distante est connue.
    /// </summary>
    private async Task DownloadAsync(
        string url,
        string path,
        IProgress<int>? progress,
        CancellationToken ct)
    {
        using var response =
            await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);

        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;

        await using var input =
            await response.Content.ReadAsStreamAsync(ct);

        await using var output =
            new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                1024 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

        var buffer = new byte[1024 * 1024];
        long done = 0;

        while (true)
        {
            var read = await input.ReadAsync(buffer, ct);

            if (read <= 0)
                break;

            await output.WriteAsync(buffer.AsMemory(0, read), ct);
            done += read;

            if (total is > 0)
            {
                progress?.Report(
                    Math.Clamp(
                        (int)Math.Round(done * 100d / total.Value),
                        0,
                        100));
            }
        }

        await output.FlushAsync(ct);
        progress?.Report(100);
    }

    /// <summary>

    /// Analyse la valeur traitée par <c>ParseVersion</c> et la convertit dans sa représentation interne.

    /// </summary>
    private static Version ParseVersion(string? raw)
    {
        raw ??= "0.0.0";
        raw = raw.Trim();

        if (raw.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            raw = raw[1..];

        var plus = raw.IndexOf('+');
        if (plus >= 0)
            raw = raw[..plus];

        var dash = raw.IndexOf('-');
        if (dash >= 0)
            raw = raw[..dash];

        return Version.TryParse(raw, out var version)
            ? version
            : new Version(0, 0, 0);
    }

    /// <summary>

    /// Construit BuildUpdateScript à partir de l’état courant sans modifier les données utilisateur au-delà de ce qui est explicitement requis.

    /// </summary>
    private static string BuildUpdateScript()
        => """
@echo off
setlocal EnableExtensions
set "PID=%~1"
set "NEWEXE=%~2"
set "TARGET=%~3"

:wait_process
tasklist /FI "PID eq %PID%" /NH 2>nul | findstr /C:"%PID%" >nul
if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto wait_process
)

copy /Y "%NEWEXE%" "%TARGET%" >nul
if errorlevel 1 (
    echo Update failed while replacing the executable.
    pause
    exit /b 1
)

start "" "%TARGET%"
del "%~f0"
""";

    /// <summary>

    /// Libère les ressources détenues par cette instance et termine proprement les objets associés.

    /// </summary>
    public void Dispose() => _http.Dispose();
}
