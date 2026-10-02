/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Validation légère des modèles FLUX.2 Klein à partir des en-têtes safetensors.
EN: Lightweight FLUX.2 Klein model validation from safetensors headers.
*/

using System.Text;
using System.Text.Json;

namespace OpenCodeLocalAI;

internal enum ModelCompatibilityState
{
    Compatible,
    Unverified,
    Incompatible,
    Missing
}

internal enum Flux2ModelRole
{
    Diffusion,
    TextEncoder,
    Vae
}

internal sealed record ModelCompatibilityResult(
    ModelCompatibilityState State,
    string Reason);

internal sealed record ModelChoice(
    string FileName,
    ModelCompatibilityState State,
    string Display)
{
    public override string ToString() => Display;
}

internal static class Flux2ModelCompatibility
{
    private const ulong MaxHeaderBytes = 64UL * 1024UL * 1024UL;

    public static ModelCompatibilityResult Validate(
        string path,
        Flux2ModelRole role)
    {
        if (!File.Exists(path))
            return new(ModelCompatibilityState.Missing, "fichier absent");

        if (!path.EndsWith(".safetensors", StringComparison.OrdinalIgnoreCase))
        {
            return new(
                ModelCompatibilityState.Unverified,
                "format détecté mais validation structurelle indisponible");
        }

        try
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var headerLength = reader.ReadUInt64();

            if (headerLength == 0 ||
                headerLength > MaxHeaderBytes ||
                headerLength > (ulong)Math.Max(0, stream.Length - 8))
            {
                return new(
                    ModelCompatibilityState.Incompatible,
                    "en-tête safetensors invalide");
            }

            var headerBytes = reader.ReadBytes((int)headerLength);
            using var doc = JsonDocument.Parse(headerBytes);
            var root = doc.RootElement;

            return role switch
            {
                Flux2ModelRole.Diffusion => ValidateDiffusion(root),
                Flux2ModelRole.TextEncoder => ValidateTextEncoder(root),
                Flux2ModelRole.Vae => ValidateVae(root),
                _ => new(ModelCompatibilityState.Unverified, "rôle inconnu")
            };
        }
        catch (Exception ex)
        {
            return new(
                ModelCompatibilityState.Incompatible,
                "lecture impossible : " + ex.Message);
        }
    }

    private static ModelCompatibilityResult ValidateDiffusion(JsonElement root)
    {
        var flux2Marker =
            HasTensor(root, "double_stream_modulation_img.lin.weight") &&
            HasAnyTensor(
                root,
                "double_blocks.0.img_attn.norm.key_norm.weight",
                "double_blocks.0.img_attn.norm.key_norm.scale") &&
            HasTensor(root, "img_in.weight");

        if (!flux2Marker)
        {
            return new(
                ModelCompatibilityState.Incompatible,
                "architecture FLUX.2 non détectée");
        }

        var klein4b =
            ShapeEquals(root, "img_in.weight", 3072, 128) &&
            ShapeEquals(root, "txt_in.weight", 3072, 7680) &&
            ShapeEquals(root, "final_layer.linear.weight", 128, 3072) &&
            CountBlocks(root, "double_blocks.") == 5 &&
            CountBlocks(root, "single_blocks.") == 20;

        return klein4b
            ? new(
                ModelCompatibilityState.Compatible,
                "signature FLUX.2 Klein 4B reconnue")
            : new(
                ModelCompatibilityState.Unverified,
                "architecture FLUX.2 reconnue, variante différente du Klein 4B validé");
    }

    private static ModelCompatibilityResult ValidateTextEncoder(JsonElement root)
    {
        if (!TryShape(
                root,
                "model.layers.0.post_attention_layernorm.weight",
                out var postShape) ||
            !HasTensor(root, "model.layers.0.self_attn.q_norm.weight"))
        {
            return new(
                ModelCompatibilityState.Incompatible,
                "encodeur Qwen3 compatible Klein non détecté");
        }

        var width = postShape.Length == 1 ? postShape[0] : -1;

        if (width is 2560 or 4096)
        {
            var family = width == 2560 ? "Qwen3 4B" : "Qwen3 8B";
            return new(
                ModelCompatibilityState.Compatible,
                family + " reconnu par le chemin Klein de ComfyUI");
        }

        if (width == 2048)
        {
            return new(
                ModelCompatibilityState.Incompatible,
                "Qwen3 2B détecté : ComfyUI ne l'utilise pas comme encodeur Klein");
        }

        if (width == 5120)
        {
            return new(
                ModelCompatibilityState.Incompatible,
                "encodeur FLUX.2 Mistral détecté, mais pas encodeur Klein");
        }

        return new(
            ModelCompatibilityState.Incompatible,
            "famille d'encodeur non reconnue pour FLUX.2 Klein");
    }

    private static ModelCompatibilityResult ValidateVae(JsonElement root)
    {
        var standardFlux2 =
            ShapeEquals(root, "decoder.conv_in.weight", 512, 32, 3, 3) &&
            ShapeEquals(root, "encoder.conv_out.weight", 64, 512, 3, 3) &&
            ShapeEquals(root, "bn.running_mean", 128) &&
            ShapeEquals(root, "bn.running_var", 128);

        if (standardFlux2)
        {
            return new(
                ModelCompatibilityState.Compatible,
                "VAE FLUX.2 128 canaux / downscale 16 reconnu");
        }

        if (HasTensor(root, "student.dconv_encoder.proj_out.weight"))
        {
            return new(
                ModelCompatibilityState.Unverified,
                "VAE alternatif à latents FLUX.2 détecté, non validé avec le workflow Klein");
        }

        if (TryShape(root, "decoder.conv_in.weight", out var decoderShape))
        {
            return new(
                ModelCompatibilityState.Incompatible,
                "VAE détecté avec dimensions incompatibles : [" +
                string.Join(", ", decoderShape) + "]");
        }

        return new(
            ModelCompatibilityState.Incompatible,
            "signature VAE FLUX.2 non détectée");
    }

    private static bool HasTensor(JsonElement root, string key)
        => root.TryGetProperty(key, out var value) &&
           value.ValueKind == JsonValueKind.Object;

    private static bool HasAnyTensor(JsonElement root, params string[] keys)
        => keys.Any(key => HasTensor(root, key));

    private static bool ShapeEquals(
        JsonElement root,
        string key,
        params int[] expected)
        => TryShape(root, key, out var actual) &&
           actual.SequenceEqual(expected);

    private static bool TryShape(
        JsonElement root,
        string key,
        out int[] shape)
    {
        shape = [];

        if (!root.TryGetProperty(key, out var tensor) ||
            tensor.ValueKind != JsonValueKind.Object ||
            !tensor.TryGetProperty("shape", out var shapeElement) ||
            shapeElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        try
        {
            shape = shapeElement
                .EnumerateArray()
                .Select(x => x.GetInt32())
                .ToArray();
            return true;
        }
        catch
        {
            shape = [];
            return false;
        }
    }

    private static int CountBlocks(JsonElement root, string prefix)
    {
        var blocks = new HashSet<int>();

        foreach (var property in root.EnumerateObject())
        {
            if (!property.Name.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            var rest = property.Name[prefix.Length..];
            var dot = rest.IndexOf('.');
            if (dot <= 0)
                continue;

            if (int.TryParse(rest[..dot], out var index))
                blocks.Add(index);
        }

        return blocks.Count;
    }
}
