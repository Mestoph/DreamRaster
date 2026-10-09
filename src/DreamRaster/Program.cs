/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later


Point d'entrée, gestion globale des erreurs et loader WebView2.
Les commentaires structurants sont r?dig?s en fran?ais. Les noms d'API, classes et protocoles
     restent dans leur forme technique afin de garder le code lisible et compatible.
*/

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenCodeLocalAI;

/// <summary>

/// Définit class « Program », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

/// </summary>
internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.ThreadException += (_, e) =>
        {
            CrashLog.Write("UI ThreadException", e.Exception);
            MessageBox.Show(
                "Une erreur a été interceptée sans fermer l'application.\n\n" + e.Exception.Message,
                BrandInfo.ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                CrashLog.Write("UnhandledException", ex);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            CrashLog.Write("UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

        PortablePaths.EnsureLayout();
        ConfigureNativeLibraries();

        if (args.Any(a => a.Equals("--diagnostic", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                DiagnosticExporter.ExportAsync(SettingsStore.Load()).GetAwaiter().GetResult();
                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                CrashLog.Write("Diagnostic", ex);
                Environment.ExitCode = 1;
            }
            return;
        }

        Application.Run(new MainForm());
    }
    /// <summary>
    /// Configure les données ou le workflow géré par <c>ConfigureNativeLibraries</c> selon les réglages actifs.
    /// </summary>
    private static void ConfigureNativeLibraries()
    {
        try
        {
            var portableLoader = EnsureEmbeddedWebView2Loader();

            var webViewAssembly =
                typeof(CoreWebView2Environment).Assembly;

            NativeLibrary.SetDllImportResolver(
                webViewAssembly,
                (libraryName, assembly, searchPath) =>
                {
                    if (!libraryName.Equals(
                            "WebView2Loader.dll",
                            StringComparison.OrdinalIgnoreCase) &&
                        !libraryName.Equals(
                            "WebView2Loader",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return IntPtr.Zero;
                    }

                    if (!File.Exists(portableLoader))
                        return IntPtr.Zero;

                    return NativeLibrary.Load(portableLoader);
                });
        }
        catch (InvalidOperationException)
        {
            // Un resolver a déjà été enregistré pour cette assembly.
        }
        catch (Exception ex)
        {
            CrashLog.Write("NativeResolver", ex);
        }
    }

    /// <summary>

    /// Vérifie puis garantit la condition requise par <c>EnsureEmbeddedWebView2Loader</c> avant de poursuivre.

    /// </summary>
    private static string EnsureEmbeddedWebView2Loader()
    {
        var targetDir = Path.Combine(
            PortablePaths.RuntimeDir,
            "native");

        Directory.CreateDirectory(targetDir);

        var target = Path.Combine(
            targetDir,
            "WebView2Loader.dll");

        if (File.Exists(target) &&
            new FileInfo(target).Length > 0)
        {
            return target;
        }

        const string resourceName =
            "OpenCodeLocalAI.Resources.WebView2Loader.dll";

        using var input =
            typeof(Program).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                "Ressource WebView2Loader.dll intégrée introuvable.");

        var temp = target + ".tmp";

        try
        {
            using (var output = new FileStream(
                temp,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                input.CopyTo(output);
                output.Flush(flushToDisk: true);
            }

            File.Move(
                temp,
                target,
                overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch { }
        }

        return target;
    }

}

/// <summary>

/// Définit class « CrashLog », utilisé par DreamRaster pour encapsuler cette responsabilité fonctionnelle.

/// </summary>
internal static class CrashLog
{
    /// <summary>
    /// Écrit les données gérées par <c>Write</c> vers leur destination.
    /// </summary>
    public static void Write(string source, Exception ex)
    {
        try
        {
            PortablePaths.EnsureLayout();
            File.AppendAllText(
                Path.Combine(PortablePaths.LogsDir, "crash.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{source}]\r\n{ex}\r\n\r\n");
        }
        catch { }
    }
}
