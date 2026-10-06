/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Seconde passe locale pour le preset Qualité maximale.
EN: Local second pass for the Maximum quality preset.
*/

using System.Diagnostics;
using System.Text;

namespace OpenCodeLocalAI;

internal sealed record QualitySharpnessPreview(
    string BeforePath,
    string AfterPath);

internal sealed class QualityPostProcessor
{
    private const double MaximumQualityScale = 2.0;

    private readonly AppSettings _settings;
    private readonly Action<string, string> _log;

    public QualityPostProcessor(
        AppSettings settings,
        Action<string, string> log)
    {
        _settings = settings;
        _log = log;
    }

    public Task<string> EnhanceImageAsync(
        string sourcePath,
        CancellationToken cancellationToken) =>
        RunAsync(
            "image",
            sourcePath,
            PortablePaths.Resolve(_settings.Images),
            ".png",
            _settings.MaximumQualityImageSharpnessPercent,
            cancellationToken);

    public Task<string> EnhanceVideoAsync(
        string sourcePath,
        CancellationToken cancellationToken) =>
        RunAsync(
            "video",
            sourcePath,
            PortablePaths.Resolve(_settings.Videos),
            ".mp4",
            _settings.MaximumQualityVideoSharpnessPercent,
            cancellationToken);

    public Task<QualitySharpnessPreview> CreateImageSharpnessPreviewAsync(
        string sourcePath,
        int sharpnessPercent,
        CancellationToken cancellationToken) =>
        CreateSharpnessPreviewAsync(
            "image",
            sourcePath,
            sharpnessPercent,
            cancellationToken);

    public Task<QualitySharpnessPreview> CreateVideoSharpnessPreviewAsync(
        string sourcePath,
        int sharpnessPercent,
        CancellationToken cancellationToken) =>
        CreateSharpnessPreviewAsync(
            "video",
            sourcePath,
            sharpnessPercent,
            cancellationToken);

    private async Task<QualitySharpnessPreview> CreateSharpnessPreviewAsync(
        string mode,
        string sourcePath,
        int sharpnessPercent,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source de l'aperçu introuvable.", sourcePath);

        var python = PortablePaths.Resolve(_settings.ComfyPython);
        if (!File.Exists(python))
            throw new FileNotFoundException("Python portable ComfyUI introuvable.", python);

        var token = Guid.NewGuid().ToString("N");
        var beforePath = Path.Combine(
            Path.GetTempPath(),
            $"DreamRaster_sharpness_before_{token}.png");
        var afterPath = Path.Combine(
            Path.GetTempPath(),
            $"DreamRaster_sharpness_after_{token}.png");
        var scriptPath = Path.Combine(
            Path.GetTempPath(),
            $"DreamRaster_sharpness_preview_{token}.py");

        await File.WriteAllTextAsync(
            scriptPath,
            PreviewPythonScript,
            new UTF8Encoding(false),
            cancellationToken);

        try
        {
            var startInfo = new ProcessStartInfo(python)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = PortablePaths.Root
            };

            startInfo.ArgumentList.Add(scriptPath);
            startInfo.ArgumentList.Add(mode);
            startInfo.ArgumentList.Add(sourcePath);
            startInfo.ArgumentList.Add(beforePath);
            startInfo.ArgumentList.Add(afterPath);
            startInfo.ArgumentList.Add(
                MaximumQualityScale.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            startInfo.ArgumentList.Add(
                Math.Clamp(sharpnessPercent, 0, 200).ToString(
                    System.Globalization.CultureInfo.InvariantCulture));

            using var process =
                Process.Start(startInfo)
                ?? throw new InvalidOperationException(
                    "Impossible de démarrer l'aperçu de netteté.");

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch
                {
                }

                throw;
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0 ||
                !File.Exists(beforePath) ||
                !File.Exists(afterPath))
            {
                throw new InvalidOperationException(
                    "Échec de l'aperçu de netteté : " +
                    (string.IsNullOrWhiteSpace(stderr)
                        ? $"code {process.ExitCode}"
                        : stderr.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(stdout))
                _log("Aperçu netteté", stdout.Trim());

            return new QualitySharpnessPreview(beforePath, afterPath);
        }
        catch
        {
            TryDelete(beforePath);
            TryDelete(afterPath);
            throw;
        }
        finally
        {
            TryDelete(scriptPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private async Task<string> RunAsync(
        string mode,
        string sourcePath,
        string outputDirectory,
        string extension,
        int sharpnessPercent,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException(
                "Le média de la première passe est introuvable.",
                sourcePath);

        var python = PortablePaths.Resolve(_settings.ComfyPython);
        if (!File.Exists(python))
        {
            throw new FileNotFoundException(
                "Python portable ComfyUI introuvable pour la seconde passe.",
                python);
        }

        Directory.CreateDirectory(outputDirectory);

        var stem = Path.GetFileNameWithoutExtension(sourcePath);
        var destination = Path.Combine(
            outputDirectory,
            $"{stem}_MAXQ_PASS2_{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ss-fffZ}{extension}");

        var scriptPath = Path.Combine(
            Path.GetTempPath(),
            "DreamRaster_max_quality_" +
            Guid.NewGuid().ToString("N") +
            ".py");

        await File.WriteAllTextAsync(
            scriptPath,
            PythonScript,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);

        try
        {
            _log(
                "Qualité max",
                mode == "image"
                    ? $"Passe 2/2 : upscale image Lanczos x2 + netteté {Math.Clamp(sharpnessPercent, 0, 200)} %."
                    : $"Passe 2/2 : upscale vidéo Lanczos x2 + netteté {Math.Clamp(sharpnessPercent, 0, 200)} % + H.264 CRF 16.");

            var startInfo = new ProcessStartInfo(python)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = PortablePaths.Root
            };

            startInfo.ArgumentList.Add(scriptPath);
            startInfo.ArgumentList.Add(mode);
            startInfo.ArgumentList.Add(sourcePath);
            startInfo.ArgumentList.Add(destination);
            startInfo.ArgumentList.Add(
                MaximumQualityScale.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            startInfo.ArgumentList.Add(
                Math.Clamp(sharpnessPercent, 0, 200).ToString(
                    System.Globalization.CultureInfo.InvariantCulture));

            using var process =
                Process.Start(startInfo)
                ?? throw new InvalidOperationException(
                    "Impossible de démarrer le post-traitement Qualité maximale.");

            var standardOutputTask =
                process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask =
                process.StandardError.ReadToEndAsync(cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch
                {
                }

                throw;
            }

            var standardOutput = await standardOutputTask;
            var standardError = await standardErrorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    "Échec de la seconde passe Qualité maximale : " +
                    (string.IsNullOrWhiteSpace(standardError)
                        ? $"code {process.ExitCode}"
                        : standardError.Trim()));
            }

            if (!File.Exists(destination))
            {
                throw new FileNotFoundException(
                    "La seconde passe n'a pas produit de fichier final.",
                    destination);
            }

            if (!string.IsNullOrWhiteSpace(standardOutput))
            {
                _log(
                    "Qualité max",
                    standardOutput.Trim());
            }

            return destination;
        }
        catch
        {
            try
            {
                if (File.Exists(destination))
                    File.Delete(destination);
            }
            catch
            {
            }

            throw;
        }
        finally
        {
            try
            {
                if (File.Exists(scriptPath))
                    File.Delete(scriptPath);
            }
            catch
            {
            }
        }
    }

    private const string PreviewPythonScript = """
import sys
from pathlib import Path

from PIL import Image, ImageFilter

mode = sys.argv[1].strip().lower()
source = Path(sys.argv[2])
before_path = Path(sys.argv[3])
after_path = Path(sys.argv[4])
scale = float(sys.argv[5])
sharpness = max(0, min(200, int(sys.argv[6])))

def even(value):
    return max(2, int(round(value / 2.0) * 2))

def source_image():
    if mode == "image":
        with Image.open(source) as image:
            return image.convert("RGB").copy()

    if mode == "video":
        import av
        container = av.open(str(source))
        try:
            frame = next(container.decode(video=0))
            return frame.to_image().convert("RGB")
        finally:
            container.close()

    raise ValueError(f"unsupported preview mode: {mode}")

image = source_image()
width = even(image.width * scale)
height = even(image.height * scale)
before = image.resize((width, height), Image.Resampling.LANCZOS)
after = before.copy()
if sharpness > 0:
    after = after.filter(
        ImageFilter.UnsharpMask(
            radius=1.1 if mode == "image" else 1.0,
            percent=sharpness,
            threshold=3))

for preview, path in ((before, before_path), (after, after_path)):
    preview.thumbnail((900, 600), Image.Resampling.LANCZOS)
    preview.save(path, format="PNG", optimize=True)

print(
    f"Aperçu {mode} : netteté {sharpness} % · "
    f"{image.width}x{image.height} -> {width}x{height}.")
""";

    private const string PythonScript = """
import sys
from pathlib import Path

from PIL import Image, ImageFilter

mode = sys.argv[1].strip().lower()
source = Path(sys.argv[2])
destination = Path(sys.argv[3])
scale = float(sys.argv[4])
sharpness = max(0, min(200, int(sys.argv[5])))

if scale <= 1.0:
    raise ValueError("scale must be greater than 1")

destination.parent.mkdir(parents=True, exist_ok=True)

def even(value):
    return max(2, int(round(value / 2.0) * 2))

def enhance_image(image, width, height, radius, percent):
    result = image.convert("RGB").resize(
        (width, height),
        Image.Resampling.LANCZOS)
    return result.filter(
        ImageFilter.UnsharpMask(
            radius=radius,
            percent=percent,
            threshold=3))

if mode == "image":
    with Image.open(source) as image:
        width = even(image.width * scale)
        height = even(image.height * scale)
        result = enhance_image(
            image,
            width,
            height,
            radius=1.15,
            percent=sharpness)
        result.save(
            destination,
            format="PNG",
            optimize=True)
        print(
            f"Image passe 2 : {image.width}x{image.height} -> "
            f"{width}x{height}.")

elif mode == "video":
    import av

    input_container = av.open(str(source))
    try:
        video_stream = input_container.streams.video[0]
        rate = video_stream.average_rate or 16

        source_width = int(video_stream.codec_context.width)
        source_height = int(video_stream.codec_context.height)
        width = even(source_width * scale)
        height = even(source_height * scale)

        output_container = av.open(str(destination), mode="w")
        try:
            output_stream = output_container.add_stream(
                "libx264",
                rate=rate)
            output_stream.width = width
            output_stream.height = height
            output_stream.pix_fmt = "yuv420p"
            output_stream.options = {
                "crf": "16",
                "preset": "slow"
            }

            frame_count = 0
            for frame in input_container.decode(video=0):
                image = enhance_image(
                    frame.to_image(),
                    width,
                    height,
                    radius=1.0,
                    percent=sharpness)

                output_frame = av.VideoFrame.from_image(image)

                for packet in output_stream.encode(output_frame):
                    output_container.mux(packet)

                frame_count += 1

            for packet in output_stream.encode():
                output_container.mux(packet)
        finally:
            output_container.close()

        print(
            f"Vidéo passe 2 : {source_width}x{source_height} -> "
            f"{width}x{height} · {frame_count} frame(s).")
    finally:
        input_container.close()

else:
    raise ValueError(f"unsupported mode: {mode}")
""";
}
