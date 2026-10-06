using System.Collections;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenCodeLocalAI;

namespace DreamRaster.Tests;

[TestClass]
public sealed class GenerationTemplateTests
{
    private static readonly Type TemplatesType =
        typeof(MainForm).Assembly.GetType(
            "OpenCodeLocalAI.GenerationTemplates",
            throwOnError: true)!;

    [TestMethod]
    public void TemplateIds_AreUniqueWithinEachCollection()
    {
        AssertUniqueIds("ImageStyles");
        AssertUniqueIds("ImageNegatives");
        AssertUniqueIds("VideoStyles");
        AssertUniqueIds("VideoNegatives");
    }

    [TestMethod]
    public void SafetyPresets_BlockExplicitSexualAndGraphicContent()
    {
        var imageSafe = FindById("ImageNegatives", "safe_quality");
        var videoSafe = FindById("VideoNegatives", "safe_stable");

        var imageNegative = GetString(imageSafe, "NegativePrompt");
        var videoNegative = GetString(videoSafe, "NegativePrompt");

        foreach (var required in new[]
                 {
                     "pornography",
                     "explicit sexual content",
                     "sexualized minor",
                     "child sexual content",
                     "graphic gore"
                 })
        {
            StringAssert.Contains(
                imageNegative,
                required,
                $"Le preset image de sécurité doit contenir '{required}'.");

            StringAssert.Contains(
                videoNegative,
                required,
                $"Le preset vidéo de sécurité doit contenir '{required}'.");
        }

        Assert.IsTrue(
            GetBool(imageSafe, "IncludeStyleNegative"),
            "Le preset image sécurisé doit conserver le négatif du style.");

        Assert.IsTrue(
            GetBool(videoSafe, "IncludeStyleNegative"),
            "Le preset vidéo sécurisé doit conserver le négatif du style.");
    }

    [TestMethod]
    public void SafetyPreset_MergesCustomStyleAndSafetyNegatives()
    {
        var style = FindById("ImageStyles", "portrait-premium");
        var safe = FindById("ImageNegatives", "safe_quality");

        var method = TemplatesType.GetMethod(
            "MergeNegative",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new AssertFailedException("MergeNegative introuvable.");

        var merged = (string?)method.Invoke(
            null,
            ["custom defect", safe, style]);

        Assert.IsFalse(string.IsNullOrWhiteSpace(merged));
        StringAssert.Contains(merged!, "custom defect");
        StringAssert.Contains(merged!, "waxy skin");
        StringAssert.Contains(merged!, "pornography");
        StringAssert.Contains(merged!, "graphic gore");
    }

    [TestMethod]
    public void ConcreteObjectivePresets_HaveCoherentAppliedSettings()
    {
        var requiredImage = new[]
        {
            "photorealistic",
            "portrait",
            "product",
            "landscape",
            "anime",
            "cinematic",
            "lowlight",
            "photo4k",
            "max-quality"
        };

        foreach (var id in requiredImage)
        {
            var settings = FindSettings("ImageObjectiveSettings", id);
            Assert.IsTrue(GetInt(settings, "Width") >= 512, id);
            Assert.IsTrue(GetInt(settings, "Height") >= 512, id);
            Assert.IsTrue(GetInt(settings, "Steps") >= 4, id);
            Assert.IsTrue(GetDouble(settings, "Cfg") > 0, id);
        }

        var requiredVideo = new[]
        {
            "photorealistic",
            "portrait",
            "product",
            "landscape",
            "anime",
            "cinematic",
            "lowlight",
            "4k",
            "max-quality"
        };

        foreach (var id in requiredVideo)
        {
            var settings = FindSettings("VideoObjectiveSettings", id);
            Assert.IsTrue(GetInt(settings, "Width") >= 480, id);
            Assert.IsTrue(GetInt(settings, "Height") >= 480, id);
            Assert.IsTrue(GetInt(settings, "Frames") >= 33, id);
            Assert.IsTrue(GetInt(settings, "Steps") >= 40, id);
            Assert.AreEqual("uni_pc", GetString(settings, "Sampler"), id);
            Assert.AreEqual("simple", GetString(settings, "Scheduler"), id);
        }

        var video4k = FindSettings("VideoObjectiveSettings", "4k");
        Assert.AreEqual(1280, GetInt(video4k, "Width"));
        Assert.AreEqual(720, GetInt(video4k, "Height"));
        Assert.AreEqual("best", GetString(video4k, "QualityPreset"));
    }

    [TestMethod]
    public void MaximumQuality_FirstPassUsesStableNativeResolution()
    {
        var image = FindSettings("ImageObjectiveSettings", "max-quality");
        Assert.AreEqual(1024, GetInt(image, "Width"));
        Assert.AreEqual(1024, GetInt(image, "Height"));
        Assert.AreEqual(8, GetInt(image, "Steps"));

        var video = FindSettings("VideoObjectiveSettings", "max-quality");
        Assert.AreEqual(832, GetInt(video, "Width"));
        Assert.AreEqual(480, GetInt(video, "Height"));
        Assert.AreEqual(33, GetInt(video, "Frames"));
        Assert.AreEqual(50, GetInt(video, "Steps"));
        Assert.AreEqual("best", GetString(video, "QualityPreset"));
    }

    [TestMethod]
    public void TwoPassMode_IsExclusiveToMaximumQuality()
    {
        var method = TemplatesType.GetMethod(
            "UsesTwoPassMaximumQuality",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new AssertFailedException(
                "UsesTwoPassMaximumQuality introuvable.");

        foreach (var propertyName in new[] { "ImageStyles", "VideoStyles" })
        {
            foreach (var preset in GetItems(propertyName))
            {
                var id = GetString(preset, "Id");
                var enabled =
                    (bool)(method.Invoke(null, [id]) ?? false);

                Assert.AreEqual(
                    id.Equals(
                        "max-quality",
                        StringComparison.OrdinalIgnoreCase),
                    enabled,
                    $"{propertyName}/{id}");
            }
        }

        Assert.IsFalse(
            (bool)(method.Invoke(null, [null]) ?? false));
    }

    [TestMethod]
    public void NewSettings_DefaultToSafeNegativePresets()
    {
        var settings = new AppSettings();

        Assert.AreEqual("safe_quality", settings.ImageNegativeTemplate);
        Assert.AreEqual("safe_stable", settings.VideoNegativeTemplate);
        Assert.AreEqual(75, settings.MaximumQualityImageSharpnessPercent);
        Assert.AreEqual(65, settings.MaximumQualityVideoSharpnessPercent);
    }

    private static void AssertUniqueIds(string propertyName)
    {
        var ids = GetItems(propertyName)
            .Select(item => GetString(item, "Id"))
            .ToArray();

        Assert.IsTrue(ids.Length > 0, $"{propertyName} ne doit pas être vide.");

        var duplicates = ids
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.AreEqual(
            0,
            duplicates.Length,
            $"IDs dupliqués dans {propertyName}: {string.Join(", ", duplicates)}");
    }

    private static object FindSettings(string propertyName, string id)
    {
        var property = TemplatesType.GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new AssertFailedException(
                $"Propriété {propertyName} introuvable.");

        var dictionary = property.GetValue(null)
            ?? throw new AssertFailedException(
                $"{propertyName} est vide.");

        var tryGetValue = dictionary.GetType().GetMethod("TryGetValue")
            ?? throw new AssertFailedException(
                $"TryGetValue introuvable pour {propertyName}.");

        var args = new object?[] { id, null };
        var found = (bool)(tryGetValue.Invoke(dictionary, args) ?? false);

        return found && args[1] is not null
            ? args[1]!
            : throw new AssertFailedException(
                $"Réglages '{id}' introuvables dans {propertyName}.");
    }

    private static int GetInt(object instance, string propertyName) =>
        (int)(instance.GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(instance)
        ?? throw new AssertFailedException(
            $"Propriété entière {propertyName} introuvable."));

    private static double GetDouble(object instance, string propertyName) =>
        Convert.ToDouble(
            instance.GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(instance)
            ?? throw new AssertFailedException(
                $"Propriété numérique {propertyName} introuvable."));

    private static object FindById(string propertyName, string id)
    {
        var item = GetItems(propertyName)
            .FirstOrDefault(value =>
                GetString(value, "Id")
                    .Equals(id, StringComparison.OrdinalIgnoreCase));

        return item
            ?? throw new AssertFailedException(
                $"Preset '{id}' introuvable dans {propertyName}.");
    }

    private static IEnumerable<object> GetItems(string propertyName)
    {
        var property = TemplatesType.GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new AssertFailedException(
                $"Propriété {propertyName} introuvable.");

        var enumerable = property.GetValue(null) as IEnumerable
            ?? throw new AssertFailedException(
                $"{propertyName} n'est pas énumérable.");

        return enumerable.Cast<object>();
    }

    private static string GetString(object instance, string propertyName) =>
        instance.GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(instance) as string
        ?? throw new AssertFailedException(
            $"Propriété texte {propertyName} introuvable.");

    private static bool GetBool(object instance, string propertyName) =>
        instance.GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(instance) as bool?
        ?? throw new AssertFailedException(
            $"Propriété booléenne {propertyName} introuvable.");
}
