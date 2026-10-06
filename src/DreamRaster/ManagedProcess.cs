/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Gestion sûre des processus enfants portables et de leurs logs.
EN: Safe management of portable child processes and their logs.

FR : Les commentaires structurants sont bilingues. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
EN: Structural comments are bilingual. API, class and protocol names remain in their
    technical form to keep the code readable and compatible.
*/

using System.Diagnostics;
using System.Text;

namespace OpenCodeLocalAI;

public sealed class ManagedProcess : IDisposable
{
    private readonly Action<string, string> _log;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private Process? _process;
    private int _expectedStopPid;

    public string Name { get; }
    public bool Running => _process is { HasExited: false };
    public Process? Process => _process;

    public ManagedProcess(string name, Action<string, string> log)
    {
        Name = name;
        _log = log;
    }

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

    private void LogStandardError(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        // ComfyUI/tqdm and some other CLIs use stderr for normal console output.
        // Severity is inferred from content, never from stderr alone.
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
        // Other child processes keep a visible warning marker for unclassified stderr.
        _log(
            Name.Equals("ComfyUI", StringComparison.OrdinalIgnoreCase)
                ? Name
                : Name + " ⚠",
            line);
    }

    private static bool ContainsAny(string text, params string[] needles) =>
        needles.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase));

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

public sealed class PortableComponentMissingException : Exception
{
    public string Component { get; }
    public string ExpectedPath { get; }

    public PortableComponentMissingException(string component, string path)
        : base($"{component} portable n'est pas installé.\nChemin attendu : {path}")
    {
        Component = component;
        ExpectedPath = path;
    }
}
