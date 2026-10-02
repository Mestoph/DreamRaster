/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Correctifs de compatibilité ciblés pour le runtime ComfyUI portable.
EN: Targeted compatibility fixes for the portable ComfyUI runtime.
*/

namespace OpenCodeLocalAI;

internal static class ComfyWindowsCompatibility
{
    private const string SupportedVersion = "0.38.0";

    private const string PatchMarker =
        "# DreamRaster stability patch: bypass AIMDO direct file-to-GPU on Windows.";

    public static bool ApplyAimdoDirectReadPatchIfNeeded(
        AppSettings settings,
        Action<string, string> log)
    {
        try
        {
            var comfyRoot = PortablePreflight.GetComfyRoot(settings);
            var versionPath = Path.Combine(comfyRoot, "comfyui_version.py");
            var memoryManagementPath = Path.Combine(
                comfyRoot,
                "comfy",
                "memory_management.py");

            if (!File.Exists(versionPath) ||
                !File.Exists(memoryManagementPath))
            {
                return false;
            }

            var versionText = File.ReadAllText(versionPath);
            if (!versionText.Contains(
                    $"__version__ = \"{SupportedVersion}\"",
                    StringComparison.Ordinal))
            {
                return false;
            }

            var source = File.ReadAllText(memoryManagementPath);

            if (source.Contains(PatchMarker, StringComparison.Ordinal))
            {
                log(
                    "ComfyUI",
                    $"Patch stabilité Windows AIMDO déjà présent pour ComfyUI {SupportedVersion}.");
                return true;
            }

            const string original =
                "    if destination is None:\n" +
                "        stream_ptr = getattr(stream, \"cuda_stream\", 0) if stream is not None else 0\n" +
                "        comfy_aimdo.host_buffer.read_file_to_device(file_obj, info.offset, info.size,\n" +
                "                                                    stream_ptr, destination2.data_ptr(),\n" +
                "                                                    destination2.device.index,\n" +
                "                                                    mark_cold=False)\n" +
                "        return True\n";

            const string conditionalPatch =
                "    if destination is None:\n" +
                "        # DreamRaster stability patch: use AIMDO direct file-to-GPU when possible,\n" +
                "        # but fall back to ComfyUI's standard tensor.copy_() path if AIMDO fails.\n" +
                "        stream_ptr = getattr(stream, \"cuda_stream\", 0) if stream is not None else 0\n" +
                "        try:\n" +
                "            comfy_aimdo.host_buffer.read_file_to_device(file_obj, info.offset, info.size,\n" +
                "                                                        stream_ptr, destination2.data_ptr(),\n" +
                "                                                        destination2.device.index,\n" +
                "                                                        mark_cold=False)\n" +
                "            return True\n" +
                "        except RuntimeError:\n" +
                "            return False\n";

            const string olderPatch =
                "    if destination is None:\n" +
                "        # DreamRaster stability patch for ComfyUI 0.38.0 on Windows:\n" +
                "        # bypass AIMDO direct file-to-GPU reads and fall back to tensor.copy_().\n" +
                "        return False\n";

            const string patched =
                "    if destination is None:\n" +
                $"        {PatchMarker}\n" +
                "        # The split FLUX workflow frees the large models before VAE decode.\n" +
                "        return False\n";

            string replacementTarget;
            if (source.Contains(conditionalPatch, StringComparison.Ordinal))
                replacementTarget = conditionalPatch;
            else if (source.Contains(olderPatch, StringComparison.Ordinal))
                replacementTarget = olderPatch;
            else if (source.Contains(original, StringComparison.Ordinal))
                replacementTarget = original;
            else
            {
                log(
                    "ComfyUI !",
                    $"ComfyUI {SupportedVersion} détecté mais le bloc AIMDO attendu a changé ; patch non appliqué.");
                return false;
            }

            var backupPath = memoryManagementPath + ".dreamraster-original";
            if (!File.Exists(backupPath))
                File.Copy(memoryManagementPath, backupPath, overwrite: false);

            source = source.Replace(
                replacementTarget,
                patched,
                StringComparison.Ordinal);

            File.WriteAllText(
                memoryManagementPath,
                source,
                new System.Text.UTF8Encoding(false));

            log(
                "ComfyUI",
                $"Patch stabilité Windows AIMDO appliqué à ComfyUI {SupportedVersion}.");

            return true;
        }
        catch (Exception ex)
        {
            log(
                "ComfyUI !",
                "Patch stabilité Windows AIMDO : " + ex.Message);
            return false;
        }
    }
}
