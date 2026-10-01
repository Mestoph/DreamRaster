/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


FR : Client de mise à jour basé sur GitHub Releases. Il ne touche qu'au pack portable.
EN: GitHub Releases updater. It only modifies files inside the portable pack.
*/
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace OpenCodeLocalAI;

internal sealed record GitHubUpdateInfo(
    bool Available,
    Version CurrentVersion,
    Version LatestVersion,
    string Tag,
    string ReleaseUrl,
    string? AssetUrl,
    string? Notes);

internal sealed class GitHubUpdater : IDisposable
{
    private readonly AppSettings _settings;
    private readonly HttpClient _http;

    public GitHubUpdater(AppSettings settings)
    {
        _settings = settings;

        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(20) };
        _http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("DreamRaster", Application.ProductVersion));
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public string RepositoryUrl
        => $"https://github.com/{Owner}/{Repository}";

    private string Owner
        => string.IsNullOrWhiteSpace(_settings.GitHubOwner)
            ? BrandInfo.DefaultGitHubOwner
            : _settings.GitHubOwner.Trim();

    private string Repository
        => string.IsNullOrWhiteSpace(_settings.GitHubRepository)
            ? BrandInfo.DefaultGitHubRepository
            : _settings.GitHubRepository.Trim();

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

    public void Dispose() => _http.Dispose();
}
