using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenCodeLocalAI;

namespace DreamRaster.Tests;

[TestClass]
[DoNotParallelize]
public sealed class UiPolishTests
{
    [TestMethod]
    public void TechnicalTabs_AreOptional_AndGenerationEditorsStayUsable()
        => RunOnSta(() =>
        {
            using var form = new MainForm();

            var settings = GetField<AppSettings>(form, "_s");
            settings.ShowOpenCodeTab = false;
            settings.ShowComfyUiTab = false;

            Invoke(form, "InitializeUiPolishV37");

            var tabs = GetField<TabControl>(form, "_tabs");
            var openCode = GetField<TabPage>(form, "tabOpenCode");
            var comfy = GetField<TabPage>(form, "tabComfy");
            var image = GetField<TabPage>(form, "tabGenerate");
            var video = GetField<TabPage>(form, "_tabVideo");
            var imagePrompt = GetField<TextBox>(form, "_prompt");
            var videoPrompt = GetField<TextBox>(form, "_videoPrompt");
            var imageGenerate = GetField<Button>(form, "btnGenerate");
            var videoGenerate =
                GetField<Button>(form, "_videoGenerateButton");

            Assert.IsFalse(
                tabs.TabPages.Contains(openCode),
                "OpenCode doit être masqué par défaut.");
            Assert.IsFalse(
                tabs.TabPages.Contains(comfy),
                "ComfyUI doit être masqué par défaut.");

            Invoke(
                form,
                "SetFeatureTab",
                image,
                false,
                "Image backend missing");
            Invoke(
                form,
                "SetFeatureTab",
                video,
                false,
                "Video backend missing");

            Assert.IsTrue(
                image.Enabled,
                "L'onglet Image doit rester accessible pour configurer les réglages.");
            Assert.IsTrue(
                video.Enabled,
                "L'onglet Vidéo doit rester accessible pour configurer les réglages.");
            Assert.IsTrue(
                imagePrompt.Enabled,
                "Le prompt Image ne doit pas être verrouillé parce qu'un modèle manque.");
            Assert.IsTrue(
                videoPrompt.Enabled,
                "Le prompt Vidéo ne doit pas être verrouillé parce qu'un modèle manque.");
            Assert.IsFalse(
                imageGenerate.Enabled,
                "Seule l'action Générer Image doit être bloquée si le backend manque.");
            Assert.IsFalse(
                videoGenerate.Enabled,
                "Seule l'action Générer Vidéo doit être bloquée si le backend manque.");
        });

    [TestMethod]
    public void TechnicalTabs_AreHiddenByDefaultInFreshSettings()
    {
        var settings = new AppSettings();

        Assert.IsFalse(settings.ShowOpenCodeTab);
        Assert.IsFalse(settings.ShowComfyUiTab);
    }

    private static void Invoke(
        MainForm form,
        string methodName,
        params object[] arguments)
    {
        var method = typeof(MainForm).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(
                typeof(MainForm).FullName,
                methodName);

        method.Invoke(form, arguments);
    }

    private static T GetField<T>(
        MainForm form,
        string name)
        where T : class
    {
        var field = typeof(MainForm).GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(
                typeof(MainForm).FullName,
                name);

        return field.GetValue(form) as T
            ?? throw new InvalidOperationException(
                $"{name} n'est pas un {typeof(T).Name}.");
    }

    private static void RunOnSta(Action scenario)
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                scenario();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        })
        {
            IsBackground = true,
            Name = "DreamRaster UI polish regression"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!thread.Join(TimeSpan.FromSeconds(30)))
            throw new TimeoutException(
                "Le test UI n'a pas terminé dans le délai prévu.");

        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
