/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Contrôles de compatibilité ciblés pour le runtime ComfyUI portable.
EN: Targeted compatibility checks for the portable ComfyUI runtime.
*/

namespace OpenCodeLocalAI;

internal static class ComfyWindowsCompatibility
{
    private const string SupportedVersion = "0.38.0";
    private const string LegacyPatchMarker =
        "# DreamRaster stability patch: bypass AIMDO direct file-to-GPU on Windows.";

    public static bool CheckAimdoCompatibility(
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

            if (!File.Exists(versionPath) || !File.Exists(memoryManagementPath))
                return false;

            var versionText = File.ReadAllText(versionPath);
            if (!versionText.Contains(
                    $"__version__ = \"{SupportedVersion}\"",
                    StringComparison.Ordinal))
            {
                return false;
            }

            var source = File.ReadAllText(memoryManagementPath);
            var normalized = source.Replace("\r\n", "\n", StringComparison.Ordinal);

            if (normalized.Contains(LegacyPatchMarker, StringComparison.Ordinal))
            {
                log(
                    "ComfyUI ⚠",
                    $"ComfyUI {SupportedVersion} contient encore l'ancien patch DreamRaster AIMDO. " +
                    "Le runtime reste utilisable, mais une installation/réparation ComfyUI propre est recommandée pour revenir au code natif.");
                return true;
            }

            const string nativeAimdoBlock =
                "    if destination is None:\n" +
                "        stream_ptr = getattr(stream, \"cuda_stream\", 0) if stream is not None else 0\n" +
                "        comfy_aimdo.host_buffer.read_file_to_device(file_obj, info.offset, info.size,\n" +
                "                                                    stream_ptr, destination2.data_ptr(),\n" +
                "                                                    destination2.device.index,\n" +
                "                                                    mark_cold=False)\n" +
                "        return True\n";

            if (normalized.Contains(nativeAimdoBlock, StringComparison.Ordinal))
            {
                log(
                    "ComfyUI",
                    $"ComfyUI {SupportedVersion} : chemin AIMDO natif reconnu, aucun patch DreamRaster nécessaire.");
                return false;
            }

            log(
                "ComfyUI ⚠",
                $"ComfyUI {SupportedVersion} : structure AIMDO inconnue. Aucun fichier ComfyUI n'a été modifié ; " +
                "vérifier le runtime si des erreurs de chargement GPU apparaissent.");
            return false;
        }
        catch (Exception ex)
        {
            log(
                "ComfyUI ⚠",
                "Vérification de compatibilité AIMDO : " + ex.Message);
            return false;
        }
    }
}
