using System.ComponentModel;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenCodeLocalAI;

namespace DreamRaster.Tests;

[TestClass]
[DoNotParallelize]
public sealed class UiInteractionSmokeTests
{
    [TestMethod]
    public void WanDecodeTimeout_AdaptsToVideoDimensions()
    {
        var method = typeof(VideoGenerator).GetMethod(
            "GetVideoDecodeTimeout",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        Assert.AreEqual(TimeSpan.FromMinutes(8),
            method.Invoke(null, new object[] { 256, 256, 5 }));
        Assert.AreEqual(TimeSpan.FromMinutes(8),
            method.Invoke(null, new object[] { 256, 256, 17 }));
        Assert.AreEqual(TimeSpan.FromMinutes(20),
            method.Invoke(null, new object[] { 832, 480, 33 }));
    }

    [TestMethod]
    public void WanLatentTimeout_AdaptsToVideoWorkload()
    {
        var method = typeof(VideoGenerator).GetMethod(
            "GetLatentGenerationTimeout",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        Assert.AreEqual(TimeSpan.FromMinutes(20),
            method.Invoke(null, new object[] { 256, 256, 5, 12 }));
        Assert.AreEqual(TimeSpan.FromMinutes(60),
            method.Invoke(null, new object[] { 832, 480, 33, 50 }));
    }

    [TestMethod]
    public void Wan14BWorkloadHint_DistinguishesSmallAndHeavyVideo()
    {
        var method = typeof(MainForm).GetMethod(
            "IsHeavyWanI2vWorkload",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        Assert.AreEqual(false, method.Invoke(null, new object[] { 256, 256, 5 }));
        Assert.AreEqual(false, method.Invoke(null, new object[] { 256, 256, 17 }));
        Assert.AreEqual(true, method.Invoke(null, new object[] { 832, 480, 33 }));
    }

    [TestMethod]
    public void VideoExportSignature_RejectsInvalidContainers()
    {
        var method = typeof(VideoGenerator).GetMethod(
            "HasValidVideoFileHeader",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);

        var directory = Path.Combine(Path.GetTempPath(), "dreamraster-video-header-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var mp4 = Path.Combine(directory, "valid.mp4");
            var invalid = Path.Combine(directory, "invalid.mp4");
            var webm = Path.Combine(directory, "valid.webm");
            File.WriteAllBytes(mp4, new byte[] { 0,0,0,16, 102,116,121,112, 105,115,111,109 });
            File.WriteAllBytes(invalid, new byte[] { 0,0,0,16, 0,0,0,0, 105,115,111,109 });
            File.WriteAllBytes(webm, new byte[] { 0x1A,0x45,0xDF,0xA3, 0,0,0,0,0,0,0,0 });
            Assert.AreEqual(true, method.Invoke(null, new object?[] { mp4 }));
            Assert.AreEqual(false, method.Invoke(null, new object?[] { invalid }));
            Assert.AreEqual(true, method.Invoke(null, new object?[] { webm }));
            Assert.AreEqual(false, method.Invoke(null, new object?[] { Path.Combine(directory, "missing.mp4") }));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void VideoReferenceField_AllowsPastedPathBeforeValidation()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var reference = GetField<TextBox>(form, "_videoReferenceImage");
            Assert.IsTrue(reference.Enabled);
            Assert.IsFalse(reference.ReadOnly);
            reference.Text = @"C:\test\reference.png";
            Assert.AreEqual(@"C:\test\reference.png", reference.Text);
        });

    [TestMethod]
    public void ComfyStartupArguments_IncludeLowVramOnlyWhenRequested()
    {
        var method = typeof(MainForm).GetMethod(
            "BuildComfyArgumentsForMemoryMode",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);

        const string main = @"C:\portable test\ComfyUI\main.py";
        var normal = (string)method.Invoke(null, new object[] { main, 8188, false })!;
        var reduced = (string)method.Invoke(null, new object[] { main, 8188, true })!;

        StringAssert.Contains(normal, "\"" + main + "\"");
        StringAssert.Contains(normal, "--port 8188");
        StringAssert.Contains(normal, "--disable-async-offload");
        Assert.IsFalse(normal.Contains("--lowvram", StringComparison.Ordinal),
            "Le mode normal ne doit pas activer LOW_VRAM.");
        Assert.AreEqual(normal + " --lowvram", reduced,
            "Le mode réduit ne doit ajouter que l'option mémoire prévue.");
    }

    [TestMethod]
    public void SwitchingVideoMemoryMode_RestartsOnlyWhenNeeded()
    {
        var method = typeof(MainForm).GetMethod(
            "ShouldRestartComfyForMemoryMode",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);

        foreach (var (running, previous, required, expected) in new[]
        {
            (true, (bool?)false, true, true),
            (true, (bool?)true, false, true),
            (true, (bool?)true, true, false),
            (true, (bool?)false, false, false),
            (true, (bool?)null, true, false),
            (false, (bool?)false, true, false)
        })
        {
            Assert.AreEqual(expected,
                method.Invoke(null, new object?[] { running, previous, required }),
                "Un redémarrage n'est nécessaire qu'au changement de mode connu.");
        }
    }

    [TestMethod]
    public void Wan14BI2V_ActivatesLowVramOnlyForHeavyVideoModel()
    {
        var method = typeof(MainForm).GetMethod(
            "RequiresLowVramForVideoModel",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);

        foreach (var model in new[]
        {
            "wan2.1_i2v_480p_14B_fp8_e4m3fn.safetensors",
            "WAN13B-OTHER-I2V-14B.safetensors"
        })
        {
            Assert.AreEqual(true, method.Invoke(null, new object?[] { model }),
                "Le mode réduit doit être activé pour " + model);
        }

        foreach (var model in new string?[]
        {
            null,
            string.Empty,
            "wan2.1_t2v_1.3B_fp16.safetensors",
            "wan_1.3B_e11_alpha.safetensors",
            "flux-2-klein-4b-fp8.safetensors",
            "wan2.1_t2v_14B_fp8.safetensors"
        })
        {
            Assert.AreEqual(false, method.Invoke(null, new object?[] { model }),
                "Ne pas ralentir les autres modèles : " + model);
        }
    }

    [TestMethod]
    public void MissingPipelineComponents_ResetProfileToCustom()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var settings = GetField<AppSettings>(form, "_s");
            SetPrivateField(form, "_suppressSettingsPersistence", true);
            settings.ComfyMain = Path.Combine(
                PortablePaths.RuntimeDir,
                "dreamraster-missing-preset-" + Guid.NewGuid().ToString("N"),
                "main.py");

            foreach (var (comboName, settingName, statusName) in new[]
            {
                ("_imagePipelinePresetCombo", "ImagePipelinePreset", "_genText"),
                ("_videoPipelinePresetCombo", "VideoPipelinePreset", "_videoStatus")
            })
            {
                var combo = GetField<ComboBox>(form, comboName);
                var selected = combo.Items.Cast<object>().First(item =>
                {
                    var id = item.GetType().GetProperty("Id")?.GetValue(item) as string;
                    return id == "max-quality";
                });

                combo.SelectedItem = selected;
                InvokeVoid(form, "ApplySelectedPipelinePreset",
                    comboName == "_videoPipelinePresetCombo");

                var actualId = typeof(AppSettings).GetProperty(settingName)!
                    .GetValue(settings) as string;
                Assert.AreEqual("custom", actualId,
                    "Un profil incomplet ne doit pas rester marqué comme actif.");

                var status = GetField<Label>(form, statusName);
                StringAssert.Contains(status.Text, "manquant",
                    "L'interface doit indiquer les composants absents.");
            }
        });

    [TestMethod]
    public void AllMainFormButtons_HaveClickHandlers()
        => RunOnSta(() =>
        {
            using var form = new MainForm();

            var buttons = EnumerateControls<Button>(form)
                .Where(button => !string.IsNullOrWhiteSpace(button.Name))
                .OrderBy(button => button.Name)
                .ToArray();

            Assert.IsTrue(buttons.Length >= 25, "Le formulaire devrait exposer les boutons principaux Image/Vidéo/Configuration.");

            var missing = buttons
                .Where(button => GetClickHandler(button, typeof(Control)) is null)
                .Select(button => button.Name)
                .ToArray();

            Assert.AreEqual(
                0,
                missing.Length,
                "Boutons sans handler Click : " + string.Join(", ", missing));
        });

    [TestMethod]
    public void RuntimeModelAndLoraMenus_BuildAndWireEveryAction()
        => RunOnSta(() =>
        {
            using var form = new MainForm();

            foreach (var language in new[] { "fr", "en" })
            {
                var settings = GetField<AppSettings>(form, "_s");
                settings.Language = language;

                using var imageModels = InvokeMenu(
                    form,
                    "BuildRuntimeModelAddMenuV36",
                    true);
                using var videoModels = InvokeMenu(
                    form,
                    "BuildRuntimeModelAddMenuV36",
                    false);
                using var imageLoras = InvokeMenu(
                    form,
                    "BuildLoraAddMenuV37",
                    false);
                using var videoLoras = InvokeMenu(
                    form,
                    "BuildLoraAddMenuV37",
                    true);

                AssertMenuTree(imageModels, "Image models");
                AssertMenuTree(videoModels, "Video models");
                AssertMenuTree(imageLoras, "Image LoRA");
                AssertMenuTree(videoLoras, "Video LoRA");
            }
        });

    [TestMethod]
    public void SafeButtons_DoNotThrowWhenNoWorkIsActive()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            _ = form.Handle;

            var names = new[]
            {
                "btnClearInputImage",
                "btnInstallCancel",
                "_imageSharpnessPreviewButton",
                "_videoSharpnessPreviewButton",
                "_videoCancelButton",
                "_videoRefreshButton",
                "_videoOpenButton",
                "_imageModelDownloadButton",
                "_videoModelDownloadButton",
                "_imageLoraDownloadButton",
                "_videoLoraDownloadButton",
                "_imageCatalogDownloadCancelButton",
                "_videoCatalogDownloadCancelButton"
            };

            foreach (var name in names)
            {
                var button = GetField<Button>(form, name);
                button.Enabled = true;
                button.PerformClick();
                Application.DoEvents();
            }
        });

    [TestMethod]
    public void CatalogDownloadButtons_WithNoSelection_DoNotThrow()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            _ = form.Handle;

            var cases = new (string Combo, string Button)[]
            {
                ("_imageModelRuntimeCombo", "_imageModelDownloadButton"),
                ("_videoModelRuntimeCombo", "_videoModelDownloadButton"),
                ("_imageLoraCombo", "_imageLoraDownloadButton"),
                ("_videoLoraCombo", "_videoLoraDownloadButton")
            };

            foreach (var item in cases)
            {
                var combo = GetField<ComboBox>(form, item.Combo);
                var button = GetField<Button>(form, item.Button);

                combo.SelectedIndex = -1;
                button.Enabled = true;

                var handler = GetClickHandler(button, typeof(Control))
                    ?? throw new InvalidOperationException(
                        $"{item.Button} n'a pas de handler Click.");

                handler.DynamicInvoke(button, EventArgs.Empty);
                Application.DoEvents();
            }
        });

    [TestMethod]
    public void CorruptImagePreview_DoesNotEscapeTheUiHandler()
        => RunOnSta(() =>
        {
            using var form = new MainForm();

            var images = PortablePaths.Resolve("images");
            Directory.CreateDirectory(images);
            var path = Path.Combine(
                images,
                "ui-smoke-corrupt-image.png");

            File.WriteAllBytes(
                path,
                new byte[] { 0x44, 0x52, 0x00, 0xFF, 0x10, 0x20 });

            try
            {
                InvokeVoid(
                    form,
                    "ShowPreviewImage",
                    path);
            }
            finally
            {
                try { File.Delete(path); }
                catch { }
            }
        });

    [TestMethod]
    public void InvalidComfyPath_VideoRefreshAndTabSelection_DoNotCrash()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            _ = form.Handle;

            var settings = GetField<AppSettings>(form, "_s");
            var originalComfyMain = settings.ComfyMain;

            try
            {
                settings.ComfyMain =
                    @"C:\outside-dreamraster\ComfyUI\main.py";

                InvokeVoid(
                    form,
                    "RefreshVideoModelStatus");

                var generate = GetField<Button>(
                    form,
                    "_videoGenerateButton");
                var status = GetField<Label>(
                    form,
                    "_videoStatus");

                Assert.IsFalse(
                    generate.Enabled,
                    "La génération vidéo doit rester désactivée si le chemin ComfyUI est invalide.");
                Assert.IsFalse(
                    string.IsNullOrWhiteSpace(status.Text),
                    "Le statut Vidéo doit expliquer l'échec de vérification.");

                var tabs = GetField<TabControl>(form, "_tabs");
                var video = GetField<TabPage>(form, "_tabVideo");
                tabs.SelectedTab = video;
                Application.DoEvents();
            }
            finally
            {
                settings.ComfyMain = originalComfyMain;
            }
        });

    [TestMethod]
    public void AllTabsAndTechnicalVisibility_CanBeToggledWithoutCrashing()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            _ = form.Handle;
            SetPrivateField(form, "_closing", true);

            InvokeVoid(
                form,
                "InitializeUiPolishV37");

            var tabs = GetField<TabControl>(form, "_tabs");
            var showOpenCode = GetField<CheckBox>(
                form,
                "_cfgShowOpenCodeTabPolish");
            var showComfy = GetField<CheckBox>(
                form,
                "_cfgShowComfyTabPolish");

            showOpenCode.Checked = true;
            showComfy.Checked = true;
            Application.DoEvents();

            var pages = tabs.TabPages
                .Cast<TabPage>()
                .ToArray();

            Assert.IsTrue(
                pages.Any(page => page.Name == "tabOpenCode"),
                "L'onglet OpenCode doit pouvoir être activé.");
            Assert.IsTrue(
                pages.Any(page => page.Name == "tabComfy"),
                "L'onglet ComfyUI doit pouvoir être activé.");

            foreach (var page in pages)
            {
                tabs.SelectedTab = page;
                Application.DoEvents();
            }

            showOpenCode.Checked = false;
            showComfy.Checked = false;
            Application.DoEvents();

            Assert.IsFalse(
                tabs.TabPages.Cast<TabPage>()
                    .Any(page => page.Name == "tabOpenCode"),
                "L'onglet OpenCode doit pouvoir être masqué.");
            Assert.IsFalse(
                tabs.TabPages.Cast<TabPage>()
                    .Any(page => page.Name == "tabComfy"),
                "L'onglet ComfyUI doit pouvoir être masqué.");
        });

    [TestMethod]
    public void TransientTechnicalTabs_OpenAndCloseWithoutCorruptingTabPages()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            _ = form.Handle;
            SetPrivateField(form, "_closing", true);

            InvokeVoid(
                form,
                "InitializeUiPolishV37");

            var settings = GetField<AppSettings>(form, "_s");
            settings.ShowOpenCodeTab = false;
            settings.ShowComfyUiTab = false;
            InvokeVoid(form, "ApplyTechnicalTabVisibilityPolish");

            var tabs = GetField<TabControl>(form, "_tabs");
            var dashboard = GetField<TabPage>(form, "tabDashboard");
            var openCode = GetField<TabPage>(form, "tabOpenCode");
            var comfy = GetField<TabPage>(form, "tabComfy");

            InvokeVoid(
                form,
                "EnsureTechnicalTabVisibleForDirectOpenPolish",
                openCode);
            Assert.IsTrue(tabs.TabPages.Contains(openCode));
            tabs.SelectedTab = openCode;
            tabs.SelectedTab = dashboard;
            Application.DoEvents();
            InvokeVoid(
                form,
                "RemoveTransientTechnicalTabsPolish");
            Assert.IsFalse(tabs.TabPages.Contains(openCode));

            InvokeVoid(
                form,
                "EnsureTechnicalTabVisibleForDirectOpenPolish",
                comfy);
            Assert.IsTrue(tabs.TabPages.Contains(comfy));
            tabs.SelectedTab = comfy;
            tabs.SelectedTab = dashboard;
            Application.DoEvents();
            InvokeVoid(
                form,
                "RemoveTransientTechnicalTabsPolish");
            Assert.IsFalse(tabs.TabPages.Contains(comfy));
        });

    [TestMethod]
    public void RuntimeModelAndLoraCombos_AllItemsCanBeSelectedDuringRefresh()
        => RunOnSta(() =>
        {
            using var form = new MainForm();

            SetPrivateField(
                form,
                "_refreshingRuntimeCatalogChoicesV37",
                true);
            SetPrivateField(
                form,
                "_refreshingLoraCatalogChoicesV37",
                true);

            foreach (var name in new[]
                     {
                         "_imageModelRuntimeCombo",
                         "_videoModelRuntimeCombo",
                         "_imageLoraCombo",
                         "_videoLoraCombo"
                     })
            {
                var combo = GetField<ComboBox>(form, name);

                for (var index = 0; index < combo.Items.Count; index++)
                {
                    combo.SelectedIndex = index;
                    Application.DoEvents();
                }
            }

            SetPrivateField(
                form,
                "_refreshingRuntimeCatalogChoicesV37",
                false);
            SetPrivateField(
                form,
                "_refreshingLoraCatalogChoicesV37",
                false);
        });

    [TestMethod]
    public void PresetCombos_AllItemsCanBeSelectedWithoutThrowing()
        => RunOnSta(() =>
        {
            using var form = new MainForm();

            var comboNames = new[]
            {
                "_imageStyleTemplateCombo",
                "_imageNegativeTemplateCombo",
                "_videoStyleTemplateCombo",
                "_videoNegativeTemplateCombo",
                "_videoQualityCombo",
                "_videoSampler",
                "_videoScheduler"
            };

            foreach (var name in comboNames)
            {
                var combo = GetField<ComboBox>(form, name);
                Assert.IsTrue(
                    combo.Items.Count > 0,
                    $"{name} ne contient aucune option.");

                for (var index = 0; index < combo.Items.Count; index++)
                {
                    combo.SelectedIndex = index;
                    Application.DoEvents();
                }
            }
        });

    [TestMethod]
    public void VideoI2v_MissingClipVision_DisablesGenerationAndExplainsDependency()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var settings = GetField<AppSettings>(form, "_s");

            var originalComfyMain = settings.ComfyMain;
            var originalVideoModel = settings.VideoModel;
            var originalTextEncoder = settings.VideoTextEncoderModel;
            var originalVae = settings.VideoVaeModel;
            var originalClipVision = settings.VideoClipVisionModel;

            var comfyRoot = Path.Combine(
                PortablePaths.RuntimeDir,
                "ui-smoke-video-i2v-" + Guid.NewGuid().ToString("N"));
            var mainPath = Path.Combine(comfyRoot, "main.py");
            var diffusionDir = Path.Combine(
                comfyRoot,
                "models",
                "diffusion_models");
            var encoderDir = Path.Combine(
                comfyRoot,
                "models",
                "text_encoders");
            var vaeDir = Path.Combine(
                comfyRoot,
                "models",
                "vae");
            Directory.CreateDirectory(diffusionDir);
            Directory.CreateDirectory(encoderDir);
            Directory.CreateDirectory(vaeDir);
            File.WriteAllText(mainPath, string.Empty);

            var videoModel = "wan-test-i2v.safetensors";
            var encoder = "umt5-test.safetensors";
            var vae = "wan-test-vae.safetensors";
            var clip = "clip_vision_h.safetensors";

            File.WriteAllBytes(
                Path.Combine(diffusionDir, videoModel),
                new byte[] { 0x01 });
            File.WriteAllBytes(
                Path.Combine(encoderDir, encoder),
                new byte[] { 0x01 });
            File.WriteAllBytes(
                Path.Combine(vaeDir, vae),
                new byte[] { 0x01 });

            try
            {
                settings.ComfyMain = mainPath;
                settings.VideoModel = videoModel;
                settings.VideoTextEncoderModel = encoder;
                settings.VideoVaeModel = vae;
                settings.VideoClipVisionModel = clip;

                InvokeVoid(form, "RefreshVideoModelStatus");

                var generate = GetField<Button>(
                    form,
                    "_videoGenerateButton");
                var status = GetField<Label>(
                    form,
                    "_videoStatus");
                var modelStatus = GetField<Label>(
                    form,
                    "_videoModelStatus");

                Assert.IsFalse(
                    generate.Enabled,
                    "La génération I2V doit être désactivée si CLIP Vision manque.");

                StringAssert.Contains(
                    status.Text,
                    "CLIP Vision",
                    "Le statut doit nommer explicitement CLIP Vision comme dépendance manquante.");

                StringAssert.Contains(
                    modelStatus.Text,
                    "[X] CLIP Vision",
                    "Le détail des modèles doit marquer CLIP Vision comme non installé.");

                using var menu = InvokeMenu(
                    form,
                    "BuildRuntimeModelAddMenuV36",
                    false);

                var clipAction = menu.Items
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(item =>
                        (item.Text ?? string.Empty).Contains(
                            "CLIP Vision",
                            StringComparison.OrdinalIgnoreCase))
                    ?? throw new AssertFailedException(
                        "Le menu Vidéo doit proposer l'installation de CLIP Vision.");

                Assert.IsNotNull(
                    GetClickHandler(
                        clipAction,
                        typeof(ToolStripItem)),
                    "L'action CLIP Vision doit avoir un handler Click.");
            }
            finally
            {
                settings.ComfyMain = originalComfyMain;
                settings.VideoModel = originalVideoModel;
                settings.VideoTextEncoderModel = originalTextEncoder;
                settings.VideoVaeModel = originalVae;
                settings.VideoClipVisionModel = originalClipVision;
                try { Directory.Delete(comfyRoot, recursive: true); }
                catch { }
            }
        });

    [TestMethod]
    public void VideoI2v_AllDependenciesInstalled_EnablesGenerationAndShowsReady()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var settings = GetField<AppSettings>(form, "_s");

            var originalComfyMain = settings.ComfyMain;
            var originalVideoModel = settings.VideoModel;
            var originalTextEncoder = settings.VideoTextEncoderModel;
            var originalVae = settings.VideoVaeModel;
            var originalClipVision = settings.VideoClipVisionModel;

            var comfyRoot = Path.Combine(
                PortablePaths.RuntimeDir,
                "ui-smoke-video-ready-" + Guid.NewGuid().ToString("N"));
            var mainPath = Path.Combine(comfyRoot, "main.py");
            var diffusionDir = Path.Combine(comfyRoot, "models", "diffusion_models");
            var encoderDir = Path.Combine(comfyRoot, "models", "text_encoders");
            var vaeDir = Path.Combine(comfyRoot, "models", "vae");
            var clipDir = Path.Combine(comfyRoot, "models", "clip_vision");
            Directory.CreateDirectory(diffusionDir);
            Directory.CreateDirectory(encoderDir);
            Directory.CreateDirectory(vaeDir);
            Directory.CreateDirectory(clipDir);
            File.WriteAllText(mainPath, string.Empty);

            var videoModel = "wan-ready-i2v.safetensors";
            var encoder = "umt5-ready.safetensors";
            var vae = "wan-ready-vae.safetensors";
            var clip = "clip-ready.safetensors";

            File.WriteAllBytes(Path.Combine(diffusionDir, videoModel), new byte[] { 0x01 });
            File.WriteAllBytes(Path.Combine(encoderDir, encoder), new byte[] { 0x01 });
            File.WriteAllBytes(Path.Combine(vaeDir, vae), new byte[] { 0x01 });
            File.WriteAllBytes(Path.Combine(clipDir, clip), new byte[] { 0x01 });

            try
            {
                settings.ComfyMain = mainPath;
                settings.VideoModel = videoModel;
                settings.VideoTextEncoderModel = encoder;
                settings.VideoVaeModel = vae;
                settings.VideoClipVisionModel = clip;

                InvokeVoid(form, "RefreshVideoModelStatus");

                var generate = GetField<Button>(form, "_videoGenerateButton");
                var status = GetField<Label>(form, "_videoStatus");

                Assert.IsTrue(
                    generate.Enabled,
                    "La génération I2V doit être active quand toutes les dépendances sont installées.");

                Assert.AreEqual(
                    "Prêt.",
                    status.Text,
                    "Le statut doit être remis à un état prêt au lieu de conserver un ancien placeholder de dépendances manquantes.");
            }
            finally
            {
                settings.ComfyMain = originalComfyMain;
                settings.VideoModel = originalVideoModel;
                settings.VideoTextEncoderModel = originalTextEncoder;
                settings.VideoVaeModel = originalVae;
                settings.VideoClipVisionModel = originalClipVision;
                try { Directory.Delete(comfyRoot, recursive: true); }
                catch { }
            }
        });

    [TestMethod]
    public void GpuUnlock_RecomputesVideoDependencyStateInsteadOfRestoringStaleEnabledSnapshot()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var settings = GetField<AppSettings>(form, "_s");

            var originalComfyMain = settings.ComfyMain;
            var originalVideoModel = settings.VideoModel;
            var originalTextEncoder = settings.VideoTextEncoderModel;
            var originalVae = settings.VideoVaeModel;
            var originalClipVision = settings.VideoClipVisionModel;

            var comfyRoot = Path.Combine(
                PortablePaths.RuntimeDir,
                "ui-smoke-gpu-unlock-" + Guid.NewGuid().ToString("N"));
            var mainPath = Path.Combine(comfyRoot, "main.py");
            var diffusionDir = Path.Combine(comfyRoot, "models", "diffusion_models");
            var encoderDir = Path.Combine(comfyRoot, "models", "text_encoders");
            var vaeDir = Path.Combine(comfyRoot, "models", "vae");
            Directory.CreateDirectory(diffusionDir);
            Directory.CreateDirectory(encoderDir);
            Directory.CreateDirectory(vaeDir);
            File.WriteAllText(mainPath, string.Empty);

            var videoModel = "wan-unlock-i2v.safetensors";
            var encoder = "umt5-unlock.safetensors";
            var vae = "wan-unlock-vae.safetensors";

            File.WriteAllBytes(
                Path.Combine(diffusionDir, videoModel),
                new byte[] { 0x01 });
            File.WriteAllBytes(
                Path.Combine(encoderDir, encoder),
                new byte[] { 0x01 });
            File.WriteAllBytes(
                Path.Combine(vaeDir, vae),
                new byte[] { 0x01 });

            try
            {
                settings.ComfyMain = mainPath;
                settings.VideoModel = videoModel;
                settings.VideoTextEncoderModel = encoder;
                settings.VideoVaeModel = vae;
                settings.VideoClipVisionModel = "missing-clip.safetensors";

                var generate = GetField<Button>(
                    form,
                    "_videoGenerateButton");
                var open = GetField<Button>(
                    form,
                    "_videoOpenButton");
                var output = GetField<TextBox>(
                    form,
                    "_videoOutput");
                var fakeVideo = Path.Combine(
                    comfyRoot,
                    "generated.mp4");
                File.WriteAllBytes(
                    fakeVideo,
                    new byte[] { 0x00, 0x00, 0x00, 0x18 });

                // Simule des états capturés avant un workflow GPU.
                generate.Enabled = true;
                open.Enabled = false;

                InvokeVoid(
                    form,
                    "SetGpuWorkflowUiLocked",
                    true,
                    "test");

                // Pendant le workflow, une nouvelle sortie vidéo devient disponible.
                output.Text = fakeVideo;
                open.Enabled = true;

                InvokeVoid(
                    form,
                    "SetGpuWorkflowUiLocked",
                    false,
                    "test");

                Assert.IsFalse(
                    generate.Enabled,
                    "Le déverrouillage GPU doit recalculer les dépendances vidéo au lieu de restaurer un ancien Enabled=true.");

                Assert.IsTrue(
                    open.Enabled,
                    "Le bouton Ouvrir la vidéo doit rester actif quand une sortie réelle a été créée pendant le workflow.");

                var status = GetField<Label>(
                    form,
                    "_videoStatus");
                StringAssert.Contains(
                    status.Text,
                    "CLIP Vision",
                    "Le statut recalculé doit conserver la dépendance CLIP Vision manquante.");
            }
            finally
            {
                settings.ComfyMain = originalComfyMain;
                settings.VideoModel = originalVideoModel;
                settings.VideoTextEncoderModel = originalTextEncoder;
                settings.VideoVaeModel = originalVae;
                settings.VideoClipVisionModel = originalClipVision;
                try { Directory.Delete(comfyRoot, recursive: true); }
                catch { }
            }
        });

    [TestMethod]
    public void ImageGenerateButton_CancelsActiveGenerationWithoutThrowing()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            _ = form.Handle;

            using var cts = new CancellationTokenSource();
            SetPrivateField(form, "_imageCts", cts);

            var button = GetField<Button>(form, "btnGenerate");
            button.Enabled = true;
            InvokeVoid(
                form,
                "btnGenerate_Click",
                button,
                EventArgs.Empty);
            Application.DoEvents();

            Assert.IsTrue(
                cts.IsCancellationRequested,
                "Le bouton Générer doit demander l'annulation quand une génération Image est active.");

            var status = GetField<Label>(form, "_genText");
            StringAssert.Contains(
                status.Text,
                "Annulation",
                "Le statut Image doit indiquer que l'annulation est en cours.");
        });

    [TestMethod]
    public void MissingImageDependencies_DisableGenerationAndNameExactDependency()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var settings = GetField<AppSettings>(form, "_s");

            var originalComfyMain = settings.ComfyMain;
            var originalComfyPython = settings.ComfyPython;
            var originalFlux = settings.FluxModel;
            var originalEncoder = settings.TextEncoderModel;
            var originalVae = settings.VaeModel;

            var comfyRoot = Path.Combine(
                PortablePaths.RuntimeDir,
                "ui-smoke-image-missing-" + Guid.NewGuid().ToString("N"));
            var mainPath = Path.Combine(comfyRoot, "main.py");
            var pythonPath = Path.Combine(comfyRoot, "python.exe");
            var diffusionDir = Path.Combine(comfyRoot, "models", "diffusion_models");
            var encoderDir = Path.Combine(comfyRoot, "models", "text_encoders");
            var vaeDir = Path.Combine(comfyRoot, "models", "vae");
            Directory.CreateDirectory(diffusionDir);
            Directory.CreateDirectory(encoderDir);
            Directory.CreateDirectory(vaeDir);
            File.WriteAllText(mainPath, string.Empty);
            File.WriteAllText(pythonPath, string.Empty);

            var flux = "flux-test.safetensors";
            var encoder = "encoder-test.safetensors";
            var vae = "vae-test.safetensors";

            void WriteAll()
            {
                File.WriteAllBytes(Path.Combine(diffusionDir, flux), new byte[] { 0x01 });
                File.WriteAllBytes(Path.Combine(encoderDir, encoder), new byte[] { 0x01 });
                File.WriteAllBytes(Path.Combine(vaeDir, vae), new byte[] { 0x01 });
            }

            try
            {
                settings.ComfyMain = mainPath;
                settings.ComfyPython = pythonPath;
                settings.FluxModel = flux;
                settings.TextEncoderModel = encoder;
                settings.VaeModel = vae;

                var cases = new[]
                {
                    (Path.Combine(diffusionDir, flux), "FLUX.2"),
                    (Path.Combine(encoderDir, encoder), "Text encoder"),
                    (Path.Combine(vaeDir, vae), "VAE FLUX.2")
                };

                foreach (var (missingPath, expectedLabel) in cases)
                {
                    WriteAll();
                    File.Delete(missingPath);

                    InvokeVoid(form, "RefreshFeatureAvailability");

                    var generate = GetField<Button>(form, "btnGenerate");
                    var status = GetField<Label>(form, "_genText");

                    Assert.IsFalse(
                        generate.Enabled,
                        $"La génération Image doit être désactivée si {expectedLabel} manque.");
                    StringAssert.Contains(
                        status.Text,
                        expectedLabel,
                        $"Le statut Image doit nommer précisément {expectedLabel}.");
                }
            }
            finally
            {
                settings.ComfyMain = originalComfyMain;
                settings.ComfyPython = originalComfyPython;
                settings.FluxModel = originalFlux;
                settings.TextEncoderModel = originalEncoder;
                settings.VaeModel = originalVae;
                try { Directory.Delete(comfyRoot, recursive: true); }
                catch { }
            }
        });

    [TestMethod]
    public void MissingVideoDependencies_DisableGenerationAndNameExactDependency()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var settings = GetField<AppSettings>(form, "_s");

            var originalComfyMain = settings.ComfyMain;
            var originalVideoModel = settings.VideoModel;
            var originalEncoder = settings.VideoTextEncoderModel;
            var originalVae = settings.VideoVaeModel;
            var originalClip = settings.VideoClipVisionModel;

            var comfyRoot = Path.Combine(
                PortablePaths.RuntimeDir,
                "ui-smoke-video-missing-matrix-" + Guid.NewGuid().ToString("N"));
            var mainPath = Path.Combine(comfyRoot, "main.py");
            var diffusionDir = Path.Combine(comfyRoot, "models", "diffusion_models");
            var encoderDir = Path.Combine(comfyRoot, "models", "text_encoders");
            var vaeDir = Path.Combine(comfyRoot, "models", "vae");
            var clipDir = Path.Combine(comfyRoot, "models", "clip_vision");
            Directory.CreateDirectory(diffusionDir);
            Directory.CreateDirectory(encoderDir);
            Directory.CreateDirectory(vaeDir);
            Directory.CreateDirectory(clipDir);
            File.WriteAllText(mainPath, string.Empty);

            var model = "wan-test-i2v.safetensors";
            var encoder = "umt5-test.safetensors";
            var vae = "wan-test-vae.safetensors";
            var clip = "clip-test.safetensors";

            void WriteAll()
            {
                File.WriteAllBytes(Path.Combine(diffusionDir, model), new byte[] { 0x01 });
                File.WriteAllBytes(Path.Combine(encoderDir, encoder), new byte[] { 0x01 });
                File.WriteAllBytes(Path.Combine(vaeDir, vae), new byte[] { 0x01 });
                File.WriteAllBytes(Path.Combine(clipDir, clip), new byte[] { 0x01 });
            }

            try
            {
                settings.ComfyMain = mainPath;
                settings.VideoModel = model;
                settings.VideoTextEncoderModel = encoder;
                settings.VideoVaeModel = vae;
                settings.VideoClipVisionModel = clip;

                var cases = new[]
                {
                    (Path.Combine(diffusionDir, model), "Wan 2.1 I2V"),
                    (Path.Combine(encoderDir, encoder), "UMT5 XXL"),
                    (Path.Combine(clipDir, clip), "CLIP Vision")
                };

                foreach (var (missingPath, expectedLabel) in cases)
                {
                    WriteAll();
                    File.Delete(missingPath);

                    InvokeVoid(form, "RefreshVideoModelStatus");

                    var generate = GetField<Button>(form, "_videoGenerateButton");
                    var status = GetField<Label>(form, "_videoStatus");

                    Assert.IsFalse(
                        generate.Enabled,
                        $"La génération Vidéo doit être désactivée si {expectedLabel} manque.");
                    StringAssert.Contains(
                        status.Text,
                        expectedLabel,
                        $"Le statut Vidéo doit nommer précisément {expectedLabel}.");
                }
            }
            finally
            {
                settings.ComfyMain = originalComfyMain;
                settings.VideoModel = originalVideoModel;
                settings.VideoTextEncoderModel = originalEncoder;
                settings.VideoVaeModel = originalVae;
                settings.VideoClipVisionModel = originalClip;
                try { Directory.Delete(comfyRoot, recursive: true); }
                catch { }
            }
        });

    [TestMethod]
    public void InvalidVideoReference_IsUserValidationNotCrash()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var settings = GetField<AppSettings>(form, "_s");

            var originalComfyMain = settings.ComfyMain;
            var originalVideoModel = settings.VideoModel;
            var originalEncoder = settings.VideoTextEncoderModel;
            var originalVae = settings.VideoVaeModel;
            var originalClip = settings.VideoClipVisionModel;

            var comfyRoot = Path.Combine(
                PortablePaths.RuntimeDir,
                "ui-smoke-video-invalid-ref-" + Guid.NewGuid().ToString("N"));
            var mainPath = Path.Combine(comfyRoot, "main.py");
            var diffusionDir = Path.Combine(comfyRoot, "models", "diffusion_models");
            var encoderDir = Path.Combine(comfyRoot, "models", "text_encoders");
            var vaeDir = Path.Combine(comfyRoot, "models", "vae");
            var clipDir = Path.Combine(comfyRoot, "models", "clip_vision");
            Directory.CreateDirectory(diffusionDir);
            Directory.CreateDirectory(encoderDir);
            Directory.CreateDirectory(vaeDir);
            Directory.CreateDirectory(clipDir);
            File.WriteAllText(mainPath, string.Empty);

            var model = "wan-invalid-ref-i2v.safetensors";
            var encoder = "umt5-invalid-ref.safetensors";
            var vae = "wan-invalid-ref-vae.safetensors";
            var clip = "clip-invalid-ref.safetensors";
            File.WriteAllBytes(Path.Combine(diffusionDir, model), new byte[] { 0x01 });
            File.WriteAllBytes(Path.Combine(encoderDir, encoder), new byte[] { 0x01 });
            File.WriteAllBytes(Path.Combine(vaeDir, vae), new byte[] { 0x01 });
            File.WriteAllBytes(Path.Combine(clipDir, clip), new byte[] { 0x01 });

            try
            {
                settings.ComfyMain = mainPath;
                settings.VideoModel = model;
                settings.VideoTextEncoderModel = encoder;
                settings.VideoVaeModel = vae;
                settings.VideoClipVisionModel = clip;

                GetField<TextBox>(form, "_videoPrompt").Text = "test";
                GetField<CheckBox>(form, "_videoAutoImprovePrompt").Checked = false;
                GetField<TextBox>(form, "_videoReferenceImage").Text =
                    Path.Combine(comfyRoot, "does-not-exist.png");
                GetField<NumericUpDown>(form, "_videoSteps").Value = 20;

                var ex = InvokeTaskExpectException(
                    form,
                    "GenerateVideoFromUiAsync");

                Assert.AreEqual(
                    "UserValidationException",
                    ex.GetType().Name,
                    "Une référence I2V invalide doit être une validation utilisateur, pas un crash.");
                StringAssert.Contains(
                    ex.Message,
                    "image de référence valide");
            }
            finally
            {
                settings.ComfyMain = originalComfyMain;
                settings.VideoModel = originalVideoModel;
                settings.VideoTextEncoderModel = originalEncoder;
                settings.VideoVaeModel = originalVae;
                settings.VideoClipVisionModel = originalClip;
                try { Directory.Delete(comfyRoot, recursive: true); }
                catch { }
            }
        });

    [TestMethod]
    public void InvalidImageSource_ReturnsClearGeneratorErrorWithoutThrowing()
        => RunOnSta(() =>
        {
            var settings = new AppSettings();
            var logs = new List<string>();
            var generator = new Flux2Generator(
                settings,
                () => Task.CompletedTask,
                () => Task.CompletedTask,
                () => Task.CompletedTask,
                (_, message) => logs.Add(message));

            var missingImage = Path.Combine(
                PortablePaths.RuntimeDir,
                "missing-source-" + Guid.NewGuid().ToString("N") + ".png");

            var result = generator.GenerateAsync(
                    "test",
                    string.Empty,
                    512,
                    512,
                    missingImage,
                    1.0,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.IsFalse(result.Ok);
            StringAssert.Contains(
                result.Error ?? string.Empty,
                "Image source introuvable");
        });

    [TestMethod]
    public void ImageRuntimeCombo_DetectsInstalledSafetensorsTextEncoder()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var settings = GetField<AppSettings>(form, "_s");
            var originalComfyMain = settings.ComfyMain;
            var originalTextEncoder = settings.TextEncoderModel;

            var comfyRoot = Path.Combine(
                PortablePaths.RuntimeDir,
                "ui-smoke-comfy-" + Guid.NewGuid().ToString("N"));
            var mainPath = Path.Combine(comfyRoot, "main.py");
            var encoderDir = Path.Combine(
                comfyRoot,
                "models",
                "text_encoders");
            var clipDir = Path.Combine(
                comfyRoot,
                "models",
                "clip");
            var diffusionDir = Path.Combine(
                comfyRoot,
                "models",
                "diffusion_models");
            Directory.CreateDirectory(encoderDir);
            Directory.CreateDirectory(clipDir);
            Directory.CreateDirectory(diffusionDir);
            File.WriteAllText(mainPath, string.Empty);

            var encoderName =
                "flux2-klein-4b-uncensored-text-encoder.safetensors";
            var clipEncoderName =
                "flux2-klein-4b-clip-location.safetensors";
            WriteFakeQwen4BTextEncoder(
                Path.Combine(encoderDir, encoderName));
            WriteFakeQwen4BTextEncoder(
                Path.Combine(clipDir, clipEncoderName));
            File.WriteAllBytes(
                Path.Combine(
                    encoderDir,
                    "flux2-klein-4b-uncensored-q8_0.gguf"),
                new byte[] { 0x47, 0x47, 0x55, 0x46 });

            try
            {
                settings.ComfyMain = mainPath;
                settings.TextEncoderModel = encoderName;

                Assert.AreEqual(
                    Path.Combine(clipDir, clipEncoderName),
                    PortablePreflight.GetComfyTextEncoderPath(
                        settings,
                        clipEncoderName),
                    "DreamRaster doit résoudre un encodeur placé dans models/clip.");

                InvokeVoid(form, "RefreshRuntimeModelChoicesV36");
                InvokeVoid(form, "RefreshRuntimeTextEncoderChoicesV39");

                var modelCombo = GetField<ComboBox>(
                    form,
                    "_imageModelRuntimeCombo");
                var encoderCombo = GetField<ComboBox>(
                    form,
                    "_imageTextEncoderRuntimeCombo");
                var encoderItems = encoderCombo.Items
                    .Cast<object>()
                    .ToArray();

                Assert.AreEqual(
                    encoderName,
                    encoderCombo.SelectedItem
                        ?.GetType()
                        .GetProperty("FileName")
                        ?.GetValue(encoderCombo.SelectedItem)
                        ?.ToString(),
                    "L'encodeur configuré doit être sélectionné dans la ComboBox Encodeur Image.");

                Assert.IsFalse(
                    modelCombo.Items
                        .Cast<object>()
                        .Any(item =>
                            item.GetType()
                                .GetProperty("ActivatesTextEncoder")
                                ?.GetValue(item) is true),
                    "La ComboBox Modèle Image ne doit plus mélanger modèles de diffusion et encodeurs texte.");

                Assert.IsTrue(
                    encoderItems.Any(item =>
                        string.Equals(
                            item.GetType()
                                .GetProperty("FileName")
                                ?.GetValue(item)
                                ?.ToString(),
                            encoderName,
                            StringComparison.OrdinalIgnoreCase)),
                    "L'encodeur Safetensors installé doit apparaître dans la ComboBox Encodeur Image.");

                Assert.IsTrue(
                    encoderItems.Any(item =>
                        string.Equals(
                            item.GetType()
                                .GetProperty("FileName")
                                ?.GetValue(item)
                                ?.ToString(),
                            clipEncoderName,
                            StringComparison.OrdinalIgnoreCase)),
                    "Un encodeur Safetensors placé dans models/clip doit aussi être détecté.");

                var gguf = encoderItems.FirstOrDefault(item =>
                    item.GetType()
                        .GetProperty("FileName")
                        ?.GetValue(item)
                        ?.ToString()
                        ?.EndsWith(
                            ".gguf",
                            StringComparison.OrdinalIgnoreCase) == true);

                Assert.IsNotNull(
                    gguf,
                    "Un encodeur GGUF installé doit être détecté dans la ComboBox Encodeur Image.");

                StringAssert.Contains(
                    gguf.ToString() ?? string.Empty,
                    "GGUF non supporté",
                    "Le GGUF détecté doit être clairement marqué comme non supporté par le CLIPLoader actuel.");
            }
            finally
            {
                settings.ComfyMain = originalComfyMain;
                settings.TextEncoderModel = originalTextEncoder;
                try { Directory.Delete(comfyRoot, recursive: true); }
                catch { }
            }
        });

    [TestMethod]
    public void PipelinePreset_ImageAnime_AppliesInstalledComponents()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var settings = GetField<AppSettings>(form, "_s");

            var original = (
                settings.ComfyMain,
                settings.FluxModel,
                settings.TextEncoderModel,
                settings.VaeModel,
                settings.ImageLora,
                settings.ImageLoraStrength,
                settings.ImageStyleTemplate,
                settings.ImageNegativeTemplate,
                settings.ImagePipelinePreset);

            var comfyRoot = Path.Combine(
                PortablePaths.RuntimeDir,
                "pipeline-preset-comfy-" + Guid.NewGuid().ToString("N"));
            var mainPath = Path.Combine(comfyRoot, "main.py");
            var diffusion = Path.Combine(comfyRoot, "models", "diffusion_models");
            var encoders = Path.Combine(comfyRoot, "models", "text_encoders");
            var vae = Path.Combine(comfyRoot, "models", "vae");
            var loras = Path.Combine(comfyRoot, "models", "loras");

            Directory.CreateDirectory(diffusion);
            Directory.CreateDirectory(encoders);
            Directory.CreateDirectory(vae);
            Directory.CreateDirectory(loras);
            File.WriteAllText(mainPath, string.Empty);

            File.WriteAllText(
                Path.Combine(diffusion, "flux-2-klein-4b-fp8.safetensors"),
                "test");
            WriteFakeQwen4BTextEncoder(
                Path.Combine(encoders, "qwen_3_4b.safetensors"));
            File.WriteAllText(
                Path.Combine(vae, "flux2-vae.safetensors"),
                "test");
            File.WriteAllText(
                Path.Combine(loras, "Flux_klein_4b_anime_Koni.safetensors"),
                "test");

            try
            {
                settings.ComfyMain = mainPath;
                InvokeVoid(form, "RefreshRuntimeModelChoicesV36");
                InvokeVoid(form, "RefreshLoraChoicesV37");

                var profileCombo = GetField<ComboBox>(
                    form,
                    "_imagePipelinePresetCombo");
                var anime = profileCombo.Items
                    .Cast<object>()
                    .First(item =>
                        string.Equals(
                            item.GetType()
                                .GetProperty("Id")
                                ?.GetValue(item)
                                ?.ToString(),
                            "anime",
                            StringComparison.OrdinalIgnoreCase));

                profileCombo.SelectedItem = anime;
                InvokeVoid(
                    form,
                    "ApplySelectedPipelinePreset",
                    false);

                Assert.AreEqual(
                    "flux-2-klein-4b-fp8.safetensors",
                    settings.FluxModel);
                Assert.AreEqual(
                    "qwen_3_4b.safetensors",
                    settings.TextEncoderModel);
                Assert.AreEqual(
                    "flux2-vae.safetensors",
                    settings.VaeModel);
                Assert.AreEqual(
                    "Flux_klein_4b_anime_Koni.safetensors",
                    settings.ImageLora);
                Assert.AreEqual(
                    0.85,
                    settings.ImageLoraStrength,
                    0.001);
                Assert.AreEqual(
                    "anime",
                    settings.ImageStyleTemplate);
                Assert.AreEqual(
                    "style",
                    settings.ImageNegativeTemplate);
                Assert.AreEqual(
                    "anime",
                    settings.ImagePipelinePreset);

                var status = GetField<Label>(
                    form,
                    "_genText");
                StringAssert.Contains(
                    status.Text,
                    "Anime premium");
            }
            finally
            {
                settings.ComfyMain = original.ComfyMain;
                settings.FluxModel = original.FluxModel;
                settings.TextEncoderModel = original.TextEncoderModel;
                settings.VaeModel = original.VaeModel;
                settings.ImageLora = original.ImageLora;
                settings.ImageLoraStrength = original.ImageLoraStrength;
                settings.ImageStyleTemplate = original.ImageStyleTemplate;
                settings.ImageNegativeTemplate = original.ImageNegativeTemplate;
                settings.ImagePipelinePreset = original.ImagePipelinePreset;

                try { Directory.Delete(comfyRoot, recursive: true); }
                catch { }
            }
        });

    [TestMethod]
    public void PipelinePreset_VideoI2v_AppliesInstalledDependencies()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var settings = GetField<AppSettings>(form, "_s");

            var original = (
                settings.ComfyMain,
                settings.VideoModel,
                settings.VideoTextEncoderModel,
                settings.VideoVaeModel,
                settings.VideoClipVisionModel,
                settings.VideoLora,
                settings.VideoStyleTemplate,
                settings.VideoNegativeTemplate,
                settings.VideoQualityPreset,
                settings.VideoPipelinePreset,
                settings.VideoSteps);

            var comfyRoot = Path.Combine(
                PortablePaths.RuntimeDir,
                "pipeline-video-comfy-" + Guid.NewGuid().ToString("N"));
            var mainPath = Path.Combine(comfyRoot, "main.py");
            var diffusion = Path.Combine(comfyRoot, "models", "diffusion_models");
            var encoders = Path.Combine(comfyRoot, "models", "text_encoders");
            var vae = Path.Combine(comfyRoot, "models", "vae");
            var clipVision = Path.Combine(comfyRoot, "models", "clip_vision");
            var loras = Path.Combine(comfyRoot, "models", "loras");

            Directory.CreateDirectory(diffusion);
            Directory.CreateDirectory(encoders);
            Directory.CreateDirectory(vae);
            Directory.CreateDirectory(clipVision);
            Directory.CreateDirectory(loras);
            File.WriteAllText(mainPath, string.Empty);

            File.WriteAllText(
                Path.Combine(
                    diffusion,
                    "wan2.1_i2v_480p_14B_fp8_e4m3fn.safetensors"),
                "test");
            File.WriteAllText(
                Path.Combine(
                    encoders,
                    "umt5_xxl_fp8_e4m3fn_scaled.safetensors"),
                "test");
            File.WriteAllText(
                Path.Combine(vae, "wan_2.1_vae.safetensors"),
                "test");
            File.WriteAllText(
                Path.Combine(clipVision, "clip_vision_h.safetensors"),
                "test");

            try
            {
                settings.ComfyMain = mainPath;

                var profileCombo = GetField<ComboBox>(
                    form,
                    "_videoPipelinePresetCombo");
                var i2v = profileCombo.Items
                    .Cast<object>()
                    .First(item =>
                        string.Equals(
                            item.GetType()
                                .GetProperty("Id")
                                ?.GetValue(item)
                                ?.ToString(),
                            "i2v-quality",
                            StringComparison.OrdinalIgnoreCase));

                profileCombo.SelectedItem = i2v;
                InvokeVoid(
                    form,
                    "ApplySelectedPipelinePreset",
                    true);

                Assert.AreEqual(
                    "wan2.1_i2v_480p_14B_fp8_e4m3fn.safetensors",
                    settings.VideoModel);
                Assert.AreEqual(
                    "umt5_xxl_fp8_e4m3fn_scaled.safetensors",
                    settings.VideoTextEncoderModel);
                Assert.AreEqual(
                    "wan_2.1_vae.safetensors",
                    settings.VideoVaeModel);
                Assert.AreEqual(
                    "clip_vision_h.safetensors",
                    settings.VideoClipVisionModel);
                Assert.AreEqual(
                    string.Empty,
                    settings.VideoLora);
                Assert.AreEqual(
                    "cinematic",
                    settings.VideoStyleTemplate);
                Assert.AreEqual(
                    "identity",
                    settings.VideoNegativeTemplate);
                Assert.AreEqual(
                    "best",
                    settings.VideoQualityPreset);
                Assert.AreEqual(
                    "i2v-quality",
                    settings.VideoPipelinePreset);
                Assert.AreEqual(
                    50,
                    settings.VideoSteps);

                var status = GetField<Label>(
                    form,
                    "_videoStatus");
                StringAssert.Contains(
                    status.Text,
                    "Image vers");
            }
            finally
            {
                settings.ComfyMain = original.ComfyMain;
                settings.VideoModel = original.VideoModel;
                settings.VideoTextEncoderModel = original.VideoTextEncoderModel;
                settings.VideoVaeModel = original.VideoVaeModel;
                settings.VideoClipVisionModel = original.VideoClipVisionModel;
                settings.VideoLora = original.VideoLora;
                settings.VideoStyleTemplate = original.VideoStyleTemplate;
                settings.VideoNegativeTemplate = original.VideoNegativeTemplate;
                settings.VideoQualityPreset = original.VideoQualityPreset;
                settings.VideoPipelinePreset = original.VideoPipelinePreset;
                settings.VideoSteps = original.VideoSteps;

                try { Directory.Delete(comfyRoot, recursive: true); }
                catch { }
            }
        });

    [TestMethod]
    public void MainForm_ConstructorKeepsSettingsPersistenceSuppressedUntilShown()
        => RunOnSta(() =>
        {
            using var form = new MainForm();

            var field = typeof(MainForm).GetField(
                "_suppressSettingsPersistence",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(
                    typeof(MainForm).FullName,
                    "_suppressSettingsPersistence");

            Assert.IsTrue(
                (bool)(field.GetValue(form) ?? false),
                "Le constructeur doit bloquer toute écriture de settings.json jusqu'à la fin de MainForm_Shown.");
        });

    [TestMethod]
    public void PipelinePreset_ListsExposeCompleteCategories()
        => RunOnSta(() =>
        {
            using var form = new MainForm();

            static HashSet<string> Ids(ComboBox combo) =>
                combo.Items
                    .Cast<object>()
                    .Select(item =>
                        item.GetType()
                            .GetProperty("Id")
                            ?.GetValue(item)
                            ?.ToString())
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Cast<string>()
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var imageIds = Ids(
                GetField<ComboBox>(
                    form,
                    "_imagePipelinePresetCombo"));
            var videoIds = Ids(
                GetField<ComboBox>(
                    form,
                    "_videoPipelinePresetCombo"));

            foreach (var expected in new[]
                     {
                         "photo",
                         "realism",
                         "portrait",
                         "product",
                         "landscape",
                         "fantasy",
                         "anime",
                         "scifi",
                         "lowlight",
                         "max-quality",
                         "erotic",
                         "adult-explicit"
                     })
            {
                Assert.IsTrue(
                    imageIds.Contains(expected),
                    $"Le profil Image '{expected}' doit rester visible.");
            }

            foreach (var expected in new[]
                     {
                         "cinematic",
                         "i2v-quality",
                         "portrait",
                         "product",
                         "landscape",
                         "photorealistic",
                         "anime",
                         "scifi",
                         "lowlight",
                         "max-quality",
                         "erotic",
                         "adult-explicit"
                     })
            {
                Assert.IsTrue(
                    videoIds.Contains(expected),
                    $"Le profil Vidéo '{expected}' doit rester visible.");
            }
        });

    [TestMethod]
    public void MissingPipelineDependenciesPreserveConfiguredFileName()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var selections = new[]
            {
                "_imageVaeRuntimeCombo",
                "_videoTextEncoderRuntimeCombo",
                "_videoVaeRuntimeCombo",
                "_videoClipVisionRuntimeCombo"
            };
            SetPrivateField(form, "_refreshingPipelineDependencyChoicesV39", true);
            foreach (var fieldName in selections)
            {
                var combo = GetField<ComboBox>(form, fieldName);
                var configured = "absent-" + Guid.NewGuid().ToString("N") + ".safetensors";
                var method = typeof(MainForm).GetMethod(
                    "FillRuntimeDependencyComboV39",
                    BindingFlags.NonPublic | BindingFlags.Static)!;
                method.Invoke(null, new object[] { combo, Array.Empty<string>(), configured });
                Assert.AreEqual(configured, combo.SelectedItem as string,
                    "Le composant absent doit rester sélectionné : " + fieldName);
                Assert.AreEqual(configured, combo.Text);
                Assert.AreEqual(DrawMode.OwnerDrawFixed, combo.DrawMode);
                var installed = combo.Tag as HashSet<string>;
                Assert.IsNotNull(installed);
                Assert.IsFalse(installed.Contains(configured),
                    "Le fichier fictif doit être signalé comme absent.");
                method.Invoke(null, new object[]
                {
                    combo,
                    new[] { configured },
                    configured
                });
                Assert.IsTrue(((HashSet<string>)combo.Tag!).Contains(configured),
                    "Le même fichier doit être reconnu après installation.");
                Assert.AreEqual(configured, combo.Text);
            }
        });

    [TestMethod]
    public void MissingLoraRemainsVisibleInImageAndVideoSelectors()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var settings = GetField<AppSettings>(form, "_s");
            var originalImage = settings.ImageLora;
            var originalVideo = settings.VideoLora;
            var missingName = "lora-absent-" + Guid.NewGuid().ToString("N") + ".safetensors";
            try
            {
                settings.ImageLora = missingName;
                settings.VideoLora = missingName;
                InvokeVoid(form, "RefreshLoraChoicesV37");
                foreach (var fieldName in new[] { "_imageLoraCombo", "_videoLoraCombo" })
                {
                    var combo = GetField<ComboBox>(form, fieldName);
                    var choice = combo.SelectedItem
                        ?? throw new AssertFailedException("LoRA sélectionné absent.");
                    var fileName = choice.GetType().GetProperty("FileName")?.GetValue(choice) as string;
                    Assert.AreEqual(missingName, fileName);
                    StringAssert.Contains(combo.Text, missingName);
                }
                Assert.AreEqual(missingName, settings.ImageLora);
                Assert.AreEqual(missingName, settings.VideoLora);
            }
            finally
            {
                settings.ImageLora = originalImage;
                settings.VideoLora = originalVideo;
            }
        });

    [TestMethod]
    public void PipelinePreset_LanguageRefreshPreservesSelections()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var settings = GetField<AppSettings>(form, "_s");
            SetPrivateField(form, "_suppressSettingsPersistence", true);
            var originalLanguage = settings.Language;
            var originalImage = settings.ImagePipelinePreset;
            var originalVideo = settings.VideoPipelinePreset;
            try
            {
                settings.ImagePipelinePreset = "portrait";
                settings.VideoPipelinePreset = "cinematic";
                string? frenchImage = null;
                string? frenchVideo = null;
                foreach (var language in new[] { "fr", "en" })
                {
                    settings.Language = language;
                    InvokeVoid(form, "RefreshPipelinePresetTranslations");
                    var image = GetField<ComboBox>(form, "_imagePipelinePresetCombo");
                    var video = GetField<ComboBox>(form, "_videoPipelinePresetCombo");
                    Assert.AreEqual("portrait", SelectedTemplateIdForTest(image));
                    Assert.AreEqual("cinematic", SelectedTemplateIdForTest(video));
                    Assert.AreEqual("portrait", settings.ImagePipelinePreset);
                    Assert.AreEqual("cinematic", settings.VideoPipelinePreset);
                    if (language == "fr")
                    {
                        frenchImage = image.Text;
                        frenchVideo = video.Text;
                    }
                    else
                    {
                        Assert.AreNotEqual(frenchImage, image.Text,
                            "Le nom du profil Image doit être traduit.");
                        Assert.AreNotEqual(frenchVideo, video.Text,
                            "Le nom du profil Vidéo doit être traduit.");
                    }
                }
            }
            finally
            {
                settings.Language = originalLanguage;
                settings.ImagePipelinePreset = originalImage;
                settings.VideoPipelinePreset = originalVideo;
            }
        });

    [TestMethod]
    public void PipelinePreset_SynchronizationDetectsExactAndCustomStates()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            var settings = GetField<AppSettings>(form, "_s");

            var original = (
                settings.FluxModel,
                settings.TextEncoderModel,
                settings.VaeModel,
                settings.ImageLora,
                settings.ImageLoraStrength,
                settings.ImageStyleTemplate,
                settings.ImageNegativeTemplate,
                settings.ImagePipelinePreset,
                settings.DefaultWidth,
                settings.DefaultHeight,
                settings.DefaultSteps,
                settings.ImageCfg,
                settings.VideoModel,
                settings.VideoTextEncoderModel,
                settings.VideoVaeModel,
                settings.VideoClipVisionModel,
                settings.VideoLora,
                settings.VideoLoraStrength,
                settings.VideoStyleTemplate,
                settings.VideoNegativeTemplate,
                settings.VideoQualityPreset,
                settings.VideoPipelinePreset,
                settings.VideoWidth,
                settings.VideoHeight,
                settings.VideoFrames,
                settings.VideoFps,
                settings.VideoSteps,
                settings.VideoCfg,
                settings.VideoSamplingShift,
                settings.VideoSampler,
                settings.VideoScheduler);

            try
            {
                settings.FluxModel =
                    "flux-2-klein-4b-fp8.safetensors";
                settings.TextEncoderModel =
                    "qwen_3_4b.safetensors";
                settings.VaeModel =
                    "flux2-vae.safetensors";
                settings.ImageLora = string.Empty;
                settings.ImageLoraStrength = 1.0;
                settings.ImageStyleTemplate = "photo4k";
                settings.ImageNegativeTemplate = "safe_quality";
                settings.DefaultWidth = 1536;
                settings.DefaultHeight = 1024;
                settings.DefaultSteps = 6;
                settings.ImageCfg = 1.0;

                InvokeVoid(
                    form,
                    "SynchronizePipelinePresetSelection",
                    false,
                    false);

                Assert.AreEqual(
                    "photo",
                    settings.ImagePipelinePreset);
                Assert.AreEqual(
                    "photo",
                    SelectedTemplateIdForTest(
                        GetField<ComboBox>(
                            form,
                            "_imagePipelinePresetCombo")));

                settings.DefaultWidth = 1024;

                InvokeVoid(
                    form,
                    "SynchronizePipelinePresetSelection",
                    false,
                    false);

                Assert.AreEqual(
                    "custom",
                    settings.ImagePipelinePreset);
                Assert.AreEqual(
                    "custom",
                    SelectedTemplateIdForTest(
                        GetField<ComboBox>(
                            form,
                            "_imagePipelinePresetCombo")));

                // Une modification manuelle de chaque composant doit
                // invalider le profil Image sans modifier les autres valeurs.
                settings.DefaultWidth = 1536;
                foreach (var change in new Action[]
                {
                    () => settings.FluxModel = "autre-modele.safetensors",
                    () => settings.TextEncoderModel = "autre-encodeur.safetensors",
                    () => settings.VaeModel = "autre-vae.safetensors",
                    () => settings.ImageLora = "autre-lora.safetensors"
                })
                {
                    var model = settings.FluxModel;
                    var encoder = settings.TextEncoderModel;
                    var vae = settings.VaeModel;
                    var lora = settings.ImageLora;
                    change();
                    InvokeVoid(form, "SynchronizePipelinePresetSelection", false, false);
                    Assert.AreEqual("custom", settings.ImagePipelinePreset);
                    Assert.AreEqual("custom", SelectedTemplateIdForTest(
                        GetField<ComboBox>(form, "_imagePipelinePresetCombo")));
                    settings.FluxModel = model;
                    settings.TextEncoderModel = encoder;
                    settings.VaeModel = vae;
                    settings.ImageLora = lora;
                }

                settings.VideoModel =
                    "wan2.1_i2v_480p_14B_fp8_e4m3fn.safetensors";
                settings.VideoTextEncoderModel =
                    "umt5_xxl_fp8_e4m3fn_scaled.safetensors";
                settings.VideoVaeModel =
                    "wan_2.1_vae.safetensors";
                settings.VideoClipVisionModel =
                    "clip_vision_h.safetensors";
                settings.VideoLora = string.Empty;
                settings.VideoLoraStrength = 1.0;
                settings.VideoStyleTemplate = "cinematic";
                settings.VideoNegativeTemplate = "identity";
                settings.VideoQualityPreset = "best";
                settings.VideoWidth = 832;
                settings.VideoHeight = 480;
                settings.VideoFrames = 49;
                settings.VideoFps = 16;
                settings.VideoSteps = 50;
                settings.VideoCfg = 6.0;
                settings.VideoSamplingShift = 8.0;
                settings.VideoSampler = "uni_pc";
                settings.VideoScheduler = "simple";

                InvokeVoid(
                    form,
                    "SynchronizePipelinePresetSelection",
                    true,
                    false);

                Assert.AreEqual(
                    "i2v-quality",
                    settings.VideoPipelinePreset);
                Assert.AreEqual(
                    "i2v-quality",
                    SelectedTemplateIdForTest(
                        GetField<ComboBox>(
                            form,
                            "_videoPipelinePresetCombo")));

                settings.VideoFrames = 33;

                InvokeVoid(
                    form,
                    "SynchronizePipelinePresetSelection",
                    true,
                    false);

                Assert.AreEqual(
                    "custom",
                    settings.VideoPipelinePreset);
                Assert.AreEqual(
                    "custom",
                    SelectedTemplateIdForTest(
                        GetField<ComboBox>(
                            form,
                            "_videoPipelinePresetCombo")));

                settings.VideoFrames = 49;
                foreach (var change in new Action[]
                {
                    () => settings.VideoModel = "autre-wan.safetensors",
                    () => settings.VideoTextEncoderModel = "autre-umt5.safetensors",
                    () => settings.VideoVaeModel = "autre-wan-vae.safetensors",
                    () => settings.VideoClipVisionModel = "autre-clip.safetensors",
                    () => settings.VideoLora = "autre-lora.safetensors"
                })
                {
                    var model = settings.VideoModel;
                    var encoder = settings.VideoTextEncoderModel;
                    var vae = settings.VideoVaeModel;
                    var clip = settings.VideoClipVisionModel;
                    var lora = settings.VideoLora;
                    change();
                    InvokeVoid(form, "SynchronizePipelinePresetSelection", true, false);
                    Assert.AreEqual("custom", settings.VideoPipelinePreset);
                    Assert.AreEqual("custom", SelectedTemplateIdForTest(
                        GetField<ComboBox>(form, "_videoPipelinePresetCombo")));
                    settings.VideoModel = model;
                    settings.VideoTextEncoderModel = encoder;
                    settings.VideoVaeModel = vae;
                    settings.VideoClipVisionModel = clip;
                    settings.VideoLora = lora;
                }
            }
            finally
            {
                settings.FluxModel = original.FluxModel;
                settings.TextEncoderModel = original.TextEncoderModel;
                settings.VaeModel = original.VaeModel;
                settings.ImageLora = original.ImageLora;
                settings.ImageLoraStrength = original.ImageLoraStrength;
                settings.ImageStyleTemplate = original.ImageStyleTemplate;
                settings.ImageNegativeTemplate = original.ImageNegativeTemplate;
                settings.ImagePipelinePreset = original.ImagePipelinePreset;
                settings.DefaultWidth = original.DefaultWidth;
                settings.DefaultHeight = original.DefaultHeight;
                settings.DefaultSteps = original.DefaultSteps;
                settings.ImageCfg = original.ImageCfg;
                settings.VideoModel = original.VideoModel;
                settings.VideoTextEncoderModel = original.VideoTextEncoderModel;
                settings.VideoVaeModel = original.VideoVaeModel;
                settings.VideoClipVisionModel =
                    original.VideoClipVisionModel;
                settings.VideoLora = original.VideoLora;
                settings.VideoLoraStrength = original.VideoLoraStrength;
                settings.VideoStyleTemplate =
                    original.VideoStyleTemplate;
                settings.VideoNegativeTemplate =
                    original.VideoNegativeTemplate;
                settings.VideoQualityPreset =
                    original.VideoQualityPreset;
                settings.VideoPipelinePreset =
                    original.VideoPipelinePreset;
                settings.VideoWidth = original.VideoWidth;
                settings.VideoHeight = original.VideoHeight;
                settings.VideoFrames = original.VideoFrames;
                settings.VideoFps = original.VideoFps;
                settings.VideoSteps = original.VideoSteps;
                settings.VideoCfg = original.VideoCfg;
                settings.VideoSamplingShift =
                    original.VideoSamplingShift;
                settings.VideoSampler = original.VideoSampler;
                settings.VideoScheduler = original.VideoScheduler;
            }
        });

    [TestMethod]
    public void RuntimeModelAndLoraMenus_CloseWithoutDisposalCrash()
        => RunOnSta(() =>
        {
            using var form = new MainForm();
            _ = form.Handle;

            var cases = new (string Method, bool Argument)[]
            {
                ("BuildRuntimeModelAddMenuV36", true),
                ("BuildRuntimeModelAddMenuV36", false),
                ("BuildLoraAddMenuV37", false),
                ("BuildLoraAddMenuV37", true)
            };

            var onClosed = typeof(ToolStripDropDown).GetMethod(
                "OnClosed",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(
                    typeof(ToolStripDropDown).FullName,
                    "OnClosed");

            foreach (var item in cases)
            {
                var menu = InvokeMenu(form, item.Method, item.Argument);

                InvokeVoid(
                    form,
                    "WireDeferredContextMenuDisposalV37",
                    menu);

                onClosed.Invoke(
                    menu,
                    new object[]
                    {
                        new ToolStripDropDownClosedEventArgs(
                            ToolStripDropDownCloseReason.CloseCalled)
                    });

                Assert.IsFalse(
                    menu.IsDisposed,
                    $"{item.Method} ne doit pas être détruit synchroniquement dans Closed.");

                Application.DoEvents();

                Assert.IsTrue(
                    menu.IsDisposed,
                    $"{item.Method} doit être libéré après la fin du message WinForms.");
            }
        });

    private static string? SelectedTemplateIdForTest(
        ComboBox combo) =>
        combo.SelectedItem
            ?.GetType()
            .GetProperty("Id")
            ?.GetValue(combo.SelectedItem)
            ?.ToString();

    private static void AssertMenuTree(
        ContextMenuStrip menu,
        string label)
    {
        Assert.IsTrue(
            menu.Items.Count >= 2,
            $"{label} : menu vide ou incomplet.");

        var problems = new List<string>();
        ValidateItems(menu.Items, label, problems);

        Assert.AreEqual(
            0,
            problems.Count,
            string.Join(Environment.NewLine, problems));
    }

    private static void ValidateItems(
        ToolStripItemCollection items,
        string path,
        List<string> problems)
    {
        foreach (ToolStripItem item in items)
        {
            if (item is ToolStripSeparator)
                continue;

            var itemPath = path + " > " + item.Text;

            if (string.IsNullOrWhiteSpace(item.Text))
                problems.Add(itemPath + " : texte vide.");

            if (item is not ToolStripMenuItem menuItem)
                continue;

            if (menuItem.DropDownItems.Count > 0)
            {
                ValidateItems(
                    menuItem.DropDownItems,
                    itemPath,
                    problems);
                continue;
            }

            if (menuItem.Enabled &&
                GetClickHandler(menuItem, typeof(ToolStripItem)) is null)
            {
                problems.Add(itemPath + " : action active sans handler Click.");
            }
        }
    }

    private static Delegate? GetClickHandler(
        Component component,
        Type eventOwner)
    {
        var eventsProperty = typeof(Component).GetProperty(
            "Events",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(
                typeof(Component).FullName,
                "Events");

        var events = eventsProperty.GetValue(component) as EventHandlerList
            ?? throw new InvalidOperationException(
                "Impossible de lire EventHandlerList.");

        for (var type = eventOwner;
             type is not null;
             type = type.BaseType)
        {
            foreach (var field in type.GetFields(
                         BindingFlags.Static |
                         BindingFlags.NonPublic))
            {
                if (field.FieldType != typeof(object) ||
                    !field.Name.Contains(
                        "click",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var key = field.GetValue(null);
                if (key is not null &&
                    events[key] is Delegate handler)
                {
                    return handler;
                }
            }
        }

        return null;
    }

    private static void SetPrivateField(
        MainForm form,
        string fieldName,
        object value)
    {
        var field = typeof(MainForm).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(
                typeof(MainForm).FullName,
                fieldName);

        field.SetValue(form, value);
    }

    private static void InvokeVoid(
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

    private static Exception InvokeTaskExpectException(
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

        var task = method.Invoke(form, arguments) as Task
            ?? throw new InvalidOperationException(
                $"{methodName} n'a pas renvoyé de Task.");

        try
        {
            task.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new AssertFailedException(
            $"{methodName} devait échouer pour ce scénario de validation.");
    }

    private static ContextMenuStrip InvokeMenu(
        MainForm form,
        string methodName,
        bool argument)
    {
        var method = typeof(MainForm).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(
                typeof(MainForm).FullName,
                methodName);

        return method.Invoke(form, new object[] { argument })
            as ContextMenuStrip
            ?? throw new InvalidOperationException(
                $"{methodName} n'a pas renvoyé de ContextMenuStrip.");
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

    private static IEnumerable<T> EnumerateControls<T>(
        Control parent)
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

    private static void WriteFakeQwen4BTextEncoder(string path)
    {
        const string header =
            "{\"model.layers.0.post_attention_layernorm.weight\":{" +
            "\"dtype\":\"F32\",\"shape\":[2560],\"data_offsets\":[0,0]}," +
            "\"model.layers.0.self_attn.q_norm.weight\":{" +
            "\"dtype\":\"F32\",\"shape\":[128],\"data_offsets\":[0,0]}}";

        var bytes = System.Text.Encoding.UTF8.GetBytes(header);
        using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);
        using var writer = new BinaryWriter(stream);
        writer.Write((ulong)bytes.Length);
        writer.Write(bytes);
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
            Name = "DreamRaster UI interaction smoke"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!thread.Join(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException(
                "Le test d'interaction UI n'a pas terminé en 30 secondes.");
        }

        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
