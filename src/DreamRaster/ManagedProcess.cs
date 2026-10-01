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
    private Process? _process;

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
        if (Running) return;

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

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, e) => { if (e.Data is not null) _log(Name, e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) _log(Name + " !", e.Data); };
        _process.Exited += (_, _) => _log(Name, $"Processus terminé (code {_process?.ExitCode}).");

        if (!_process.Start())
            throw new InvalidOperationException($"Impossible de démarrer {Name}.");

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        _log(Name, $"PID {_process.Id} · {exe}");

        await Task.Delay(100, ct);
    }

    public async Task StopAsync(TimeSpan? grace = null)
    {
        var p = _process;
        if (p is null) return;

        try
        {
            if (!p.HasExited)
            {
                try
                {
                    if (p.MainWindowHandle != IntPtr.Zero)
                        p.CloseMainWindow();
                }
                catch { }

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
            _log(Name + " !", "Arrêt : " + ex.Message);
        }
        finally
        {
            p.Dispose();
            _process = null;
        }
    }

    public void Dispose()
    {
        try { StopAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult(); }
        catch { }
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
