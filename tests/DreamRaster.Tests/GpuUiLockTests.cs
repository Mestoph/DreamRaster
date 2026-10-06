using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenCodeLocalAI;

namespace DreamRaster.Tests;

[TestClass]
[DoNotParallelize]
public sealed class GpuUiLockTests
{
    private static readonly string[] RequiredGpuControlFields =
    [
        // Image
        "_prompt",
        "_negativePrompt",
        "_imageStyleTemplateCombo",
        "_imageNegativeTemplateCombo",
        "numDefaultWidth",
        "numDefaultHeight",
        "numDefaultSteps",
        "_imageCfg",
        "_seedInput",
        "_randomSeedCheck",
        "_imageModelRuntimeCombo",
        "_imageImportModelButton",
        "_imageModelDownloadButton",
        "_imageLoraCombo",
        "_imageLoraStrength",
        "_imageLoraAddButton",
        "_imageLoraDownloadButton",
        "cmbGenerationMode",
        "txtInputImage",
        "btnBrowseInputImage",
        "numImg2ImgStrength",
        "_imageExtractPromptButton",
        "_improvePromptButton",
        "_benchmarkButton",
        "btnGenerate",

        // Vidéo
        "_videoPrompt",
        "_videoNegative",
        "_videoStyleTemplateCombo",
        "_videoNegativeTemplateCombo",
        "_videoModelRuntimeCombo",
        "_videoImportModelButton",
        "_videoModelDownloadButton",
        "_videoQualityCombo",
        "_videoLoraCombo",
        "_videoLoraStrength",
        "_videoLoraAddButton",
        "_videoLoraDownloadButton",
        "_videoImprovePromptButton",
        "_videoAutoImprovePrompt",
        "_videoWidth",
        "_videoHeight",
        "_videoFrames",
        "_videoFps",
        "_videoSteps",
        "_videoCfg",
        "_videoSamplingShift",
        "_videoSampler",
        "_videoScheduler",
        "_videoSeed",
        "_videoRandomSeed",
        "_videoReferenceImage",
        "_videoReferenceBrowseButton",
        "_videoReferenceCropButton",
        "_videoExtractPromptButton",
        "_videoGenerateButton"
    ];


    [TestMethod]
    public void AllMainTabs_ControlsStayInsideParents_AtSupportedSizes()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();
            var tabs = GetField<TabControl>(form, "_tabs");

            var sizes = new[]
            {
                new Size(900, 620),
                new Size(1080, 720),
                new Size(1440, 900),
                new Size(1920, 1032)
            };

            var layout = typeof(MainForm).GetMethod(
                "ApplyV37SharedLayout",
                BindingFlags.Instance | BindingFlags.NonPublic);

            _ = form.Handle;

            foreach (var size in sizes)
            {
                form.Size = size;
                form.PerformLayout();

                // Le formulaire n'est pas montré afin d'éviter le preflight
                // de MainForm_Shown ; on force donc le Dock=Fill comme le
                // ferait WinForms sur une fenêtre réellement affichée.
                tabs.SetBounds(
                    0,
                    0,
                    form.ClientSize.Width,
                    form.ClientSize.Height);
                tabs.PerformLayout();

                var display = tabs.DisplayRectangle;
                foreach (TabPage page in tabs.TabPages)
                {
                    page.SetBounds(
                        display.X,
                        display.Y,
                        display.Width,
                        display.Height);
                    page.PerformLayout();
                }

                layout?.Invoke(form, null);
                tabs.PerformLayout();

                foreach (TabPage page in tabs.TabPages)
                {
                    page.PerformLayout();

                    var clipped = FindClippedControls(page)
                        .Distinct()
                        .ToArray();

                    Assert.AreEqual(
                        0,
                        clipped.Length,
                        $"Contrôles coupés dans {page.Name}/{page.Text} à " +
                        $"{size.Width}x{size.Height}:{Environment.NewLine}" +
                        string.Join(Environment.NewLine, clipped));
                }
            }

            return Task.CompletedTask;
        });



    [TestMethod]
    public void AllMainTabs_VisibleSiblingControls_DoNotMaskEachOther()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();
            var tabs = GetField<TabControl>(form, "_tabs");
            var layout = typeof(MainForm).GetMethod(
                "ApplyV37SharedLayout",
                BindingFlags.Instance | BindingFlags.NonPublic);

            _ = form.Handle;

            foreach (var size in new[]
                     {
                         new Size(900, 620),
                         new Size(1080, 720),
                         new Size(1440, 900),
                         new Size(1920, 1032)
                     })
            {
                form.Size = size;
                form.PerformLayout();
                tabs.SetBounds(
                    0,
                    0,
                    form.ClientSize.Width,
                    form.ClientSize.Height);
                tabs.PerformLayout();

                var display = tabs.DisplayRectangle;
                foreach (TabPage page in tabs.TabPages)
                {
                    page.SetBounds(
                        display.X,
                        display.Y,
                        display.Width,
                        display.Height);
                    page.PerformLayout();
                }

                layout?.Invoke(form, null);

                foreach (TabPage page in tabs.TabPages)
                {
                    var overlaps = FindUnexpectedSiblingOverlaps(page)
                        .Distinct()
                        .ToArray();

                    Assert.AreEqual(
                        0,
                        overlaps.Length,
                        $"Contrôles visibles qui se masquent dans " +
                        $"{page.Name}/{page.Text} à " +
                        $"{size.Width}x{size.Height}:{Environment.NewLine}" +
                        string.Join(Environment.NewLine, overlaps));
                }
            }

            return Task.CompletedTask;
        });

    [TestMethod]
    public void SecondaryDialogs_ControlsStayInsideParents()
        => RunOnStaAsync(() =>
        {
            var assembly = typeof(MainForm).Assembly;

            var downloadType = assembly.GetType(
                "OpenCodeLocalAI.ModelDownloadForm",
                throwOnError: true)!;
            using (var download = (Form)Activator.CreateInstance(
                       downloadType,
                       BindingFlags.Instance |
                       BindingFlags.Public |
                       BindingFlags.NonPublic,
                       binder: null,
                       args: [true, "fr"],
                       culture: null)!)
            {
                foreach (var size in new[]
                         {
                             new Size(680, 306),
                             new Size(900, 380)
                         })
                {
                    download.ClientSize = size;
                    download.PerformLayout();

                    var clipped = FindClippedControls(download)
                        .Distinct()
                        .ToArray();

                    Assert.AreEqual(
                        0,
                        clipped.Length,
                        $"Dialogue téléchargement coupé à " +
                        $"{size.Width}x{size.Height}:{Environment.NewLine}" +
                        string.Join(Environment.NewLine, clipped));
                }
            }

            var tempImage = Path.Combine(
                Path.GetTempPath(),
                "DreamRaster_region_layout_test.png");

            using (var bitmap = new Bitmap(640, 480))
                bitmap.Save(tempImage);

            try
            {
                var regionType = assembly.GetType(
                    "OpenCodeLocalAI.ImageRegionSelectorForm",
                    throwOnError: true)!;
                using var region = (Form)Activator.CreateInstance(
                    regionType,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    binder: null,
                    args: [tempImage, "fr"],
                    culture: null)!;

                foreach (var size in new[]
                         {
                             new Size(720, 520),
                             new Size(980, 760),
                             new Size(1280, 900)
                         })
                {
                    region.Size = size;
                    region.PerformLayout();

                    var clipped = FindClippedControls(region)
                        .Distinct()
                        .ToArray();

                    Assert.AreEqual(
                        0,
                        clipped.Length,
                        $"Sélecteur de zone coupé à " +
                        $"{size.Width}x{size.Height}:{Environment.NewLine}" +
                        string.Join(Environment.NewLine, clipped));
                }
            }
            finally
            {
                if (File.Exists(tempImage))
                    File.Delete(tempImage);
            }

            return Task.CompletedTask;
        });

    [TestMethod]
    public void AllTabs_TopLevelControls_FitCommonWindowSizes()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();

            var layout = typeof(MainForm).GetMethod(
                "ApplyV37SharedLayout",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "ApplyV37SharedLayout");
            var workspaceMethod = typeof(MainForm).GetMethod(
                "GetSharedWorkspaceSizeV37",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "GetSharedWorkspaceSizeV37");

            var tabs = GetField<TabControl>(form, "_tabs");
            var sizes = new[]
            {
                new Size(900, 620),
                new Size(1080, 720),
                new Size(1440, 900),
                new Size(1920, 1032)
            };

            foreach (var size in sizes)
            {
                form.Size = size;
                form.PerformLayout();
                layout.Invoke(form, null);

                var workspace = (Size)(
                    workspaceMethod.Invoke(form, null)
                    ?? throw new InvalidOperationException(
                        "GetSharedWorkspaceSizeV37 n'a renvoyé aucune taille."));

                foreach (TabPage tab in tabs.TabPages)
                {
                    // Les TabPage non sélectionnées conservent parfois leur
                    // ancienne taille tant qu'elles ne sont pas affichées.
                    // Reproduit ici la zone que TabControl leur donne à l'écran.
                    tab.Size = workspace;
                    tab.PerformLayout();

                    foreach (Control control in tab.Controls)
                    {
                        if (!IsLocallyVisible(control) ||
                            control.Width <= 0 ||
                            control.Height <= 0)
                        {
                            continue;
                        }

                        var bounds = control.Bounds;
                        Assert.IsTrue(
                            bounds.Left >= -2 &&
                            bounds.Top >= -2 &&
                            bounds.Right <= workspace.Width + 2 &&
                            bounds.Bottom <= workspace.Height + 2,
                            $"Contrôle hors zone à {size.Width}x{size.Height} · " +
                            $"onglet={tab.Name}/{tab.Text} · " +
                            $"contrôle={control.Name}/{control.Text} · " +
                            $"bounds={bounds} · workspace={workspace}.");
                    }
                }
            }

            return Task.CompletedTask;
        });

    [TestMethod]
    public void Compact900_KeyboardTabOrder_HasNoDuplicateVisibleTabStops()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();
            var tabs = GetField<TabControl>(form, "_tabs");
            var layout = typeof(MainForm).GetMethod(
                "ApplyV37SharedLayout",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "ApplyV37SharedLayout");

            _ = form.Handle;
            form.Size = new Size(900, 620);
            form.PerformLayout();
            tabs.SetBounds(
                0,
                0,
                form.ClientSize.Width,
                form.ClientSize.Height);
            tabs.PerformLayout();

            var display = tabs.DisplayRectangle;
            foreach (TabPage page in tabs.TabPages)
            {
                page.SetBounds(
                    display.X,
                    display.Y,
                    display.Width,
                    display.Height);
                page.PerformLayout();
            }

            layout.Invoke(form, null);

            var duplicates = new List<string>();
            foreach (TabPage page in tabs.TabPages)
            {
                FindDuplicateTabIndices(
                    page,
                    page.Name,
                    duplicates);
            }

            Assert.AreEqual(
                0,
                duplicates.Count,
                "TabIndex dupliqués entre contrôles visibles/tabulables à 900x620:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, duplicates));

            return Task.CompletedTask;
        });

    [TestMethod]
    public void Compact900_StressStates_DoNotClipOverlapOrLoseLongStatusText()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();
            var tabs = GetField<TabControl>(form, "_tabs");
            var layout = typeof(MainForm).GetMethod(
                "ApplyV37SharedLayout",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "ApplyV37SharedLayout");

            _ = form.Handle;
            form.Size = new Size(900, 620);
            form.PerformLayout();
            tabs.SetBounds(
                0,
                0,
                form.ClientSize.Width,
                form.ClientSize.Height);
            tabs.PerformLayout();

            var display = tabs.DisplayRectangle;
            foreach (TabPage page in tabs.TabPages)
            {
                page.SetBounds(
                    display.X,
                    display.Y,
                    display.Width,
                    display.Height);
                page.PerformLayout();
            }

            const string longError =
                "ERREUR : impossible de terminer l'opération demandée. " +
                "Le serveur local a renvoyé une réponse inattendue après plusieurs tentatives ; " +
                "vérifiez les modèles, l'espace disque et les journaux détaillés avant de réessayer.";

            GetField<Label>(form, "_status").Text = longError;
            GetField<Label>(form, "_gpu").Text =
                "GPU NVIDIA GeForce RTX — mémoire utilisée 15234 MiB / 16384 MiB — " +
                "pilote 999.99 — génération fortement chargée.";
            GetField<RichTextBox>(form, "_liveLog").Text =
                string.Join(Environment.NewLine, Enumerable.Repeat(longError, 40));

            var genText = GetField<Label>(form, "_genText");
            genText.Text = longError;
            var genProgress = GetField<ProgressBar>(form, "_genProgress");
            genProgress.Value = 73;
            genProgress.Style = ProgressBarStyle.Continuous;
            GetField<Button>(form, "btnGenerate").Enabled = false;
            GetField<Button>(form, "_benchmarkButton").Enabled = false;

            var videoStatus = GetField<Label>(form, "_videoStatus");
            videoStatus.Text = longError;
            GetField<ProgressBar>(form, "_videoProgress").Value = 61;
            GetField<Button>(form, "_videoGenerateButton").Enabled = false;
            GetField<Button>(form, "_videoCancelButton").Enabled = true;
            GetField<Button>(form, "_videoRefreshButton").Enabled = false;

            foreach (var fieldName in new[]
                     {
                         "_imageModelRuntimeCombo",
                         "_imageLoraCombo",
                         "_videoModelRuntimeCombo",
                         "_videoLoraCombo",
                         "_videoQualityCombo",
                         "_visionModelCombo",
                         "_fluxModelCombo",
                         "_textEncoderCombo",
                         "_vaeCombo"
                     })
            {
                var combo = GetField<ComboBox>(form, fieldName);
                combo.Items.Add(
                    "Modèle extrêmement long pour test d'affichage compact " +
                    "— variante expérimentale FP16 uncensored catalogue local");
                combo.SelectedIndex = combo.Items.Count - 1;
            }

            foreach (var fieldName in new[]
                     {
                         "_imageModelDownloadButton",
                         "_imageLoraDownloadButton",
                         "_imageCatalogDownloadCancelButton",
                         "_videoModelDownloadButton",
                         "_videoLoraDownloadButton",
                         "_videoCatalogDownloadCancelButton"
                     })
            {
                GetField<Button>(form, fieldName).Visible = true;
            }

            foreach (var fieldName in new[]
                     {
                         "_imageCatalogDownloadProgress",
                         "_videoCatalogDownloadProgress"
                     })
            {
                var progress = GetField<ProgressBar>(form, fieldName);
                progress.Visible = true;
                progress.Value = 58;
            }

            foreach (var fieldName in new[]
                     {
                         "_imageCatalogDownloadStatus",
                         "_videoCatalogDownloadStatus"
                     })
            {
                var label = GetField<Label>(form, fieldName);
                label.Visible = true;
                label.Text = longError;
            }

            foreach (var fieldName in new[]
                     {
                         "_imageCatalogDownloadSize",
                         "_videoCatalogDownloadSize"
                     })
            {
                var label = GetField<Label>(form, fieldName);
                label.Visible = true;
                label.Text = "7,75 Go / 15,90 Go · SHA256 en cours de vérification";
            }

            GetField<Label>(form, "_configurationSaveStatus").Text = longError;
            GetField<Button>(form, "btnSettingsSave").Enabled = false;
            GetField<Button>(form, "btnOpenConfigFolder").Enabled = false;

            GetField<Label>(form, "_installVideoModelsStatus").Text = longError;
            GetField<TextBox>(form, "_installComponentsStatus").Text =
                string.Join(Environment.NewLine, Enumerable.Repeat(longError, 6));
            GetField<ProgressBar>(form, "_installProgress").Value = 79;
            GetField<Button>(form, "btnInstallAll").Enabled = false;
            GetField<Button>(form, "btnInstallCancel").Enabled = true;
            GetField<Button>(form, "_installImageModelsButton").Enabled = false;
            GetField<Button>(form, "_installVideoModelsButton").Enabled = false;
            GetField<RichTextBox>(form, "_installLog").Text =
                string.Join(Environment.NewLine, Enumerable.Repeat(longError, 50));

            var comfyPanel = GetField<Panel>(form, "_comfyStatusPanel");
            comfyPanel.Visible = true;
            GetField<Label>(form, "_comfyStatusMessage").Text = longError;
            GetField<Button>(form, "_comfyStatusRetryButton").Visible = true;
            GetField<Control>(form, "_comfyWeb").Visible = false;

            GetField<RichTextBox>(form, "_ollamaLog").Text =
                string.Join(Environment.NewLine, Enumerable.Repeat(longError, 50));
            GetField<RichTextBox>(form, "_allLog").Text =
                string.Join(Environment.NewLine, Enumerable.Repeat(longError, 80));

            var aboutStatus = GetField<Label>(form, "_aboutUpdateStatus");
            aboutStatus.Text = longError;
            GetField<ProgressBar>(form, "_aboutUpdateProgress").Value = 82;
            GetField<Button>(form, "btnCheckUpdates").Enabled = false;
            GetField<Button>(form, "btnOpenGitHub").Enabled = false;

            layout.Invoke(form, null);

            foreach (TabPage page in tabs.TabPages)
            {
                page.PerformLayout();

                var clipped = FindClippedControls(page)
                    .Distinct()
                    .ToArray();
                var overlaps = FindUnexpectedSiblingOverlaps(page)
                    .Distinct()
                    .ToArray();

                Assert.AreEqual(
                    0,
                    clipped.Length,
                    $"État chargé coupé dans {page.Name} à 900x620:" +
                    Environment.NewLine +
                    string.Join(Environment.NewLine, clipped));
                Assert.AreEqual(
                    0,
                    overlaps.Length,
                    $"État chargé qui se chevauche dans {page.Name} à 900x620:" +
                    Environment.NewLine +
                    string.Join(Environment.NewLine, overlaps));
            }

            foreach (var label in new[]
                     {
                         GetField<Label>(form, "_status"),
                         GetField<Label>(form, "_gpu"),
                         genText,
                         videoStatus,
                         GetField<Label>(form, "_imageCatalogDownloadStatus"),
                         GetField<Label>(form, "_imageCatalogDownloadSize"),
                         GetField<Label>(form, "_videoCatalogDownloadStatus"),
                         GetField<Label>(form, "_videoCatalogDownloadSize"),
                         GetField<Label>(form, "_configurationSaveStatus"),
                         GetField<Label>(form, "_installVideoModelsStatus"),
                         GetField<Label>(form, "_comfyStatusMessage"),
                         aboutStatus
                     })
            {
                AssertDynamicLabelHandlesLongText(label);
            }

            return Task.CompletedTask;
        });

    [TestMethod]
    public void Compact900_EnglishUi_IsNotTruncated()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();
            var tabs = GetField<TabControl>(form, "_tabs");
            var settings = GetField<AppSettings>(form, "_s");
            settings.Language = "en";

            var translate = typeof(MainForm).GetMethod(
                "ApplyTranslations",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "ApplyTranslations");
            var layout = typeof(MainForm).GetMethod(
                "ApplyV37SharedLayout",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "ApplyV37SharedLayout");

            translate.Invoke(form, null);

            _ = form.Handle;
            form.Size = new Size(900, 620);
            form.PerformLayout();
            tabs.SetBounds(
                0,
                0,
                form.ClientSize.Width,
                form.ClientSize.Height);
            tabs.PerformLayout();

            var display = tabs.DisplayRectangle;
            foreach (TabPage page in tabs.TabPages)
            {
                page.SetBounds(
                    display.X,
                    display.Y,
                    display.Width,
                    display.Height);
                page.PerformLayout();
            }

            layout.Invoke(form, null);

            var truncated = tabs.TabPages
                .Cast<TabPage>()
                .SelectMany(FindTruncatedStaticText)
                .Distinct()
                .ToArray();

            Assert.AreEqual(
                0,
                truncated.Length,
                "English static text truncated at 900x620:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, truncated));

            foreach (var tabControl in EnumerateControls<TabControl>(form)
                         .Prepend(tabs)
                         .Distinct())
            {
                if (tabControl.TabCount == 0)
                    continue;

                _ = tabControl.Handle;
                tabControl.PerformLayout();

                var last = tabControl.GetTabRect(
                    tabControl.TabCount - 1);
                Assert.IsTrue(
                    last.Right <= tabControl.ClientRectangle.Right - 1,
                    $"English tab header hidden at 900x620 in " +
                    $"{tabControl.Name}: last={last}, " +
                    $"client={tabControl.ClientRectangle}.");
            }

            return Task.CompletedTask;
        });

    [TestMethod]
    public void Compact900_CatalogControls_DoNotOverlapWhenAllVisible()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();
            var tabs = GetField<TabControl>(form, "_tabs");
            var layout = typeof(MainForm).GetMethod(
                "ApplyV37SharedLayout",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "ApplyV37SharedLayout");

            _ = form.Handle;
            form.Size = new Size(900, 620);
            form.PerformLayout();
            tabs.SetBounds(
                0,
                0,
                form.ClientSize.Width,
                form.ClientSize.Height);
            tabs.PerformLayout();

            var display = tabs.DisplayRectangle;
            foreach (TabPage page in tabs.TabPages)
            {
                page.SetBounds(
                    display.X,
                    display.Y,
                    display.Width,
                    display.Height);
                page.PerformLayout();
            }

            var controls = new Control[]
            {
                GetField<Button>(form, "_imageModelDownloadButton"),
                GetField<Button>(form, "_imageLoraDownloadButton"),
                GetField<Label>(form, "_imageCatalogDownloadStatus"),
                GetField<ProgressBar>(form, "_imageCatalogDownloadProgress"),
                GetField<Label>(form, "_imageCatalogDownloadSize"),
                GetField<Button>(form, "_imageCatalogDownloadCancelButton"),
                GetField<Button>(form, "_videoModelDownloadButton"),
                GetField<Button>(form, "_videoLoraDownloadButton"),
                GetField<Label>(form, "_videoCatalogDownloadStatus"),
                GetField<ProgressBar>(form, "_videoCatalogDownloadProgress"),
                GetField<Label>(form, "_videoCatalogDownloadSize"),
                GetField<Button>(form, "_videoCatalogDownloadCancelButton")
            };

            foreach (var control in controls)
                control.Visible = true;

            layout.Invoke(form, null);

            foreach (var page in new[]
                     {
                         GetField<TabPage>(form, "tabGenerate"),
                         GetField<TabPage>(form, "_tabVideo")
                     })
            {
                var clipped = FindClippedControls(page)
                    .Distinct()
                    .ToArray();
                var overlaps = FindUnexpectedSiblingOverlaps(page)
                    .Distinct()
                    .ToArray();

                Assert.AreEqual(
                    0,
                    clipped.Length,
                    $"Catalogue coupé dans {page.Name} à 900x620:" +
                    Environment.NewLine +
                    string.Join(Environment.NewLine, clipped));
                Assert.AreEqual(
                    0,
                    overlaps.Length,
                    $"Catalogue qui se chevauche dans {page.Name} à 900x620:" +
                    Environment.NewLine +
                    string.Join(Environment.NewLine, overlaps));
            }

            return Task.CompletedTask;
        });

    [TestMethod]
    public void Compact900_AllTabControls_ShowTheirLastHeader()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();
            var tabs = GetField<TabControl>(form, "_tabs");
            var layout = typeof(MainForm).GetMethod(
                "ApplyV37SharedLayout",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "ApplyV37SharedLayout");

            _ = form.Handle;
            form.Size = new Size(900, 620);
            form.PerformLayout();
            tabs.SetBounds(
                0,
                0,
                form.ClientSize.Width,
                form.ClientSize.Height);
            tabs.PerformLayout();

            var display = tabs.DisplayRectangle;
            foreach (TabPage page in tabs.TabPages)
            {
                page.SetBounds(
                    display.X,
                    display.Y,
                    display.Width,
                    display.Height);
                page.PerformLayout();
            }

            layout.Invoke(form, null);

            var allTabControls = EnumerateControls<TabControl>(form)
                .Prepend(tabs)
                .Distinct()
                .ToArray();

            foreach (var tabControl in allTabControls)
            {
                if (tabControl.TabCount == 0)
                    continue;

                _ = tabControl.Handle;
                tabControl.PerformLayout();

                var last = tabControl.GetTabRect(
                    tabControl.TabCount - 1);

                Assert.IsTrue(
                    last.Right <= tabControl.ClientRectangle.Right - 1,
                    $"Sous-onglets masqués à 900x620 dans " +
                    $"{tabControl.Name}: last={last}, " +
                    $"client={tabControl.ClientRectangle}.");
            }

            return Task.CompletedTask;
        });

    [TestMethod]
    public void DesignerForms_HaveParameterlessConstructorsAndVisibleControls()
        => RunOnStaAsync(() =>
        {
            var assembly = typeof(MainForm).Assembly;

            foreach (var typeName in new[]
                     {
                         "OpenCodeLocalAI.ModelDownloadForm",
                         "OpenCodeLocalAI.ImageRegionSelectorForm"
                     })
            {
                var type = assembly.GetType(
                    typeName,
                    throwOnError: true)!;

                Assert.IsTrue(
                    type.IsPublic,
                    $"{type.Name} doit être publique pour le Designer WinForms.");

                var ctor = type.GetConstructor(Type.EmptyTypes);
                Assert.IsNotNull(
                    ctor,
                    $"{type.Name} doit conserver un constructeur public sans paramètre.");

                using var form = (Form)ctor.Invoke(null);
                _ = form.Handle;
                form.PerformLayout();

                Assert.IsTrue(
                    form.Controls.Count > 0,
                    $"{type.Name} doit exposer des contrôles au Designer.");

                Assert.IsTrue(
                    form.ClientSize.Width > 100 &&
                    form.ClientSize.Height > 100,
                    $"{type.Name} doit avoir une surface Designer exploitable.");
            }

            return Task.CompletedTask;
        });

    [TestMethod]
    public void SecondaryDialogs_HeavyStates_DoNotClipOrOverlap()
        => RunOnStaAsync(() =>
        {
            var assembly = typeof(MainForm).Assembly;
            var downloadType = assembly.GetType(
                "OpenCodeLocalAI.ModelDownloadForm",
                throwOnError: true)!;

            foreach (var language in new[] { "fr", "en" })
            {
                using var download = (Form)Activator.CreateInstance(
                    downloadType,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    binder: null,
                    args: [true, language],
                    culture: null)!;

                download.ClientSize = new Size(680, 306);
                download.PerformLayout();

                var url = (TextBox)(
                    downloadType.GetField(
                        "_url",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.GetValue(download)
                    ?? throw new MissingFieldException("_url"));
                var fileName = (TextBox)(
                    downloadType.GetField(
                        "_fileName",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.GetValue(download)
                    ?? throw new MissingFieldException("_fileName"));
                var sha = (TextBox)(
                    downloadType.GetField(
                        "_sha256",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.GetValue(download)
                    ?? throw new MissingFieldException("_sha256"));

                url.Text =
                    "https://huggingface.co/example/very-long-repository-name/" +
                    "resolve/main/models/super-long-model-file-name-with-many-details-" +
                    "fp16-uncensored-experimental.safetensors?download=true";
                fileName.Text =
                    "super-long-model-file-name-with-many-details-fp16-" +
                    "uncensored-experimental.safetensors";
                sha.Text = new string('a', 64);

                foreach (var button in EnumerateControls<Button>(download))
                    button.Enabled = false;

                var clipped = FindClippedControls(download)
                    .Distinct()
                    .ToArray();
                var overlaps = FindUnexpectedSiblingOverlaps(download)
                    .Distinct()
                    .ToArray();

                Assert.AreEqual(
                    0,
                    clipped.Length,
                    $"Modale téléchargement chargée coupée ({language}):" +
                    Environment.NewLine +
                    string.Join(Environment.NewLine, clipped));
                Assert.AreEqual(
                    0,
                    overlaps.Length,
                    $"Modale téléchargement chargée chevauchée ({language}):" +
                    Environment.NewLine +
                    string.Join(Environment.NewLine, overlaps));
            }

            var tempImage = Path.Combine(
                Path.GetTempPath(),
                "DreamRaster_region_heavy_test.png");
            using (var bitmap = new Bitmap(1920, 1080))
                bitmap.Save(tempImage);

            try
            {
                var regionType = assembly.GetType(
                    "OpenCodeLocalAI.ImageRegionSelectorForm",
                    throwOnError: true)!;

                foreach (var language in new[] { "fr", "en" })
                {
                    using var region = (Form)Activator.CreateInstance(
                        regionType,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic,
                        binder: null,
                        args: [tempImage, language],
                        culture: null)!;

                    region.Size = new Size(720, 520);
                    region.PerformLayout();

                    foreach (var button in EnumerateControls<Button>(region))
                        button.Enabled = false;

                    var clipped = FindClippedControls(region)
                        .Distinct()
                        .ToArray();
                    var overlaps = FindUnexpectedSiblingOverlaps(region)
                        .Distinct()
                        .ToArray();

                    Assert.AreEqual(
                        0,
                        clipped.Length,
                        $"Modale sélection chargée coupée ({language}):" +
                        Environment.NewLine +
                        string.Join(Environment.NewLine, clipped));
                    Assert.AreEqual(
                        0,
                        overlaps.Length,
                        $"Modale sélection chargée chevauchée ({language}):" +
                        Environment.NewLine +
                        string.Join(Environment.NewLine, overlaps));
                }
            }
            finally
            {
                if (File.Exists(tempImage))
                    File.Delete(tempImage);
            }

            return Task.CompletedTask;
        });

    [TestMethod]
    public void SecondaryDialogs_StaticText_IsNotTruncated()
        => RunOnStaAsync(() =>
        {
            var assembly = typeof(MainForm).Assembly;

            foreach (var language in new[] { "fr", "en" })
            {
                var downloadType = assembly.GetType(
                    "OpenCodeLocalAI.ModelDownloadForm",
                    throwOnError: true)!;
                using var download = (Form)Activator.CreateInstance(
                    downloadType,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    binder: null,
                    args: [true, language],
                    culture: null)!;

                download.ClientSize = new Size(680, 306);
                download.PerformLayout();

                var truncatedDownload = FindTruncatedStaticText(download)
                    .Distinct()
                    .ToArray();

                Assert.AreEqual(
                    0,
                    truncatedDownload.Length,
                    $"Textes tronqués dans ModelDownloadForm ({language}):" +
                    Environment.NewLine +
                    string.Join(Environment.NewLine, truncatedDownload));
            }

            var tempImage = Path.Combine(
                Path.GetTempPath(),
                "DreamRaster_region_text_test.png");
            using (var bitmap = new Bitmap(640, 480))
                bitmap.Save(tempImage);

            try
            {
                foreach (var language in new[] { "fr", "en" })
                {
                    var regionType = assembly.GetType(
                        "OpenCodeLocalAI.ImageRegionSelectorForm",
                        throwOnError: true)!;
                    using var region = (Form)Activator.CreateInstance(
                        regionType,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic,
                        binder: null,
                        args: [tempImage, language],
                        culture: null)!;

                    region.Size = new Size(720, 520);
                    region.PerformLayout();

                    var truncatedRegion = FindTruncatedStaticText(region)
                        .Distinct()
                        .ToArray();

                    Assert.AreEqual(
                        0,
                        truncatedRegion.Length,
                        $"Textes tronqués dans ImageRegionSelectorForm ({language}):" +
                        Environment.NewLine +
                        string.Join(Environment.NewLine, truncatedRegion));
                }
            }
            finally
            {
                if (File.Exists(tempImage))
                    File.Delete(tempImage);
            }

            return Task.CompletedTask;
        });

    [TestMethod]
    public void Compact900_MainTabStrip_ShowsAllTabsWithoutScrolling()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();
            var tabs = GetField<TabControl>(form, "_tabs");
            var layout = typeof(MainForm).GetMethod(
                "ApplyV37SharedLayout",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "ApplyV37SharedLayout");

            _ = form.Handle;
            form.Size = new Size(900, 620);
            form.PerformLayout();
            tabs.SetBounds(
                0,
                0,
                form.ClientSize.Width,
                form.ClientSize.Height);
            tabs.PerformLayout();
            layout.Invoke(form, null);
            tabs.PerformLayout();

            Assert.AreEqual(
                10,
                tabs.TabCount,
                "DreamRaster doit exposer les 10 onglets principaux.");

            var last = tabs.GetTabRect(tabs.TabCount - 1);
            Assert.IsTrue(
                last.Right <= tabs.ClientRectangle.Right - 2,
                $"Le dernier onglet est hors de la barre à 900x620 : " +
                $"last={last}, client={tabs.ClientRectangle}, item={tabs.ItemSize}.");

            using var compactFont = new Font(
                form.Font.FontFamily,
                8F,
                FontStyle.Regular,
                GraphicsUnit.Point);
            using var compactBoldFont = new Font(
                form.Font.FontFamily,
                8F,
                FontStyle.Bold,
                GraphicsUnit.Point);

            foreach (TabPage page in tabs.TabPages)
            {
                var regular = TextRenderer.MeasureText(
                    page.Text,
                    compactFont,
                    Size.Empty,
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.NoPrefix |
                    TextFormatFlags.NoPadding);
                var bold = TextRenderer.MeasureText(
                    page.Text,
                    compactBoldFont,
                    Size.Empty,
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.NoPrefix |
                    TextFormatFlags.NoPadding);
                var measuredWidth = Math.Max(
                    regular.Width,
                    bold.Width);

                Assert.IsTrue(
                    measuredWidth <= tabs.ItemSize.Width - 6,
                    $"Texte d'onglet tronqué à 900x620 : " +
                    $"{page.Name}/{page.Text} mesure jusqu'à {measuredWidth}px " +
                    $"pour {tabs.ItemSize.Width}px.");
            }

            return Task.CompletedTask;
        });

    [TestMethod]
    public void Compact900_StaticUiText_IsNotTruncated()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();
            var tabs = GetField<TabControl>(form, "_tabs");
            var layout = typeof(MainForm).GetMethod(
                "ApplyV37SharedLayout",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "ApplyV37SharedLayout");

            _ = form.Handle;
            form.Size = new Size(900, 620);
            form.PerformLayout();
            tabs.SetBounds(
                0,
                0,
                form.ClientSize.Width,
                form.ClientSize.Height);
            tabs.PerformLayout();

            var display = tabs.DisplayRectangle;
            foreach (TabPage page in tabs.TabPages)
            {
                page.SetBounds(
                    display.X,
                    display.Y,
                    display.Width,
                    display.Height);
                page.PerformLayout();
            }

            layout.Invoke(form, null);

            var truncated = tabs.TabPages
                .Cast<TabPage>()
                .SelectMany(FindTruncatedStaticText)
                .Distinct()
                .ToArray();

            Assert.AreEqual(
                0,
                truncated.Length,
                "Textes statiques tronqués à 900x620:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, truncated));

            return Task.CompletedTask;
        });

    [TestMethod]
    public void VideoDuration_SynchronizesWithWanFramesAndFps()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();

            var duration = GetField<NumericUpDown>(
                form,
                "_videoDurationSeconds");
            var frames = GetField<NumericUpDown>(
                form,
                "_videoFrames");
            var fps = GetField<NumericUpDown>(
                form,
                "_videoFps");

            fps.Value = 16;
            duration.Value = 2.00M;

            Assert.AreEqual(
                33M,
                frames.Value,
                "2,00 s à 16 FPS doit être ajusté au nombre de frames Wan valide le plus proche.");
            Assert.AreEqual(
                2.06M,
                duration.Value,
                "La durée doit afficher la durée réelle après quantification Wan.");

            fps.Value = 4;
            frames.Value = 5;

            Assert.AreEqual(
                1.25M,
                duration.Value,
                "Frames/FPS doit recalculer immédiatement la durée affichée.");

            return Task.CompletedTask;
        });

    [TestMethod]
    public void EmbeddedWebViews_AdaptZoomInCompactMode()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();
            var comfyTab = GetField<TabPage>(form, "tabComfy");
            var openCodeTab = GetField<TabPage>(form, "tabOpenCode");
            var comfyWeb = GetField<Control>(
                form,
                "_comfyWeb");
            var openCodeWeb = GetField<Control>(
                form,
                "_web");
            static double ZoomOf(Control control) =>
                (double)(
                    control.GetType()
                        .GetProperty("ZoomFactor")?
                        .GetValue(control)
                    ?? throw new MissingMemberException(
                        control.GetType().FullName,
                        "ZoomFactor"));

            var apply = typeof(MainForm).GetMethod(
                "ApplyEmbeddedWebZoomV37",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "ApplyEmbeddedWebZoomV37");

            comfyTab.ClientSize = new Size(850, 550);
            openCodeTab.ClientSize = new Size(850, 550);
            apply.Invoke(form, null);

            Assert.AreEqual(
                0.80D,
                ZoomOf(comfyWeb),
                0.001D,
                "ComfyUI doit être réduit en mode compact.");
            Assert.AreEqual(
                0.85D,
                ZoomOf(openCodeWeb),
                0.001D,
                "OpenCode doit être réduit en mode compact.");

            comfyTab.ClientSize = new Size(1200, 720);
            openCodeTab.ClientSize = new Size(1200, 720);
            apply.Invoke(form, null);

            Assert.AreEqual(
                1.00D,
                ZoomOf(comfyWeb),
                0.001D,
                "ComfyUI doit revenir à 100 % en grande fenêtre.");
            Assert.AreEqual(
                1.00D,
                ZoomOf(openCodeWeb),
                0.001D,
                "OpenCode doit revenir à 100 % en grande fenêtre.");

            return Task.CompletedTask;
        });

    [TestMethod]
    public void NativeButtons_TextFitsAtCompactSize()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();
            var tabs = GetField<TabControl>(form, "_tabs");
            var layout = typeof(MainForm).GetMethod(
                "ApplyV37SharedLayout",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "ApplyV37SharedLayout");

            _ = form.Handle;
            form.Size = new Size(900, 620);
            form.PerformLayout();
            tabs.SetBounds(
                0,
                0,
                form.ClientSize.Width,
                form.ClientSize.Height);
            tabs.PerformLayout();

            var display = tabs.DisplayRectangle;
            foreach (TabPage page in tabs.TabPages)
            {
                page.SetBounds(
                    display.X,
                    display.Y,
                    display.Width,
                    display.Height);
                page.PerformLayout();
            }

            layout.Invoke(form, null);

            var clipped = new List<string>();
            foreach (TabPage page in tabs.TabPages)
            {
                foreach (var button in EnumerateControls<Button>(page))
                {
                    if (!IsControlLocallyVisible(button) ||
                        string.IsNullOrWhiteSpace(button.Text) ||
                        button.Text.Trim().Length <= 2)
                    {
                        continue;
                    }

                    var measured = TextRenderer.MeasureText(
                        button.Text,
                        button.Font,
                        Size.Empty,
                        TextFormatFlags.SingleLine |
                        TextFormatFlags.NoPrefix);

                    // WinForms Flat buttons need a little horizontal breathing room.
                    if (measured.Width + 12 > button.ClientSize.Width)
                    {
                        clipped.Add(
                            $"{page.Name}/{button.Name} '{button.Text}' " +
                            $"needs≈{measured.Width + 12}px has={button.ClientSize.Width}px");
                    }
                }
            }

            Assert.AreEqual(
                0,
                clipped.Count,
                "Libellés de boutons tronqués à 900x620:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, clipped));

            return Task.CompletedTask;
        });

    [TestMethod]
    public void ProductVersion_IsV38()
    {
        var assembly = typeof(MainForm).Assembly;
        var assemblyVersion = assembly.GetName().Version?.ToString();
        var productVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        Assert.AreEqual(
            "38.0.0.0",
            assemblyVersion,
            "L'AssemblyVersion doit correspondre à la branche fonctionnelle v38.");
        Assert.IsNotNull(
            productVersion,
            "La version produit/informationnelle doit être générée.");
        StringAssert.StartsWith(
            productVersion,
            "38.0.0",
            "Application.ProductVersion doit annoncer v38.");
    }

    [TestMethod]
    public void ImageAndVideoControls_AreLockedAndRestored_AfterSuccess()
        => RunOnStaAsync(() => VerifyScenarioAsync(GpuExit.Success));

    [TestMethod]
    public void ImageAndVideoControls_AreLockedAndRestored_AfterCancellation()
        => RunOnStaAsync(() => VerifyScenarioAsync(GpuExit.Cancellation));

    [TestMethod]
    public void ImageAndVideoControls_AreLockedAndRestored_AfterTimeout()
        => RunOnStaAsync(() => VerifyScenarioAsync(GpuExit.Timeout));

    [TestMethod]
    public void ImageAndVideoControls_AreLockedAndRestored_AfterUnexpectedException()
        => RunOnStaAsync(() => VerifyScenarioAsync(GpuExit.UnexpectedException));

    [TestMethod]
    public void ImageAndVideoLayouts_ShareWorkspaceGeometry()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();

            var layout = typeof(MainForm).GetMethod(
                "ApplyV37SharedLayout",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "ApplyV37SharedLayout");

            var workspaceMethod = typeof(MainForm).GetMethod(
                "GetSharedWorkspaceSizeV37",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "GetSharedWorkspaceSizeV37");

            var imageModel = GetField<ComboBox>(form, "_imageModelRuntimeCombo");
            var videoModel = GetField<ComboBox>(form, "_videoModelRuntimeCombo");
            var imageLora = GetField<ComboBox>(form, "_imageLoraCombo");
            var videoLora = GetField<ComboBox>(form, "_videoLoraCombo");
            var imageModelDownload = GetField<Button>(form, "_imageModelDownloadButton");
            var videoModelDownload = GetField<Button>(form, "_videoModelDownloadButton");
            var imageLoraDownload = GetField<Button>(form, "_imageLoraDownloadButton");
            var videoLoraDownload = GetField<Button>(form, "_videoLoraDownloadButton");
            var imageCatalogProgress = GetField<ProgressBar>(form, "_imageCatalogDownloadProgress");
            var videoCatalogProgress = GetField<ProgressBar>(form, "_videoCatalogDownloadProgress");
            var imageCatalogStatus = GetField<Label>(form, "_imageCatalogDownloadStatus");
            var videoCatalogStatus = GetField<Label>(form, "_videoCatalogDownloadStatus");
            var imageCatalogSize = GetField<Label>(form, "_imageCatalogDownloadSize");
            var videoCatalogSize = GetField<Label>(form, "_videoCatalogDownloadSize");
            var imagePreview = GetField<Control>(form, "_imagePreviewViewport");
            var videoPreview = GetField<Control>(form, "_videoPreviewWeb");
            var imageHistory = GetField<FlowLayoutPanel>(form, "_imageHistoryPanel");
            var videoHistory = GetField<FlowLayoutPanel>(form, "_videoHistoryPanel");
            var videoPrompt = GetField<TextBox>(form, "_videoPrompt");
            var videoNegative = GetField<TextBox>(form, "_videoNegative");
            var imageGenerate = GetField<Button>(form, "btnGenerate");
            var videoGenerate = GetField<Button>(form, "_videoGenerateButton");
            var imageBenchmark = GetField<Button>(form, "_benchmarkButton");
            var imageStatus = GetField<Label>(form, "_genText");
            var imageProgress = GetField<ProgressBar>(form, "_genProgress");
            var videoStatus = GetField<Label>(form, "_videoStatus");
            var videoProgress = GetField<ProgressBar>(form, "_videoProgress");

            var sizes = new[]
            {
                new Size(900, 620),
                new Size(1080, 720),
                new Size(1440, 900),
                new Size(1920, 1032)
            };

            foreach (var size in sizes)
            {
                form.Size = size;
                form.PerformLayout();
                layout.Invoke(form, null);

                Assert.AreEqual(
                    imageModel.Bounds,
                    videoModel.Bounds,
                    $"Modèles non alignés à {size.Width}x{size.Height}.");
                Assert.AreEqual(
                    imageLora.Bounds,
                    videoLora.Bounds,
                    $"LoRA non alignés à {size.Width}x{size.Height}.");
                Assert.AreEqual(
                    imageModelDownload.Bounds,
                    videoModelDownload.Bounds,
                    $"Boutons Télécharger Modèle non alignés à {size.Width}x{size.Height}.");
                Assert.AreEqual(
                    imageLoraDownload.Bounds,
                    videoLoraDownload.Bounds,
                    $"Boutons Télécharger LoRA non alignés à {size.Width}x{size.Height}.");
                Assert.AreEqual(
                    imageCatalogProgress.Bounds,
                    videoCatalogProgress.Bounds,
                    $"Progressions catalogue non alignées à {size.Width}x{size.Height}.");
                Assert.AreEqual(
                    imageCatalogStatus.Bounds,
                    videoCatalogStatus.Bounds,
                    $"Statuts catalogue non alignés à {size.Width}x{size.Height}.");
                Assert.AreEqual(
                    imageCatalogSize.Bounds,
                    videoCatalogSize.Bounds,
                    $"Tailles catalogue non alignées à {size.Width}x{size.Height}.");
                Assert.AreEqual(
                    imagePreview.Bounds,
                    videoPreview.Bounds,
                    $"Previews non alignées à {size.Width}x{size.Height}.");
                Assert.AreEqual(
                    imageHistory.Bounds,
                    videoHistory.Bounds,
                    $"Historiques non alignés à {size.Width}x{size.Height}.");
                Assert.AreEqual(
                    imageGenerate.Bounds,
                    videoGenerate.Bounds,
                    $"Boutons Générer non alignés à {size.Width}x{size.Height}.");
                Assert.AreEqual(
                    imageStatus.Bounds,
                    videoStatus.Bounds,
                    $"Zones d'état non alignées à {size.Width}x{size.Height}.");
                Assert.AreEqual(
                    imageProgress.Bounds,
                    videoProgress.Bounds,
                    $"Progressions non alignées à {size.Width}x{size.Height}.");

                Assert.AreEqual(
                    330,
                    videoPrompt.Width,
                    $"Prompt Vidéo hors colonne commune à {size.Width}x{size.Height}.");
                Assert.AreEqual(
                    330,
                    videoNegative.Width,
                    $"Négatif Vidéo hors colonne commune à {size.Width}x{size.Height}.");

                var workspace = (Size)(
                    workspaceMethod.Invoke(form, null)
                    ?? throw new InvalidOperationException(
                        "GetSharedWorkspaceSizeV37 n'a renvoyé aucune taille."));

                Assert.IsTrue(
                    imageHistory.Bottom <= workspace.Height,
                    $"Historique Image coupé à {size.Width}x{size.Height}: " +
                    $"{imageHistory.Bottom}>{workspace.Height}.");
                Assert.IsTrue(
                    videoHistory.Bottom <= workspace.Height,
                    $"Historique Vidéo coupé à {size.Width}x{size.Height}: " +
                    $"{videoHistory.Bottom}>{workspace.Height}.");
                Assert.IsTrue(
                    imageHistory.Right <= workspace.Width,
                    $"Historique Image déborde horizontalement à {size.Width}x{size.Height}.");
                Assert.IsTrue(
                    videoHistory.Right <= workspace.Width,
                    $"Historique Vidéo déborde horizontalement à {size.Width}x{size.Height}.");
                Assert.IsTrue(
                    videoCatalogProgress.Right <= workspace.Width,
                    $"Progression catalogue Vidéo déborde à {size.Width}x{size.Height}.");
                Assert.IsTrue(
                    imageCatalogProgress.Right <= workspace.Width,
                    $"Progression catalogue Image déborde à {size.Width}x{size.Height}.");
                Assert.IsTrue(
                    imageBenchmark.Bottom <= workspace.Height,
                    $"Benchmark Image coupé à {size.Width}x{size.Height}.");
                Assert.IsTrue(
                    imageProgress.Bottom <= workspace.Height,
                    $"Progression Image coupée à {size.Width}x{size.Height}.");
                Assert.IsTrue(
                    videoProgress.Bottom <= workspace.Height,
                    $"Progression Vidéo coupée à {size.Width}x{size.Height}.");
                Assert.IsTrue(
                    videoStatus.Bottom <= workspace.Height,
                    $"Statut Vidéo coupé à {size.Width}x{size.Height}.");
            }

            return Task.CompletedTask;
        });

    [TestMethod]
    public void AuxiliaryTabs_FitCompactAndLargeWorkspaces()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();

            var layout = typeof(MainForm).GetMethod(
                "ApplyV37SharedLayout",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "ApplyV37SharedLayout");

            var workspaceMethod = typeof(MainForm).GetMethod(
                "GetSharedWorkspaceSizeV37",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "GetSharedWorkspaceSizeV37");

            var controls = new Control[]
            {
                GetField<Button>(form, "btnSettingsSave"),
                GetField<Button>(form, "btnOpenConfigFolder"),
                GetField<Label>(form, "lblConfigHint"),
                GetField<TextBox>(form, "txtGitHubRepo"),
                GetField<TextBox>(form, "_cfgVideoModel"),
                GetField<TextBox>(form, "_cfgVideoTextEncoder"),
                GetField<TextBox>(form, "_cfgVideoVae"),
                GetField<Label>(form, "_installVideoModelsStatus"),
                GetField<TextBox>(form, "_installComponentsStatus"),
                GetField<RichTextBox>(form, "_installLog"),
                GetField<Label>(form, "lblAboutTitle"),
                GetField<Label>(form, "lblAboutDescription"),
                GetField<Label>(form, "_aboutUpdateStatus"),
                GetField<ProgressBar>(form, "_aboutUpdateProgress"),
                GetField<Button>(form, "btnCheckUpdates"),
                GetField<Button>(form, "btnOpenGitHub")
            };

            foreach (var size in new[]
                     {
                         new Size(900, 620),
                         new Size(1080, 720),
                         new Size(1440, 900),
                         new Size(1920, 1032)
                     })
            {
                form.Size = size;
                form.PerformLayout();
                layout.Invoke(form, null);

                var workspace = (Size)(
                    workspaceMethod.Invoke(form, null)
                    ?? throw new InvalidOperationException(
                        "GetSharedWorkspaceSizeV37 n'a renvoyé aucune taille."));

                foreach (var control in controls)
                {
                    Assert.IsTrue(
                        control.Left >= 0 &&
                        control.Top >= 0 &&
                        control.Right <= workspace.Width &&
                        control.Bottom <= workspace.Height,
                        $"{control.Name} déborde à {size.Width}x{size.Height}: " +
                        $"{control.Bounds} hors {workspace.Width}x{workspace.Height}.");
                }
            }

            return Task.CompletedTask;
        });

    [TestMethod]
    public void Catalogs_ShowModelsAndLoras_BeforeDownload()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();

            var imageModels = GetField<ComboBox>(
                form,
                "_imageModelRuntimeCombo");
            var videoModels = GetField<ComboBox>(
                form,
                "_videoModelRuntimeCombo");
            var imageLoras = GetField<ComboBox>(
                form,
                "_imageLoraCombo");
            var videoLoras = GetField<ComboBox>(
                form,
                "_videoLoraCombo");

            AssertComboContains(
                imageModels,
                "FLUX.2 Klein 4B FP8",
                "Le modèle Image officiel doit rester visible dans le catalogue.");
            AssertComboContains(
                imageModels,
                "Uncensored",
                "L'encodeur FLUX.2 uncensored doit être visible avant téléchargement.");
            AssertComboContains(
                videoModels,
                "Wan 2.1 T2V 1.3B",
                "Le modèle vidéo officiel doit rester visible dans le catalogue.");
            AssertComboContains(
                videoModels,
                "NSFW / uncensored",
                "Le modèle Wan NSFW doit être visible avant téléchargement.");

            var fillLora = typeof(MainForm).GetMethod(
                "FillLoraComboV37",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "FillLoraComboV37");

            // Force un catalogue sans aucun LoRA installé afin de vérifier
            // précisément le cas demandé : les entrées restent visibles.
            fillLora.Invoke(
                form,
                [imageLoras, Array.Empty<string>(), string.Empty, false]);
            fillLora.Invoke(
                form,
                [videoLoras, Array.Empty<string>(), string.Empty, true]);

            AssertComboContains(
                imageLoras,
                "[↓]",
                "Un LoRA Image non installé doit rester visible avec le marqueur ↓.");
            AssertComboContains(
                imageLoras,
                "nudité anime",
                "Le LoRA adulte Image doit être présent dans le catalogue.");
            AssertComboContains(
                videoLoras,
                "NSFW général",
                "Le LoRA NSFW général Wan doit être présent dans le catalogue.");
            AssertComboContains(
                videoLoras,
                "motion helper",
                "Le LoRA motion helper Wan doit être présent dans le catalogue.");
            AssertComboContains(
                videoLoras,
                "sex helper",
                "Le LoRA adulte Wan doit être présent dans le catalogue.");
            AssertComboContains(
                videoLoras,
                "nudité poitrine",
                "Le LoRA nudité Wan doit être présent dans le catalogue.");

            return Task.CompletedTask;
        });

    [TestMethod]
    public void CatalogDownloadMetadata_HasSizesAndCleanupRemovesPartialArtifacts()
    {
        var allEntries = GetStaticCatalogEntries("RuntimeModelCatalogV37")
            .Concat(GetStaticCatalogEntries("LoraCatalogV37"))
            .ToArray();

        foreach (var entry in allEntries)
        {
            var label = CatalogString(entry, "Label");
            var size = (long)(
                entry.GetType().GetProperty("FileSizeBytes")?.GetValue(entry)
                ?? 0L);

            Assert.IsTrue(
                size > 0,
                $"Taille de fichier absente pour {label}.");
        }

        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "DreamRaster-catalog-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var destination = Path.Combine(tempRoot, "test-model.safetensors");
        var parts = destination + ".curlparts";

        try
        {
            File.WriteAllBytes(destination, [1, 2, 3, 4]);
            Directory.CreateDirectory(parts);
            File.WriteAllBytes(
                Path.Combine(parts, "part-00.bin"),
                [5, 6, 7, 8]);

            var cleanup = typeof(MainForm).GetMethod(
                "TryDeleteCatalogDownloadArtifactsV37",
                BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "TryDeleteCatalogDownloadArtifactsV37");

            cleanup.Invoke(null, [destination, true]);

            Assert.IsFalse(
                File.Exists(destination),
                "Le fichier partiel doit être supprimé après annulation/échec.");
            Assert.IsFalse(
                Directory.Exists(parts),
                "Le dossier .curlparts doit être supprimé après annulation/échec.");
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
            catch { }
        }
    }

    [TestMethod]
    public void CatalogDownloadUi_ShowsButtonSizeProgressAndClearErrors()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();

            var imageLoras = GetField<ComboBox>(
                form,
                "_imageLoraCombo");
            var fillLora = typeof(MainForm).GetMethod(
                "FillLoraComboV37",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "FillLoraComboV37");

            fillLora.Invoke(
                form,
                [imageLoras, Array.Empty<string>(), string.Empty, false]);

            var downloadable = imageLoras.Items
                .Cast<object>()
                .FirstOrDefault(x =>
                    (x?.ToString() ?? string.Empty)
                        .Contains("[↓]", StringComparison.Ordinal))
                ?? throw new AssertFailedException(
                    "Aucun LoRA Image téléchargeable n'est visible.");

            imageLoras.SelectedItem = downloadable;

            var refresh = typeof(MainForm).GetMethod(
                "RefreshCatalogDownloadUiV37",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "RefreshCatalogDownloadUiV37");
            refresh.Invoke(form, null);

            var button = GetField<Button>(
                form,
                "_imageLoraDownloadButton");
            var size = GetField<Label>(
                form,
                "_imageCatalogDownloadSize");
            var progress = GetField<ProgressBar>(
                form,
                "_imageCatalogDownloadProgress");
            var cancel = GetField<Button>(
                form,
                "_imageCatalogDownloadCancelButton");

            StringAssert.Contains(
                button.Text,
                "Télécharger",
                "Le bouton doit annoncer clairement l'action Télécharger.");
            StringAssert.Contains(
                size.Text,
                "Taille",
                "La taille du fichier doit être affichée avant téléchargement.");
            Assert.IsTrue(
                size.Text.Contains("Mo", StringComparison.OrdinalIgnoreCase) ||
                size.Text.Contains("Go", StringComparison.OrdinalIgnoreCase),
                "La taille doit être présentée dans une unité lisible.");
            Assert.AreEqual(
                0,
                progress.Value,
                "La progression initiale doit commencer à zéro.");
            Assert.IsFalse(
                cancel.Visible,
                "Annuler ne doit apparaître que pendant un téléchargement actif.");

            var errorFormatter = typeof(MainForm).GetMethod(
                "FormatCatalogDownloadErrorV37",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "FormatCatalogDownloadErrorV37");

            var clearError = (string)(
                errorFormatter.Invoke(
                    form,
                    [
                        "Modèle test",
                        new InvalidOperationException("HTTP 403 Forbidden")
                    ])
                ?? string.Empty);

            StringAssert.Contains(
                clearError,
                "Hugging Face",
                "Un refus HTTP doit expliquer clairement le cas Hugging Face.");
            StringAssert.Contains(
                clearError,
                "403",
                "Le détail technique original doit rester visible.");

            return Task.CompletedTask;
        });

    [TestMethod]
    public void VideoCatalogDownloadButtons_AreVisibleForUninstalledSelections()
        => RunOnStaAsync(() =>
        {
            using var form = new MainForm();
            var videoTab = GetField<TabPage>(form, "_tabVideo");

            var videoModels = GetField<ComboBox>(
                form,
                "_videoModelRuntimeCombo");
            var videoLoras = GetField<ComboBox>(
                form,
                "_videoLoraCombo");

            var fillLora = typeof(MainForm).GetMethod(
                "FillLoraComboV37",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "FillLoraComboV37");
            fillLora.Invoke(
                form,
                [videoLoras, Array.Empty<string>(), string.Empty, true]);

            var model = videoModels.Items
                .Cast<object>()
                .FirstOrDefault(x =>
                    (x?.ToString() ?? string.Empty)
                        .Contains("[↓]", StringComparison.Ordinal))
                ?? throw new AssertFailedException(
                    "Aucun modèle Vidéo non installé n'est visible.");

            var lora = videoLoras.Items
                .Cast<object>()
                .FirstOrDefault(x =>
                    (x?.ToString() ?? string.Empty)
                        .Contains("[↓]", StringComparison.Ordinal))
                ?? throw new AssertFailedException(
                    "Aucun LoRA Vidéo non installé n'est visible.");

            videoModels.SelectedItem = model;
            videoLoras.SelectedItem = lora;

            var refresh = typeof(MainForm).GetMethod(
                "RefreshCatalogDownloadUiV37",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(MainForm).FullName,
                    "RefreshCatalogDownloadUiV37");
            refresh.Invoke(form, null);

            var modelButton = GetField<Button>(
                form,
                "_videoModelDownloadButton");
            var loraButton = GetField<Button>(
                form,
                "_videoLoraDownloadButton");
            var size = GetField<Label>(
                form,
                "_videoCatalogDownloadSize");
            var progress = GetField<ProgressBar>(
                form,
                "_videoCatalogDownloadProgress");

            // Control.Visible inclut la visibilité des parents. Le formulaire
            // reste volontairement caché pour ne pas déclencher MainForm_Shown.
            // Détacher les contrôles révèle donc leur état Visible local,
            // exactement celui réglé par RefreshCatalogDownloadUiV37.
            videoTab.Controls.Remove(modelButton);
            videoTab.Controls.Remove(loraButton);
            videoTab.Controls.Remove(size);
            videoTab.Controls.Remove(progress);

            Assert.IsTrue(
                modelButton.Visible,
                "Télécharger Modèle Vidéo doit être visible pour une entrée ↓.");
            Assert.IsTrue(
                loraButton.Visible,
                "Télécharger LoRA Vidéo doit être visible pour une entrée ↓.");
            Assert.IsTrue(
                size.Visible,
                "La taille doit être visible pour les entrées Vidéo non installées.");
            Assert.IsTrue(
                progress.Visible,
                "La barre de progression doit être visible à 0% avant téléchargement.");

            return Task.CompletedTask;
        });

    [TestMethod]
    public void CatalogEntries_AllHaveKnownFileSizes()
    {
        var entries = GetStaticCatalogEntries("RuntimeModelCatalogV37")
            .Concat(GetStaticCatalogEntries("LoraCatalogV37"))
            .ToArray();

        Assert.IsTrue(
            entries.Length >= 10,
            "Le test doit couvrir l'ensemble du catalogue modèles + LoRA.");

        foreach (var entry in entries)
        {
            var label = CatalogString(entry, "Label");
            var size = (long)(
                entry.GetType().GetProperty("FileSizeBytes")?.GetValue(entry)
                ?? 0L);

            Assert.IsTrue(
                size > 0,
                $"Taille de fichier inconnue dans le catalogue : {label}.");
        }
    }

    [TestMethod]
    public void AdultCatalogEntries_HaveDirectLinksAndLicenseMetadata()
    {
        var runtimeEntries = GetStaticCatalogEntries(
            "RuntimeModelCatalogV37");
        var loraEntries = GetStaticCatalogEntries(
            "LoraCatalogV37");

        var adultRuntime = runtimeEntries
            .Where(x => CatalogBool(x, "AdultOnly"))
            .ToArray();
        var adultLoras = loraEntries
            .Where(x => CatalogBool(x, "AdultOnly"))
            .ToArray();

        Assert.AreEqual(
            3,
            adultRuntime.Length,
            "Le catalogue doit contenir les trois modèles/encodeurs 18+ documentés.");
        Assert.AreEqual(
            5,
            adultLoras.Length,
            "Le catalogue doit contenir les cinq LoRA 18+ documentés.");

        foreach (var entry in adultRuntime.Concat(adultLoras))
            AssertAdultCatalogMetadata(entry);

        AssertCatalogLicense(
            adultRuntime,
            "encodeur abliterated",
            "other");
        AssertCatalogLicense(
            adultRuntime,
            "T2V e11 alpha",
            "creativeml-openrail-m");
        AssertCatalogLicense(
            adultRuntime,
            "I2V",
            "apache-2.0");

        AssertCatalogLicense(
            adultLoras,
            "nudité anime",
            "non-specifiee");
        AssertCatalogLicense(
            adultLoras,
            "NSFW général",
            "creativeml-openrail-m");
        AssertCatalogLicense(
            adultLoras,
            "motion helper",
            "non-specifiee");
        AssertCatalogLicense(
            adultLoras,
            "sex helper",
            "non-specifiee");
        AssertCatalogLicense(
            adultLoras,
            "nudité poitrine",
            "non-specifiee");
    }

    private static async Task VerifyScenarioAsync(GpuExit exit)
    {
        using var form = new MainForm();

        var controls = InvokeGpuWorkspaceControls(form)
            .Distinct()
            .ToArray();

        Assert.IsTrue(
            controls.Length >= 30,
            "Le test doit couvrir l'ensemble des contrôles interactifs Image/Vidéo. " +
            $"Seulement {controls.Length} contrôles ont été trouvés.");

        AssertRequiredControlsAreCovered(form, controls);

        // Simule une indisponibilité indépendante du GPU. Le déverrouillage ne
        // doit jamais transformer cet état en Enabled=true.
        var externallyDisabled =
            GetField<Control>(form, "_imageImportModelButton");
        externallyDisabled.Enabled = false;

        var initialEnabled = controls.ToDictionary(
            control => control,
            control => control.Enabled);

        Assert.IsFalse(
            initialEnabled[externallyDisabled],
            "Le contrôle témoin doit être désactivé avant le verrouillage GPU.");

        var entered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();

        async Task FakeGpuGeneration()
        {
            entered.TrySetResult();

            switch (exit)
            {
                case GpuExit.Success:
                    await release.Task;
                    return;

                case GpuExit.Cancellation:
                    await Task.Delay(
                        Timeout.InfiniteTimeSpan,
                        cancellation.Token);
                    return;

                case GpuExit.Timeout:
                    await release.Task;
                    throw new TimeoutException(
                        "Timeout GPU simulé par le test de régression.");

                case GpuExit.UnexpectedException:
                    await release.Task;
                    throw new InvalidOperationException(
                        "Exception GPU inattendue simulée par le test de régression.");

                default:
                    throw new ArgumentOutOfRangeException(nameof(exit));
            }
        }

        var run = InvokeGpuExclusiveAsync(
            form,
            "Régression verrouillage GPU",
            FakeGpuGeneration);

        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var stillEnabled = controls
            .Where(control => control.Enabled)
            .Select(control => DescribeControl(form, control))
            .ToArray();

        Assert.AreEqual(
            0,
            stillEnabled.Length,
            "Des contrôles Image/Vidéo restent actifs pendant la génération GPU : " +
            string.Join(", ", stillEnabled));

        switch (exit)
        {
            case GpuExit.Success:
                release.TrySetResult();
                await run.WaitAsync(TimeSpan.FromSeconds(5));
                break;

            case GpuExit.Cancellation:
                cancellation.Cancel();

                try
                {
                    await run.WaitAsync(TimeSpan.FromSeconds(5));
                    Assert.Fail(
                        "La génération GPU simulée aurait dû être annulée.");
                }
                catch (OperationCanceledException)
                {
                    // TaskCanceledException est une annulation .NET normale.
                }
                break;

            case GpuExit.Timeout:
                release.TrySetResult();

                await Assert.ThrowsExceptionAsync<TimeoutException>(
                    async () =>
                        await run.WaitAsync(TimeSpan.FromSeconds(5)));
                break;

            case GpuExit.UnexpectedException:
                release.TrySetResult();

                await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                    async () =>
                        await run.WaitAsync(TimeSpan.FromSeconds(5)));
                break;
        }

        var restorationErrors = controls
            .Where(control =>
                control.Enabled != initialEnabled[control])
            .Select(control =>
                $"{DescribeControl(form, control)} : " +
                $"attendu Enabled={initialEnabled[control]}, " +
                $"obtenu Enabled={control.Enabled}")
            .ToArray();

        Assert.AreEqual(
            0,
            restorationErrors.Length,
            "L'état des contrôles Image/Vidéo n'a pas été restauré après " +
            $"{exit} : {string.Join(" | ", restorationErrors)}");

        Assert.IsFalse(
            externallyDisabled.Enabled,
            "Un contrôle indisponible avant l'opération GPU a été réactivé à tort.");

        // Vérifie aussi que le SemaphoreSlim est libéré dans le finally :
        // une seconde opération doit pouvoir démarrer immédiatement.
        await InvokeGpuExclusiveAsync(
                form,
                "Régression libération verrou GPU",
                () => Task.CompletedTask)
            .WaitAsync(TimeSpan.FromSeconds(5));
    }

    private static void AssertRequiredControlsAreCovered(
        MainForm form,
        IReadOnlyCollection<Control> controls)
    {
        foreach (var fieldName in RequiredGpuControlFields)
        {
            var control = GetField<Control>(form, fieldName);

            Assert.IsTrue(
                controls.Contains(control),
                $"Le contrôle obligatoire {fieldName} n'est pas couvert par " +
                "EnumerateGpuWorkspaceInteractiveControls().");
        }
    }



    private static IEnumerable<T> EnumerateControls<T>(Control parent)
        where T : Control
    {
        foreach (Control child in parent.Controls)
        {
            if (child is T match)
                yield return match;

            foreach (var nested in EnumerateControls<T>(child))
                yield return nested;
        }
    }

    private static void FindDuplicateTabIndices(
        Control parent,
        string path,
        List<string> duplicates)
    {
        var tabbable = parent.Controls
            .Cast<Control>()
            .Where(c =>
                IsControlLocallyVisible(c) &&
                c.TabStop &&
                c.Enabled)
            .ToArray();

        foreach (var group in tabbable
                     .GroupBy(c => c.TabIndex)
                     .Where(g => g.Count() > 1))
        {
            duplicates.Add(
                $"{path} TabIndex={group.Key}: " +
                string.Join(
                    ", ",
                    group.Select(c =>
                        $"{c.Name}/{c.GetType().Name}@{c.Left},{c.Top}")));
        }

        foreach (Control child in parent.Controls)
        {
            if (child.HasChildren)
                FindDuplicateTabIndices(
                    child,
                    path + ">" + child.Name,
                    duplicates);
        }
    }

    private static void AssertDynamicLabelHandlesLongText(Label label)
    {
        if (label.AutoEllipsis)
            return;

        var available = new Size(
            Math.Max(1, label.ClientSize.Width - label.Padding.Horizontal),
            Math.Max(1, label.ClientSize.Height - label.Padding.Vertical));

        var measured = TextRenderer.MeasureText(
            label.Text,
            label.Font,
            new Size(available.Width, 10000),
            TextFormatFlags.WordBreak |
            TextFormatFlags.NoPrefix |
            TextFormatFlags.NoPadding);

        Assert.IsTrue(
            measured.Height <= available.Height + 2,
            $"Le texte dynamique long de {label.Name} n'est ni entièrement visible " +
            $"ni protégé par AutoEllipsis : needs={measured.Width}x{measured.Height}, " +
            $"has={available.Width}x{available.Height}, text='{label.Text}'.");
    }

    private static IEnumerable<string> FindTruncatedStaticText(
        Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (!IsControlLocallyVisible(child) ||
                child.Width <= 1 ||
                child.Height <= 1 ||
                string.IsNullOrWhiteSpace(child.Text))
            {
                foreach (var nested in FindTruncatedStaticText(child))
                    yield return nested;
                continue;
            }

            var text = child.Text.Trim();
            var availableWidth = Math.Max(
                1,
                child.ClientSize.Width -
                child.Padding.Horizontal);
            var availableHeight = Math.Max(
                1,
                child.ClientSize.Height -
                child.Padding.Vertical);
            var shouldCheck = true;
            var wordBreak = false;

            switch (child)
            {
                case Button:
                    availableWidth = Math.Max(1, availableWidth - 12);
                    break;

                case CheckBox:
                case RadioButton:
                    availableWidth = Math.Max(1, availableWidth - 22);
                    break;

                case GroupBox:
                    availableWidth = Math.Max(1, availableWidth - 18);
                    availableHeight = child.Font.Height + 4;
                    break;

                case Label label when !label.AutoSize:
                    wordBreak =
                        availableHeight >= (label.Font.Height * 2) ||
                        text.Contains('\n');
                    break;

                case Label:
                    shouldCheck = false;
                    break;

                default:
                    shouldCheck = false;
                    break;
            }

            if (shouldCheck)
            {
                var flags =
                    TextFormatFlags.NoPrefix |
                    TextFormatFlags.NoPadding |
                    (wordBreak
                        ? TextFormatFlags.WordBreak
                        : TextFormatFlags.SingleLine);

                var measured = wordBreak
                    ? TextRenderer.MeasureText(
                        text,
                        child.Font,
                        new Size(availableWidth, 10000),
                        flags)
                    : TextRenderer.MeasureText(
                        text,
                        child.Font,
                        Size.Empty,
                        flags);

                if (measured.Width > availableWidth + 2 ||
                    measured.Height > availableHeight + 2)
                {
                    yield return
                        $"{parent.Name}->{child.Name}/{child.GetType().Name} " +
                        $"'{text}' needs={measured.Width}x{measured.Height} " +
                        $"has={availableWidth}x{availableHeight}";
                }
            }

            foreach (var nested in FindTruncatedStaticText(child))
                yield return nested;
        }
    }

    private static IEnumerable<string> FindUnexpectedSiblingOverlaps(
        Control parent)
    {
        var children = parent.Controls
            .Cast<Control>()
            .Where(IsControlLocallyVisible)
            .Where(x => x.Width > 1 && x.Height > 1)
            .Where(x => x.Dock == DockStyle.None)
            .ToArray();

        for (var i = 0; i < children.Length; i++)
        {
            for (var j = i + 1; j < children.Length; j++)
            {
                var a = children[i];
                var b = children[j];
                var intersection = Rectangle.Intersect(
                    a.Bounds,
                    b.Bounds);

                if (intersection.IsEmpty ||
                    intersection.Width <= 2 ||
                    intersection.Height <= 2)
                {
                    continue;
                }

                var minArea = Math.Min(
                    a.Width * a.Height,
                    b.Width * b.Height);
                var overlapArea =
                    intersection.Width *
                    intersection.Height;

                if (minArea <= 0 ||
                    overlapArea / (double)minArea < 0.30)
                {
                    continue;
                }

                // Labels may intentionally overlay a decorative container,
                // but two interactive controls or a control covered by a
                // non-label sibling are never intentional here.
                if (a is Label && b is Label)
                    continue;

                yield return
                    $"{a.Name}/{a.GetType().Name} {a.Bounds} <-> " +
                    $"{b.Name}/{b.GetType().Name} {b.Bounds}";
            }
        }

        foreach (Control child in children)
        {
            foreach (var nested in FindUnexpectedSiblingOverlaps(child))
                yield return nested;
        }
    }

    private static bool IsControlLocallyVisible(Control control)
    {
        // Control.Visible tient compte des parents ; dans ce test les
        // TabPages ne sont pas affichées simultanément. GetState(STATE_VISIBLE)
        // permet de tester l'intention locale du contrôle sans faux négatif.
        var method = typeof(Control).GetMethod(
            "GetState",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (method is null)
            return control.Visible;

        return (bool)(method.Invoke(control, [2]) ?? false);
    }

    private static IEnumerable<string> FindClippedControls(Control parent)
    {
        if (parent is ScrollableControl scroll && scroll.AutoScroll)
            yield break;

        foreach (Control child in parent.Controls)
        {
            if (!IsControlLocallyVisible(child) ||
                child.Width <= 0 ||
                child.Height <= 0)
            {
                continue;
            }

            var client = parent.ClientRectangle;
            var bounds = child.Bounds;

            var clipped =
                bounds.Left < client.Left ||
                bounds.Top < client.Top ||
                bounds.Right > client.Right + 1 ||
                bounds.Bottom > client.Bottom + 1;

            if (clipped)
            {
                yield return
                    $"{parent.Name}->{child.Name} " +
                    $"B={bounds.X},{bounds.Y},{bounds.Width},{bounds.Height} " +
                    $"Parent={client.Width}x{client.Height}";
            }

            foreach (var nested in FindClippedControls(child))
                yield return nested;
        }
    }

    private static bool IsLocallyVisible(Control control)
    {
        var getState = typeof(Control).GetMethod(
            "GetState",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(
                typeof(Control).FullName,
                "GetState");

        return (bool)(
            getState.Invoke(control, [2])
            ?? false);
    }

    private static object[] GetStaticCatalogEntries(string fieldName)
    {
        var field = typeof(MainForm).GetField(
            fieldName,
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(
                typeof(MainForm).FullName,
                fieldName);

        var value = field.GetValue(null) as System.Collections.IEnumerable
            ?? throw new InvalidOperationException(
                $"{fieldName} n'est pas une collection.");

        return value.Cast<object>().ToArray();
    }

    private static bool CatalogBool(object entry, string propertyName) =>
        (bool)(
            entry.GetType().GetProperty(propertyName)?.GetValue(entry)
            ?? throw new MissingMemberException(
                entry.GetType().FullName,
                propertyName));

    private static string CatalogString(object entry, string propertyName) =>
        entry.GetType().GetProperty(propertyName)?.GetValue(entry)?.ToString()
        ?? string.Empty;

    private static void AssertAdultCatalogMetadata(object entry)
    {
        var label = CatalogString(entry, "Label");
        var direct = CatalogString(entry, "Url");
        var source = CatalogString(entry, "SourceUrl");
        var license = CatalogString(entry, "LicenseId");
        var note = CatalogString(entry, "LicenseNote");
        var size = (long)(
            entry.GetType().GetProperty("FileSizeBytes")?.GetValue(entry)
            ?? 0L);

        Assert.IsTrue(
            size > 0,
            $"Taille de fichier manquante pour {label}.");
        Assert.IsTrue(
            Uri.TryCreate(direct, UriKind.Absolute, out var directUri) &&
            directUri.Scheme == Uri.UriSchemeHttps,
            $"Lien direct HTTPS manquant pour {label}.");
        Assert.IsTrue(
            direct.Contains("/resolve/", StringComparison.OrdinalIgnoreCase),
            $"Le lien de {label} doit pointer directement vers un fichier Hugging Face.");
        Assert.IsTrue(
            Uri.TryCreate(source, UriKind.Absolute, out var sourceUri) &&
            sourceUri.Scheme == Uri.UriSchemeHttps,
            $"Page source HTTPS manquante pour {label}.");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(license),
            $"Licence manquante pour {label}.");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(note),
            $"Note de licence manquante pour {label}.");
    }

    private static void AssertCatalogLicense(
        IEnumerable<object> entries,
        string labelFragment,
        string expectedLicense)
    {
        var entry = entries.FirstOrDefault(x =>
            CatalogString(x, "Label").Contains(
                labelFragment,
                StringComparison.OrdinalIgnoreCase))
            ?? throw new AssertFailedException(
                $"Entrée de catalogue introuvable : {labelFragment}.");

        Assert.AreEqual(
            expectedLicense,
            CatalogString(entry, "LicenseId"),
            $"Licence inattendue pour {CatalogString(entry, "Label")}.");
    }

    private static void AssertComboContains(
        ComboBox combo,
        string expected,
        string message)
    {
        var displays = combo.Items
            .Cast<object>()
            .Select(x => x?.ToString() ?? string.Empty)
            .ToArray();

        Assert.IsTrue(
            displays.Any(x =>
                x.Contains(
                    expected,
                    StringComparison.OrdinalIgnoreCase)),
            message + Environment.NewLine +
            "Contenu : " + string.Join(" | ", displays));
    }

    private static IEnumerable<Control> InvokeGpuWorkspaceControls(
        MainForm form)
    {
        var method = typeof(MainForm).GetMethod(
            "EnumerateGpuWorkspaceInteractiveControls",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(
                typeof(MainForm).FullName,
                "EnumerateGpuWorkspaceInteractiveControls");

        return method.Invoke(form, null) as IEnumerable<Control>
            ?? throw new InvalidOperationException(
                "EnumerateGpuWorkspaceInteractiveControls n'a pas renvoyé " +
                "IEnumerable<Control>.");
    }

    private static Task InvokeGpuExclusiveAsync(
        MainForm form,
        string operation,
        Func<Task> action)
    {
        var method = typeof(MainForm)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate =>
                candidate.Name == "RunGpuExclusiveAsync" &&
                !candidate.IsGenericMethod &&
                candidate.GetParameters() is
                [
                    { ParameterType: var first },
                    { ParameterType: var second }
                ] &&
                first == typeof(string) &&
                second == typeof(Func<Task>));

        return (Task)(method.Invoke(
            form,
            [operation, action])
            ?? throw new InvalidOperationException(
                "RunGpuExclusiveAsync n'a renvoyé aucune Task."));
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
                $"Le champ {name} n'est pas un {typeof(T).Name}.");
    }

    private static string DescribeControl(
        MainForm form,
        Control control)
    {
        var fieldName = typeof(MainForm)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(field =>
                typeof(Control).IsAssignableFrom(field.FieldType) &&
                ReferenceEquals(
                    field.GetValue(form),
                    control))
            ?.Name;

        return fieldName
            ?? (!string.IsNullOrWhiteSpace(control.Name)
                ? control.Name
                : control.GetType().Name);
    }

    private static void RunOnStaAsync(
        Func<Task> scenario)
    {
        Exception? failure = null;
        using var finished = new ManualResetEventSlim();

        var thread = new Thread(() =>
        {
            var context = new ApplicationContext();
            using var timer = new System.Windows.Forms.Timer
            {
                Interval = 1
            };

            timer.Tick += async (_, _) =>
            {
                timer.Stop();

                try
                {
                    await scenario();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    context.ExitThread();
                    finished.Set();
                }
            };

            timer.Start();
            Application.Run(context);
        })
        {
            IsBackground = true,
            Name = "DreamRaster GPU UI lock regression"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!finished.Wait(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException(
                "Le test WinForms STA n'a pas terminé en 30 secondes.");
        }

        thread.Join(TimeSpan.FromSeconds(2));

        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private enum GpuExit
    {
        Success,
        Cancellation,
        Timeout,
        UnexpectedException
    }
}
