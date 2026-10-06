using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenCodeLocalAI;

namespace DreamRaster.Tests;

[TestClass]
[DoNotParallelize]
public sealed class V38UiTests
{
    [TestMethod]
    public void Configuration_PolishPanel_ContainsOnlyTechnicalInterfaces()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            Invoke(form, "InitializeUiPolishV37");

            var panel = GetField<Panel>(
                form,
                "_configurationAdvancedPanelPolish");

            var names = panel.Controls
                .Cast<Control>()
                .Select(control => control.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();

            CollectionAssert.Contains(
                names,
                "cfgShowOpenCodeTabPolish");
            CollectionAssert.Contains(
                names,
                "cfgShowComfyTabPolish");

            Assert.IsFalse(
                names.Any(name =>
                    name.StartsWith(
                        "cfgImage",
                        StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith(
                        "cfgVideo",
                        StringComparison.OrdinalIgnoreCase)),
                "Configuration ne doit pas dupliquer les réglages créatifs Image/Vidéo.");
        });

    [TestMethod]
    public void Configuration_V38Layout_FitsCompactWorkspace()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            Invoke(form, "InitializeUiPolishV37");

            var tabs = GetField<TabControl>(
                form,
                "_tabs");
            var page = GetField<TabPage>(
                form,
                "tabConfiguration");

            form.Size = new Size(900, 620);
            form.PerformLayout();

            tabs.SetBounds(
                0,
                0,
                form.ClientSize.Width,
                form.ClientSize.Height);
            tabs.PerformLayout();

            var display = tabs.DisplayRectangle;
            page.SetBounds(
                display.X,
                display.Y,
                display.Width,
                display.Height);
            tabs.SelectedTab = page;
            page.PerformLayout();

            Invoke(form, "ApplyV37SharedLayout");
            Invoke(form, "ApplyUiPolishLayout");

            var workspace = page.ClientSize;

            foreach (Control control in page.Controls)
            {
                if (!control.Visible ||
                    control.Width <= 0 ||
                    control.Height <= 0)
                {
                    continue;
                }

                Assert.IsTrue(
                    control.Left >= -2 &&
                    control.Top >= -2 &&
                    control.Right <= workspace.Width + 2 &&
                    control.Bottom <= workspace.Height + 2,
                    $"Contrôle Configuration hors zone : " +
                    $"{control.Name} {control.Bounds} / {workspace}.");
            }
        });

    [TestMethod]
    public void ImageAndVideo_V38CatalogRows_AreAligned()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            Invoke(form, "InitializeUiPolishV37");
            Invoke(form, "ApplyV37SharedLayout");
            Invoke(form, "ApplyUiPolishLayout");

            var imageModel = GetField<ComboBox>(
                form,
                "_imageModelRuntimeCombo");
            var videoModel = GetField<ComboBox>(
                form,
                "_videoModelRuntimeCombo");
            var imageStyle = GetField<ComboBox>(
                form,
                "_imageStyleTemplateCombo");
            var videoStyle = GetField<ComboBox>(
                form,
                "_videoStyleTemplateCombo");
            var imageLora = GetField<ComboBox>(
                form,
                "_imageLoraCombo");
            var videoLora = GetField<ComboBox>(
                form,
                "_videoLoraCombo");

            Assert.AreEqual(
                imageModel.Top,
                videoModel.Top,
                "La rangée Modèle doit être alignée.");
            Assert.AreEqual(
                imageStyle.Top,
                videoStyle.Top,
                "La rangée Style doit être alignée.");
            Assert.AreEqual(
                imageLora.Top,
                videoLora.Top,
                "La rangée LoRA doit être alignée.");

            Assert.AreEqual(
                imageModel.Left,
                videoModel.Left,
                "Les sélecteurs Modèle doivent partager la même colonne.");
            Assert.AreEqual(
                imageStyle.Left,
                videoStyle.Left,
                "Les sélecteurs Style doivent partager la même colonne.");
            Assert.AreEqual(
                imageLora.Left,
                videoLora.Left,
                "Les sélecteurs LoRA doivent partager la même colonne.");
        });

    private static void Invoke(
        MainForm form,
        string methodName)
    {
        var method = typeof(MainForm).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(
                typeof(MainForm).FullName,
                methodName);

        method.Invoke(form, null);
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
            Name = "DreamRaster v38 UI"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!thread.Join(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException(
                "Le test UI v38 n'a pas terminé dans le délai prévu.");
        }

        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
