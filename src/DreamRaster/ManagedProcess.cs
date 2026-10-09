/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

Gestion sûre des processus enfants portables et de leurs logs.
Les commentaires structurants sont r?dig?s en fran?ais. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
*/

using System.Diagnostics;
using System.Text;

namespace OpenCodeLocalAI;

/// <summary>

/// Définit class « ManagedProcess », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

/// </summary>
public sealed class ManagedProcess : IDisposable
{
    /// <summary>
    /// Stocke « _log », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private readonly Action<string, string> _log;
    /// <summary>
    /// Stocke « _lifecycleGate », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    /// <summary>
    /// Valeur de configuration _process utilisée par DreamRaster.
    /// </summary>
    private Process? _process;
    /// <summary>
    /// Stocke « _expectedStopPid », donnée interne utilisée par ce composant pour conserver son état ou ses dépendances.
    /// </summary>
    private int _expectedStopPid;

        /// <summary>
    /// Nom logique du service enfant g?r? ; il est utilis? dans les journaux et messages d??tat ind?pendamment du nom r?el de l?ex?cutable.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Exécute Running en coordonnant les ressources et les mécanismes d’annulation nécessaires.
    /// </summary>
    public bool Running => _process is { HasExited: false };
/// <summary>
/// Expose en lecture seule le processus enfant actuellement suivi, ou null lorsqu?aucun processus n?est actif.
/// </summary>
    public Process? Process => _process;

/// <summary>
/// Initialise le gestionnaire d?un processus portable avec son nom logique et le callback utilis? pour journaliser stdout, stderr et les ?v?nements de cycle de vie.
/// </summary>
    public ManagedProcess(string name, Action<string, string> log)
    {
        Name = name;
        _log = log;
    }

    /// <summary>

    /// Démarre l’opération gérée par <c>StartAsync</c> et prépare ses ressources.

    /// </summary>
    public async Task StartAsync(
        string portableExe,
        string arguments,
        string workingDirectory,
        IDictionary<string, string>? environment = null,
        CancellationToken ct = default)
    {
        await _lifecycleGate.WaitAsync(ct);
        try
        {
            await StartCoreAsync(
                portableExe,
                arguments,
                workingDirectory,
                environment,
                ct);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <summary>

    /// Démarre l’opération gérée par <c>StartCoreAsync</c> et prépare ses ressources.

    /// </summary>
    private async Task StartCoreAsync(
        string portableExe,
        string arguments,
        string workingDirectory,
        IDictionary<string, string>? environment,
        CancellationToken ct)
    {
        if (Running)
            return;

        if (_process is not null)
        {
            try { _process.Dispose(); } catch { }
            _process = null;
        }

        var exe = PortablePaths.Resolve(portableExe);
        if (!File.Exists(exe))
            throw new PortableComponentMissingException(Name, exe);

        if (!PortablePaths.IsInsidePack(exe))
            throw new InvalidOperationException($"{Name} : exécutable hors du pack refusé.");

        workingDirectory = PortablePaths.Resolve(workingDirectory);
        Directory.CreateDirectory(workingDirectory);

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        if (environment is not null)
            foreach (var kv in environment)
                psi.Environment[kv.Key] = kv.Value;

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process = process;
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) _log(Name, e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) LogStandardError(e.Data); };
        process.Exited += (_, _) => LogProcessExit(process);

        try
        {
            if (!process.Start())
                throw new InvalidOperationException($"Impossible de démarrer {Name}.");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            _log(Name, $"PID {process.Id} · {exe}");
        }
        catch
        {
            _process.Dispose();
            _process = null;
            throw;
        }

        await Task.Delay(100, ct);
    }

    /// <summary>

    /// Journalise l’information gérée par <c>LogStandardError</c> avec la gravité appropriée.

    /// </summary>
    private void LogStandardError(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        // ComfyUI/tqdm et plusieurs autres outils CLI utilisent stderr pour une sortie console normale.
        // La gravité est déduite du contenu et jamais du seul fait que le message provient de stderr.
        if (line.Contains("[INFO]", StringComparison.OrdinalIgnoreCase))
        {
            _log(Name, line);
            return;
        }

        var isError =
            ContainsAny(
                line,
                "[ERROR]",
                "ERROR:",
                "Traceback",
                "Exception",
                "RuntimeError",
                "CUDA error",
                "Prompt outputs failed validation",
                "execution_error",
                "fatal error",
                " FATAL ");

        if (isError)
        {
            _log(Name + " ✗", line);
            return;
        }

        var isWarning =
            ContainsAny(
                line,
                "[WARNING]",
                "FutureWarning",
                "RuntimeWarning",
                "UserWarning",
                "DeprecationWarning");

        if (isWarning)
        {
            _log(Name + " ⚠", line);
            return;
        }

        // ComfyUI/tqdm routinely emits ordinary progress on stderr.
        // Les autres processus enfants conservent un marqueur d’avertissement visible pour stderr non classé.
        _log(
            Name.Equals("ComfyUI", StringComparison.OrdinalIgnoreCase)
                ? Name
                : Name + " ⚠",
            line);
    }

    /// <summary>

    /// Détermine si la collection ou le texte analysé par <c>ContainsAny</c> contient l’une des valeurs recherchées.

    /// </summary>
    private static bool ContainsAny(string text, params string[] needles) =>
        needles.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase));

    /// <summary>

    /// Journalise l’information gérée par <c>LogProcessExit</c> avec la gravité appropriée.

    /// </summary>
    private void LogProcessExit(Process process)
    {
        int pid = 0;
        int? code = null;
        try
        {
            pid = process.Id;
            if (process.HasExited)
                code = process.ExitCode;
        }
        catch
        {
        }

        var expectedStop =
            pid != 0 &&
            pid == Volatile.Read(ref _expectedStopPid);

        if (expectedStop)
        {
            _log(
                Name,
                code is null
                    ? "Processus arrêté volontairement."
                    : $"Processus arrêté volontairement (code {code}).");
            Interlocked.CompareExchange(ref _expectedStopPid, 0, pid);
            return;
        }

        if (code is null or 0)
        {
            _log(
                Name,
                code is null
                    ? "Processus terminé."
                    : "Processus terminé (code 0).");
        }
        else
        {
            _log(
                Name + " ✗",
                $"Processus terminé de façon inattendue (code {code}).");
        }
    }

    /// <summary>

    /// Arrête proprement l’opération gérée par <c>StopAsync</c> et libère ses ressources.

    /// </summary>
    public async Task StopAsync(TimeSpan? grace = null)
    {
        await _lifecycleGate.WaitAsync();
        try
        {
            await StopCoreAsync(grace);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <summary>

    /// Arrête proprement l’opération gérée par <c>StopCoreAsync</c> et libère ses ressources.

    /// </summary>
    private async Task StopCoreAsync(TimeSpan? grace)
    {
        var p = _process;
        if (p is null) return;

        try
        {
            if (!p.HasExited)
            {
                Volatile.Write(ref _expectedStopPid, p.Id);

                try
                {
                    if (p.MainWindowHandle != IntPtr.Zero)
                        p.CloseMainWindow();
                }
                catch
                {
                }

                var until = DateTime.UtcNow + (grace ?? TimeSpan.FromSeconds(5));
                while (!p.HasExited && DateTime.UtcNow < until)
                    await Task.Delay(150);

                if (!p.HasExited)
                    p.Kill(entireProcessTree: true);

                await p.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            _log(Name + " ⚠", "Arrêt : " + ex.Message);
        }
        finally
        {
            p.Dispose();
            if (ReferenceEquals(_process, p))
                _process = null;
        }
    }

    /// <summary>

    /// Libère les ressources détenues par cette instance et termine proprement les objets associés.

    /// </summary>
    public void Dispose()
    {
        try
        {
            StopAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
        }
        catch
        {
        }
    }
}

/// <summary>

/// Définit class « PortableComponentMissingException », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

/// </summary>
public sealed class PortableComponentMissingException : Exception
{
    /// <summary>
    /// Nom du composant portable manquant ayant provoqué l’exception.
    /// </summary>
    public string Component { get; }
    /// <summary>
    /// Chemin local attendu pour le composant portable manquant.
    /// </summary>
    public string ExpectedPath { get; }

/// <summary>
/// Cr?e une erreur explicite indiquant quel composant portable est absent et quel chemin ?tait attendu, afin de guider l?utilisateur vers l?installation ou la r?paration.
/// </summary>
    public PortableComponentMissingException(string component, string path)
        : base($"{component} portable n'est pas installé.\nChemin attendu : {path}")
    {
        Component = component;
        ExpectedPath = path;
    }
}
