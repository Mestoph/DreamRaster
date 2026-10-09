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

            var hint = GetField<Label>(
                form,
                "lblConfigHint");
            var technicalPanel = GetField<Panel>(
                form,
                "_configurationAdvancedPanelPolish");

            Assert.IsTrue(
                page.AutoScroll,
                "Configuration doit rester scrollable en vue compacte.");
            if (hint.Visible)
            {
                Assert.IsTrue(
                    hint.Top >= technicalPanel.Bottom + 8,
                    "Le texte d'aide Configuration ne doit pas recouvrir les interfaces techniques.");
            }

            Assert.IsTrue(
                technicalPanel.Bottom <= workspace.Height,
                "Les interfaces techniques doivent être entièrement visibles en vue compacte.");
            Assert.IsFalse(
                hint.Visible,
                "Le texte d'aide long doit être masqué en vue compacte pour éviter un scroll inutile.");
            Assert.IsTrue(
                page.AutoScrollMinSize.Height <= workspace.Height,
                "La Configuration compacte ne doit pas nécessiter de défilement.");
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
            var imageEncoder = GetField<ComboBox>(
                form,
                "_imageTextEncoderRuntimeCombo");
            var videoEncoder = GetField<ComboBox>(
                form,
                "_videoTextEncoderRuntimeCombo");
            var imageLora = GetField<ComboBox>(
                form,
                "_imageLoraCombo");
            var videoLora = GetField<ComboBox>(
                form,
                "_videoLoraCombo");
            var imageProfile = GetField<ComboBox>(
                form,
                "_imagePipelinePresetCombo");
            var videoProfile = GetField<ComboBox>(
                form,
                "_videoPipelinePresetCombo");
            var imageSharpness = GetField<NumericUpDown>(
                form,
                "_imageMaxQualitySharpness");
            var videoSharpness = GetField<NumericUpDown>(
                form,
                "_videoMaxQualitySharpness");
            var imageSharpnessPreview = GetField<Button>(
                form,
                "_imageSharpnessPreviewButton");
            var videoSharpnessPreview = GetField<Button>(
                form,
                "_videoSharpnessPreviewButton");

            Assert.AreEqual(
                imageModel.Top,
                videoModel.Top,
                "La rangée Modèle doit être alignée.");
            Assert.AreEqual(
                imageStyle.Top,
                videoStyle.Top,
                "La rangée Style doit être alignée.");
            Assert.AreEqual(
                imageEncoder.Top,
                videoEncoder.Top,
                "La rangée Encodeur doit être alignée.");
            Assert.AreEqual(
                imageLora.Top,
                videoLora.Top,
                "La rangée LoRA doit être alignée.");
            Assert.AreEqual(
                imageProfile.Bounds,
                videoProfile.Bounds,
                "Les profils complets Image/Vidéo doivent partager exactement la même géométrie.");

            Assert.IsTrue(
                imageEncoder.Top > imageModel.Top,
                "Encodeur doit être placé sous le modèle pour rendre le pipeline explicite.");
            Assert.IsTrue(
                imageStyle.Top > imageEncoder.Top,
                "Les presets Style/Négatif doivent être placés sous les dépendances du pipeline.");
            Assert.IsTrue(
                imageLora.Top > imageStyle.Top,
                "LoRA doit être placé sous les presets pour garder la hiérarchie visuelle claire.");
            Assert.IsTrue(
                imageProfile.Top > imageLora.Top,
                "Le profil complet doit occuper la dernière rangée du catalogue, à côté de la netteté.");

            Assert.AreEqual(
                imageModel.Left,
                videoModel.Left,
                "Les sélecteurs Modèle doivent partager la même colonne.");
            Assert.AreEqual(
                imageStyle.Left,
                videoStyle.Left,
                "Les sélecteurs Style doivent partager la même colonne.");
            Assert.AreEqual(
                imageEncoder.Left,
                videoEncoder.Left,
                "Les sélecteurs Encodeur doivent partager la même colonne.");
            Assert.AreEqual(
                imageLora.Left,
                videoLora.Left,
                "Les sélecteurs LoRA doivent partager la même colonne.");

            Assert.AreEqual(
                imageSharpness.Bounds,
                videoSharpness.Bounds,
                "La netteté maximale Image/Vidéo doit partager la même grille.");
            Assert.AreEqual(
                imageSharpnessPreview.Bounds,
                videoSharpnessPreview.Bounds,
                "Les boutons d'aperçu netteté Image/Vidéo doivent être alignés.");
        });

    [TestMethod]
    public void CreativePresets_ContainAdultAndGenreProfiles()
        => RunOnSta(() =>
        {
            using var form = new MainForm();

            var imageCombo = GetField<ComboBox>(
                form,
                "_imageStyleTemplateCombo");
            var videoCombo = GetField<ComboBox>(
                form,
                "_videoStyleTemplateCombo");

            static string[] ReadIds(ComboBox combo) =>
                combo.Items
                    .Cast<object>()
                    .Select(item =>
                        item.GetType()
                            .GetProperty("Id")
                            ?.GetValue(item)
                            ?.ToString() ??
                        string.Empty)
                    .Where(id => id.Length > 0)
                    .ToArray();

            var imageIds = ReadIds(imageCombo);
            var videoIds = ReadIds(videoCombo);

            foreach (var expected in new[]
                     {
                         "anime",
                         "scifi",
                         "erotic",
                         "adult-explicit"
                     })
            {
                CollectionAssert.Contains(
                    imageIds,
                    expected,
                    $"Le preset Image {expected} doit être visible dans l'interface.");
                CollectionAssert.Contains(
                    videoIds,
                    expected,
                    $"Le preset Vidéo {expected} doit être visible dans l'interface.");
            }

            var templateType = typeof(MainForm).Assembly.GetType(
                "OpenCodeLocalAI.GenerationTemplates")
                ?? throw new AssertFailedException(
                    "GenerationTemplates introuvable.");

            var imageStyles = templateType.GetProperty(
                    "ImageStyles",
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)
                ?.GetValue(null) as System.Collections.IEnumerable
                ?? throw new AssertFailedException(
                    "ImageStyles introuvable.");

            var adultImage = imageStyles
                .Cast<object>()
                .Single(item =>
                    string.Equals(
                        item.GetType()
                            .GetProperty("Id")
                            ?.GetValue(item)
                            ?.ToString(),
                        "adult-explicit",
                        StringComparison.Ordinal));

            StringAssert.Contains(
                adultImage.GetType()
                    .GetProperty("NegativePrompt")
                    ?.GetValue(adultImage)
                    ?.ToString() ??
                string.Empty,
                "underage",
                "Le preset adulte doit exclure explicitement les sujets mineurs ou d'âge ambigu.");
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
