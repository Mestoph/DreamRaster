/*
Copyright (C) 2026 Mestoph
SPDX-License-Identifier: AGPL-3.0-or-later

FR : Modèles de style réutilisables pour les générations image et vidéo.
EN: Reusable style templates for image and video generation.
*/

namespace OpenCodeLocalAI;

internal sealed record GenerationStyleTemplate(
    string Id,
    string FrenchName,
    string EnglishName,
    string FrenchDescription,
    string EnglishDescription,
    string PromptSuffix,
    string NegativePrompt,
    string RecommendedSettings);

internal sealed record GenerationNegativeTemplate(
    string Id,
    string FrenchName,
    string EnglishName,
    string NegativePrompt,
    bool IncludeStyleNegative = false);

internal sealed record GenerationImageObjectiveSettings(
    string Id,
    int Width,
    int Height,
    int Steps,
    double Cfg);

internal sealed record GenerationVideoObjectiveSettings(
    string Id,
    int Width,
    int Height,
    int Frames,
    int Fps,
    int Steps,
    double Cfg,
    double SamplingShift,
    string Sampler,
    string Scheduler,
    string QualityPreset);

internal static class GenerationTemplates
{
    private const string GeneralQualityNegative =
        "low quality, blurry, out of focus, jpeg artifacts, compression artifacts, " +
        "watermark, signature, logo, accidental text, oversharpening, oversaturated, " +
        "underexposed, overexposed, malformed, distorted";

    private const string PeopleAnatomyNegative =
        "extra fingers, missing fingers, fused fingers, extra limbs, duplicated body parts, " +
        "malformed hands, malformed face, asymmetrical eyes, deformed anatomy, extra teeth";

    private const string ExplicitContentNegative =
        "pornography, explicit sexual content, explicit sex, exposed genitals, genitalia, " +
        "sexualized nudity, fetish content, erotic explicit content, sexualized minor, " +
        "underage sexual content, child sexual content";

    private const string GraphicViolenceNegative =
        "graphic gore, dismemberment, exposed organs, graphic injury, excessive blood, " +
        "torture imagery";

    private const string VideoStabilityNegative =
        "flicker, jitter, camera shake, frame tearing, temporal inconsistency, warped motion, " +
        "duplicated objects, sudden cuts, frame skipping, unstable geometry";

    public static IReadOnlyDictionary<string, GenerationImageObjectiveSettings> ImageObjectiveSettings { get; } =
        new Dictionary<string, GenerationImageObjectiveSettings>(StringComparer.OrdinalIgnoreCase)
        {
            ["photorealistic"] = new("photorealistic", 1024, 1024, 6, 1.0),
            ["portrait"] = new("portrait", 832, 1216, 6, 1.0),
            ["portrait-premium"] = new("portrait-premium", 896, 1344, 8, 1.0),
            ["product"] = new("product", 1024, 1024, 6, 1.0),
            ["landscape"] = new("landscape", 1344, 768, 6, 1.0),
            ["anime"] = new("anime", 1024, 1024, 6, 1.0),
            ["cinematic"] = new("cinematic", 1344, 768, 6, 1.0),
            ["lowlight"] = new("lowlight", 1344, 768, 8, 1.0),
            ["photo4k"] = new("photo4k", 1536, 1024, 6, 1.0),
            ["max-quality"] = new("max-quality", 1024, 1024, 8, 1.0)
        };

    public static IReadOnlyDictionary<string, GenerationVideoObjectiveSettings> VideoObjectiveSettings { get; } =
        new Dictionary<string, GenerationVideoObjectiveSettings>(StringComparer.OrdinalIgnoreCase)
        {
            ["photorealistic"] = new("photorealistic", 832, 480, 33, 16, 40, 6.0, 8.0, "uni_pc", "simple", "quality"),
            ["portrait"] = new("portrait", 480, 832, 33, 16, 40, 6.0, 8.0, "uni_pc", "simple", "quality"),
            ["product"] = new("product", 832, 480, 33, 16, 40, 6.0, 8.0, "uni_pc", "simple", "quality"),
            ["landscape"] = new("landscape", 832, 480, 49, 16, 40, 6.0, 8.0, "uni_pc", "simple", "quality"),
            ["anime"] = new("anime", 832, 480, 33, 16, 40, 6.0, 8.0, "uni_pc", "simple", "quality"),
            ["cinematic"] = new("cinematic", 832, 480, 49, 16, 40, 6.0, 8.0, "uni_pc", "simple", "quality"),
            ["lowlight"] = new("lowlight", 832, 480, 33, 16, 40, 6.0, 8.0, "uni_pc", "simple", "quality"),
            ["4k"] = new("4k", 1280, 720, 33, 16, 50, 6.0, 8.0, "uni_pc", "simple", "best"),
            ["max-quality"] = new("max-quality", 832, 480, 33, 16, 50, 6.0, 8.0, "uni_pc", "simple", "best")
        };

    public static IReadOnlyList<GenerationStyleTemplate> ImageStyles { get; } =
    [
        S("none","Aucun / personnalisé","None / custom",
            "Aucun enrichissement automatique.",
            "No automatic prompt enrichment.",
            "","",""),

        S("photo4k","Photo 4K / studio","Photo 4K / studio",
            "Photo très détaillée, propre et professionnelle.",
            "Highly detailed, clean professional photography.",
            "high-end professional photography, crisp realistic detail, physically plausible lighting, controlled highlights, accurate materials, high dynamic range, natural color science, professional camera rendering",
            "plastic skin, waxy texture, fake reflections, excessive sharpening, chromatic aberration, distorted perspective, CGI look",
            "1024×1024 · 4+ steps · seed fixe pour comparer"),

        S("photorealistic","Photorealistic","Photorealistic",
            "Photo naturelle et crédible.",
            "Natural, believable photography.",
            "photorealistic photography, physically plausible lighting, realistic materials, natural textures, accurate perspective, subtle sensor detail, balanced dynamic range",
            "plastic skin, fake texture, painterly look, CGI look, oversharpening, distorted perspective",
            "1024×1024 · 4+ steps"),

        S("portrait","Portrait naturel","Natural portrait",
            "Portrait naturel avec peau et regard crédibles.",
            "Natural portrait with believable skin and eyes.",
            "professional portrait photography, natural skin texture, expressive eyes, flattering soft light, realistic facial detail, subtle depth of field, accurate hair strands",
            "waxy skin, asymmetrical eyes, malformed ears, extra teeth, deformed hands, oversmoothing, beauty-filter look, text, watermark",
            "Orientation verticale recommandée · 4+ steps"),

        S("portrait-premium","Portrait premium / magazine","Premium editorial portrait",
            "Portrait éditorial haut de gamme avec lumière et peau très soignées.",
            "High-end editorial portrait with refined skin and lighting.",
            "premium editorial portrait, magazine photography, natural skin pores, realistic eyes, detailed hair, soft key light, subtle rim light, controlled depth of field, elegant color grading, high micro-contrast without oversharpening",
            "waxy skin, plastic texture, fake pores, asymmetrical eyes, malformed hands, extra fingers, excessive beauty retouching, harsh HDR, text, watermark",
            "Vertical · résolution native élevée · seed fixe conseillé"),

        S("cinematic","Cinématique","Cinematic",
            "Image type photogramme de film.",
            "Film-still style image.",
            "cinematic composition, premium film still, dramatic natural lighting, atmospheric depth, realistic color grading, subtle film grain, controlled contrast, detailed scene, intentional lens language",
            "flat lighting, amateur framing, oversaturated colors, crushed blacks, blown highlights, watermark, text",
            "Paysage ou 16:9 · 4+ steps"),

        S("lowlight","Nuit / basse lumière","Low-light photography",
            "Scène nocturne propre avec ombres détaillées et lumières contrôlées.",
            "Clean night scene with detailed shadows and controlled highlights.",
            "professional low-light photography, realistic night exposure, controlled practical lights, detailed shadows, clean blacks, natural reflections, subtle atmospheric haze, realistic high-ISO texture",
            "crushed blacks, clipped highlights, neon bloom overload, color banding, excessive noise, muddy shadows, fake reflections",
            "Résolution native élevée · éviter la surexposition"),

        S("macro","Macro / gros plan","Macro / close-up",
            "Détails fins et mise au point précise sur un sujet proche.",
            "Fine detail and precise focus on a close subject.",
            "professional macro photography, precise focal plane, fine surface detail, realistic micro-textures, soft controlled lighting, shallow depth of field, natural bokeh",
            "mushy detail, fake texture, excessive sharpening, haloing, incorrect scale, duplicated structures",
            "Carré ou paysage · seed fixe conseillé"),

        S("food","Cuisine / gastronomie","Food photography",
            "Photo culinaire appétissante et réaliste.",
            "Appetizing realistic food photography.",
            "premium food photography, realistic ingredients, appetizing texture, natural steam where appropriate, controlled highlights, soft directional light, clean plating, commercial editorial finish",
            "plastic food, fake garnish, melted geometry, oversaturation, dirty plate, random utensils, text, watermark",
            "1024×1024 · lumière douce recommandée"),

        S("product","Photo produit","Product photography",
            "Packshot publicitaire propre.",
            "Clean commercial product shot.",
            "premium product photography, clean studio background, controlled softbox lighting, sharp subject, accurate materials, realistic reflections, commercial advertising quality",
            "distorted product geometry, fake labels, random text, dirty background, harsh reflections, watermark",
            "1024×1024 · seed fixe pour séries produit"),

        S("ecommerce","E-commerce fond propre","Clean e-commerce",
            "Packshot lisible, centré et cohérent pour catalogue.",
            "Clean centered catalog-ready product image.",
            "catalog product photography, centered isolated subject, clean neutral background, even studio illumination, accurate proportions, realistic materials, minimal shadows, consistent commercial framing",
            "busy background, cropped product, distorted proportions, fake labels, random text, dramatic lighting, watermark",
            "Carré · cadrage centré · seed fixe"),

        S("architecture","Architecture","Architecture",
            "Visualisation architecturale réaliste.",
            "Realistic architectural visualization.",
            "architectural photography, straight verticals, accurate perspective, realistic materials, balanced natural light, clean spatial composition, believable scale, detailed surfaces",
            "warped walls, impossible geometry, fisheye distortion, crooked verticals, random text, watermark",
            "Paysage recommandé"),

        S("interior","Intérieur / design","Interior design",
            "Intérieur réaliste avec matériaux et volumes cohérents.",
            "Realistic interior with coherent materials and spatial geometry.",
            "premium interior photography, straight geometry, realistic furniture scale, accurate materials, balanced window light, soft indirect illumination, clean composition, natural perspective",
            "warped furniture, impossible room geometry, duplicated objects, floating decor, fisheye distortion, blown windows",
            "Paysage · lumière naturelle recommandée"),

        S("landscape","Paysage","Landscape",
            "Grand paysage naturel détaillé.",
            "Detailed natural landscape.",
            "epic natural landscape, realistic atmosphere, layered depth, detailed terrain, natural color balance, volumetric light, believable weather, fine distant detail",
            "oversaturated sky, repeating terrain, artificial textures, random structures, text, watermark",
            "Paysage recommandé"),

        S("travel","Voyage / documentaire","Travel documentary",
            "Photo de voyage naturelle et crédible.",
            "Natural believable travel photography.",
            "documentary travel photography, authentic environment, natural light, realistic people and architecture, observational composition, balanced color, believable atmosphere",
            "tourist-poster look, excessive HDR, fake signage, duplicated people, warped architecture, watermark",
            "Paysage ou vertical selon sujet"),

        S("anime","Anime premium","Premium anime",
            "Illustration anime propre avec lignes et couleurs cohérentes.",
            "Clean coherent anime illustration.",
            "high-quality anime key visual, precise clean line art, expressive shapes, polished cel shading, coherent character design, detailed background, controlled color palette",
            "photorealistic skin, muddy line art, inconsistent eyes, malformed hands, extra fingers, broken anatomy, text, watermark",
            "1024×1024 · 4+ steps"),

        S("illustration","Illustration","Illustration",
            "Illustration éditoriale soignée.",
            "Polished editorial illustration.",
            "polished illustration, deliberate composition, clean shapes, rich controlled detail, coherent palette, professional editorial finish",
            "photographic noise, accidental text, watermark, muddy edges, incoherent perspective",
            "1024×1024 · 4+ steps"),

        S("digitalart","Art numérique","Digital art",
            "Peinture numérique détaillée.",
            "Detailed digital painting.",
            "high-end digital art, refined brushwork, layered lighting, rich materials, detailed environment, polished finish",
            "jpeg artifacts, muddy colors, broken anatomy, accidental text, watermark",
            "1024×1024 · 4+ steps"),

        S("concept","Concept art","Concept art",
            "Concept de production lisible et exploitable.",
            "Production-ready readable concept art.",
            "professional concept art, strong silhouette, production design, material definition, painterly detail, cinematic atmosphere, readable value structure",
            "weak silhouette, random details, muddy values, broken perspective, text, watermark",
            "Paysage recommandé"),

        S("fantasy","Fantasy","Fantasy",
            "Univers fantasy riche et atmosphérique.",
            "Rich atmospheric fantasy world.",
            "epic fantasy concept, magical atmosphere, ornate materials, cinematic light, volumetric depth, intricate environment detail, coherent worldbuilding",
            "modern accidental objects, flat lighting, low detail, malformed anatomy, text, watermark",
            "Paysage recommandé"),

        S("scifi","Science-fiction","Sci-fi",
            "Design SF crédible et détaillé.",
            "Detailed believable science-fiction design.",
            "cinematic science-fiction design, advanced technology, believable materials, atmospheric lighting, precise industrial detail, coherent worldbuilding",
            "toy-like materials, random text, broken geometry, duplicated machinery, watermark",
            "Paysage recommandé"),

        S("pixelart","Pixel art","Pixel art",
            "Pixel art net à résolution logique cohérente.",
            "Crisp pixel art with coherent logical resolution.",
            "crisp pixel art, limited palette, deliberate dithering, readable silhouette, consistent pixel scale, no anti-aliasing",
            "blur, anti-aliasing, smooth vector edges, mixed pixel scales, photographic texture, text, watermark",
            "Carré recommandé"),

        S("sprite","Sprite","Sprite",
            "Sprite de jeu isolé et lisible.",
            "Readable isolated game sprite.",
            "2D game sprite, consistent proportions, clean silhouette, orthographic presentation, centered subject, crisp game-ready edges",
            "cropped sprite, perspective distortion, soft edges, blurry pixels, inconsistent scale, overlapping frames, text, watermark",
            "Carré recommandé"),
        S("max-quality","Qualité maximale","Maximum quality",
            "Deux passes : génération premium puis upscale/amélioration x2.",
            "Two passes: premium generation followed by x2 upscale/enhancement.",
            "maximum quality professional render, exceptional fine detail, physically coherent lighting, accurate materials, clean micro-textures, balanced dynamic range, refined composition, artifact-free premium finish, upscale-ready detail",
            "low detail, soft focus, plastic texture, oversharpening, haloing, compression artifacts, broken geometry, malformed anatomy, accidental text, watermark",
            "Passe 1 sûre : 1024×1024 · 8 steps · CFG 1.0 · Passe 2 : 2048×2048 Lanczos + sharpen")
    ];

    public static IReadOnlyList<GenerationNegativeTemplate> ImageNegatives { get; } =
    [
        N("style","Adapté au style","Style-aware","",true),
        N("safe_quality","Sécurité + qualité (recommandé)","Safety + quality (recommended)",
            GeneralQualityNegative + ", " + PeopleAnatomyNegative + ", " +
            ExplicitContentNegative + ", " + GraphicViolenceNegative,true),
        N("safe","SFW strict / contenu explicite interdit","Strict SFW / explicit content blocked",
            ExplicitContentNegative,true),
        N("family","Familial / tout public","Family-friendly",
            ExplicitContentNegative + ", " + GraphicViolenceNegative +
            ", graphic violence, disturbing imagery",true),
        N("default","Qualité générale","General quality",GeneralQualityNegative),
        N("anatomy","Personnes / anatomie","People / anatomy",PeopleAnatomyNegative),
        N("faces","Visages / peau","Faces / skin",
            "asymmetrical eyes, crossed eyes, deformed face, duplicate face, extra teeth, " +
            "waxy skin, plastic skin, fake pores, excessive skin smoothing, uncanny face"),
        N("hands","Mains / doigts","Hands / fingers",
            "extra fingers, missing fingers, fused fingers, malformed hands, duplicated hands, " +
            "broken wrists, impossible finger joints"),
        N("no-text","Sans texte / logo","No text / logo",
            "letters, words, captions, subtitles, watermark, signature, logo, UI elements, fake labels"),
        N("clean-bg","Fond propre","Clean background",
            "cluttered background, random objects, dirty background, distracting elements, accidental text, watermark"),
        N("no-gore","Violence graphique interdite","Graphic violence blocked",GraphicViolenceNegative,true),
        N("none","Aucun preset","No preset","")
    ];

    public static IReadOnlyList<GenerationStyleTemplate> VideoStyles { get; } =
    [
        V("none","Aucun / personnalisé","None / custom",
            "Aucun enrichissement automatique.",
            "No automatic prompt enrichment.",
            "","",""),

        V("cinematic","Cinématique premium","Premium cinematic",
            "Mouvement cinématique fluide et cohérent.",
            "Smooth coherent cinematic motion.",
            "premium cinematic shot, controlled camera movement, coherent subject motion, realistic parallax, natural lighting changes, stable perspective, realistic temporal consistency, filmic color grading",
            "flicker, jitter, frame tearing, temporal inconsistency, sudden cuts, warped motion, watermark, subtitles",
            "832×480 · 33 frames · 30 steps · qualité Best"),

        V("portrait","Portrait vidéo","Portrait video",
            "Portrait animé stable avec visage et peau cohérents.",
            "Stable animated portrait with coherent face and skin.",
            "cinematic portrait video, stable face identity, natural blinking, subtle breathing, realistic hair motion, soft flattering light, controlled camera, consistent skin texture",
            "flickering face, identity drift, melting features, asymmetrical eyes, warped hands, sudden expression changes, camera shake",
            "832×480 ou vertical équivalent · 33 frames · Best"),

        V("tracking","Travelling / tracking","Tracking shot",
            "Caméra qui suit le sujet.",
            "Camera follows the subject.",
            "smooth tracking shot following the subject, stable framing, consistent distance, natural parallax, continuous camera motion, coherent background motion",
            "camera jumps, subject teleportation, unstable framing, flicker, duplicated subject, warped motion",
            "33+ frames · vitesse caméra modérée"),

        V("static","Caméra fixe","Static camera",
            "Cadre verrouillé, mouvement naturel dans la scène.",
            "Locked frame with natural motion inside the scene.",
            "locked-off static camera, stable tripod framing, no camera drift, natural subject motion, consistent composition, realistic temporal detail",
            "camera drift, zoom, pan, tilt, shake, sudden reframing, flicker",
            "33+ frames · idéal pour cohérence maximale"),

        V("drone","Drone","Drone",
            "Prise de vue aérienne stable.",
            "Stable aerial camera move.",
            "smooth drone shot, slow forward glide, stable horizon, realistic parallax, coherent depth, natural atmospheric motion, gradual camera movement",
            "camera shake, unstable horizon, sudden altitude jumps, warped terrain, flicker, text, watermark",
            "832×480 · paysage · 33+ frames"),

        V("slowmotion","Ralenti premium","Premium slow motion",
            "Mouvement lent, continu et détaillé.",
            "Slow continuous detailed motion.",
            "cinematic slow motion, high temporal consistency, graceful subject movement, stable camera, realistic motion blur, detailed intermediate motion",
            "jerky motion, frame skipping, duplicated frames, sudden speed changes, flicker, strobing",
            "33+ frames · mouvement simple recommandé"),

        V("product","Produit / publicité","Product commercial",
            "Mise en valeur produit stable et commerciale.",
            "Stable commercial product showcase.",
            "premium product video, slow controlled orbit around the product, accurate materials, realistic reflections, controlled studio lighting, stable center framing, commercial advertising finish",
            "distorted product geometry, random labels, fast camera motion, harsh flicker, warped reflections, watermark, subtitles",
            "832×480 · 33 frames · seed fixe conseillé"),

        V("nature","Nature documentaire","Nature documentary",
            "Mouvement naturel doux et crédible.",
            "Gentle believable natural motion.",
            "premium nature footage, slow camera motion, wind moving foliage naturally, realistic water movement, stable horizon, natural light, coherent atmospheric motion",
            "unnatural plant motion, looping artifacts, unstable horizon, flicker, sudden cuts, watermark",
            "832×480 · 33+ frames"),

        V("atmospheric","Atmosphérique","Atmospheric",
            "Mouvement subtil de brume, particules et lumière.",
            "Subtle mist, particle and lighting motion.",
            "atmospheric cinematic shot, slow controlled pan, drifting mist, subtle particles, gentle light changes, stable geometry, layered depth",
            "heavy shake, random cuts, geometry warping, flicker, abrupt lighting changes, text, watermark",
            "33+ frames · mouvement lent recommandé"),

        V("anime","Anime premium","Premium anime",
            "Animation stylisée cohérente.",
            "Coherent stylized animation.",
            "polished anime sequence, clean outlines, coherent cel shading, consistent character design, fluid intentional motion, stable camera, controlled color palette",
            "line boil, flicker, inconsistent proportions, melting features, color shifts, subtitles, watermark",
            "33+ frames · mouvement simple"),

        V("scifi","Science-fiction","Sci-fi",
            "Mouvement SF cinématique et mécanique cohérent.",
            "Cinematic sci-fi motion with coherent mechanics.",
            "cinematic science-fiction shot, slow orbit, precise mechanical motion, volumetric atmosphere, coherent futuristic lighting, stable geometry, realistic parallax",
            "melting machinery, broken geometry, random text, flicker, sudden cuts, duplicated objects, watermark",
            "832×480 · 33+ frames"),

        V("photorealistic","Photoréaliste","Photorealistic",
            "Vidéo naturelle avec matières, lumière et mouvement crédibles.",
            "Natural video with believable materials, lighting and motion.",
            "photorealistic video, physically plausible lighting, realistic materials, natural motion, stable geometry, coherent depth, consistent exposure, realistic temporal detail",
            "CGI look, plastic materials, flicker, warped motion, unstable geometry, exposure pumping, watermark, subtitles",
            "832×480 · 33 frames · 16 fps · 40 steps · Qualité"),

        V("landscape","Paysage","Landscape",
            "Plan large naturel, stable et détaillé.",
            "Detailed stable natural wide shot.",
            "cinematic natural landscape video, stable horizon, layered atmospheric depth, realistic wind and water motion, detailed terrain, coherent lighting, gentle camera movement",
            "unstable horizon, repeating terrain, warped trees, looping water, flicker, sudden cuts, watermark",
            "832×480 · 49 frames · 16 fps · 40 steps · Qualité"),

        V("lowlight","Basse lumière / nuit","Low-light / night",
            "Vidéo nocturne propre avec flicker et bruit réduits.",
            "Clean night video with reduced flicker and noise.",
            "professional low-light cinematic video, controlled practical lights, detailed shadows, clean blacks, realistic reflections, stable exposure, subtle atmospheric haze, temporally consistent night lighting",
            "flicker, exposure pumping, crushed blacks, clipped highlights, neon bloom overload, noisy shadows, warped reflections",
            "832×480 · 33 frames · 16 fps · 40 steps · Qualité"),

        V("4k","4K / prêt pour upscale","4K / upscale-ready",
            "Profil 720p haute qualité destiné à un upscale final vers 4K.",
            "High-quality 720p profile intended for final 4K upscaling.",
            "ultra-detailed premium video, crisp realistic texture, controlled cinematic lighting, stable geometry, high temporal consistency, clean edges, fine detail preserved for 4K upscaling",
            "soft detail, shimmer, flicker, edge crawling, compression artifacts, warped geometry, text, watermark",
            "1280×720 · 33 frames · 16 fps · 50 steps · Best"),

        V("max-quality","Qualité maximale","Maximum quality",
            "Deux passes : génération vidéo premium puis upscale/amélioration x2.",
            "Two passes: premium video generation followed by x2 upscale/enhancement.",
            "maximum quality cinematic video, exceptional temporal consistency, stable identity and geometry, refined lighting, realistic materials, detailed motion, clean edges, premium color grading, upscale-ready fine detail",
            "flicker, jitter, frame tearing, identity drift, warped geometry, duplicated objects, soft detail, compression artifacts, subtitles, watermark",
            "Passe 1 sûre : 832×480 · 33 frames · 16 fps · 50 steps · Passe 2 : 1664×960 Lanczos + sharpen · H.264 CRF 16")
    ];

    public static IReadOnlyList<GenerationNegativeTemplate> VideoNegatives { get; } =
    [
        N("style","Adapté au style","Style-aware","",true),
        N("safe_stable","Sécurité + stabilité (recommandé)","Safety + stability (recommended)",
            VideoStabilityNegative + ", " + GeneralQualityNegative + ", " +
            ExplicitContentNegative + ", " + GraphicViolenceNegative,true),
        N("safe","SFW strict / contenu explicite interdit","Strict SFW / explicit content blocked",
            ExplicitContentNegative,true),
        N("family","Familial / tout public","Family-friendly",
            ExplicitContentNegative + ", " + GraphicViolenceNegative +
            ", disturbing imagery",true),
        N("stable","Stabilité vidéo","Video stability",VideoStabilityNegative,true),
        N("people","Personnes / mouvement","People / motion",
            PeopleAnatomyNegative + ", warped anatomy, duplicated face, inconsistent clothing, " +
            "melting features, unnatural gait, flickering face",true),
        N("identity","Visage / identité stable","Stable face / identity",
            "face flicker, identity drift, facial morphing, melting features, changing eye color, " +
            "changing hairstyle, duplicated face, asymmetrical eyes",true),
        N("animation","Animation propre","Clean animation",
            "flicker, line boil, inconsistent proportions, broken outlines, color shifts, " +
            "duplicate frames, abrupt pose changes, subtitles, watermark",true),
        N("no-text","Sans texte / sous-titres","No text / subtitles",
            "letters, words, captions, subtitles, watermark, signature, logo, UI elements, fake labels"),
        N("no-gore","Violence graphique interdite","Graphic violence blocked",GraphicViolenceNegative,true),
        N("none","Aucun preset","No preset","")
    ];

    private static GenerationStyleTemplate S(
        string id,
        string fr,
        string en,
        string frDesc,
        string enDesc,
        string suffix,
        string negative,
        string settings) =>
        new(id, fr, en, frDesc, enDesc, suffix, negative, settings);

    private static GenerationStyleTemplate V(
        string id,
        string fr,
        string en,
        string frDesc,
        string enDesc,
        string suffix,
        string negative,
        string settings) =>
        new(id, fr, en, frDesc, enDesc, suffix, negative, settings);

    private static GenerationNegativeTemplate N(
        string id,
        string fr,
        string en,
        string negative,
        bool includeStyleNegative = false) =>
        new(id, fr, en, negative, includeStyleNegative);

    public static string Display(GenerationStyleTemplate item, string language) =>
        language.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? item.EnglishName
            : item.FrenchName;

    public static string Description(GenerationStyleTemplate item, string language) =>
        language.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? item.EnglishDescription
            : item.FrenchDescription;

    public static string Display(GenerationNegativeTemplate item, string language) =>
        language.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? item.EnglishName
            : item.FrenchName;

    public static string ApplyPrompt(
        string prompt,
        GenerationStyleTemplate? template)
    {
        if (template is null ||
            string.IsNullOrWhiteSpace(template.PromptSuffix))
        {
            return prompt.Trim();
        }

        return prompt.TrimEnd().TrimEnd('.') + ". " + template.PromptSuffix;
    }

    public static string MergeNegative(
        string custom,
        GenerationNegativeTemplate? preset,
        GenerationStyleTemplate? style)
    {
        var parts = new List<string>(3);

        custom = custom.Trim();
        if (!string.IsNullOrWhiteSpace(custom))
            parts.Add(custom);

        if (preset?.IncludeStyleNegative == true &&
            !string.IsNullOrWhiteSpace(style?.NegativePrompt))
        {
            parts.Add(style.NegativePrompt.Trim());
        }

        if (!string.IsNullOrWhiteSpace(preset?.NegativePrompt))
            parts.Add(preset!.NegativePrompt.Trim());

        return string.Join(", ", parts);
    }

    public static GenerationStyleTemplate? FindStyle(
        IEnumerable<GenerationStyleTemplate> source,
        string? id) =>
        source.FirstOrDefault(x =>
            x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public static GenerationNegativeTemplate? FindNegative(
        IEnumerable<GenerationNegativeTemplate> source,
        string? id) =>
        source.FirstOrDefault(x =>
            x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public static GenerationImageObjectiveSettings? FindImageSettings(string? id) =>
        !string.IsNullOrWhiteSpace(id) &&
        ImageObjectiveSettings.TryGetValue(id, out var settings)
            ? settings
            : null;

    public static GenerationVideoObjectiveSettings? FindVideoSettings(string? id) =>
        !string.IsNullOrWhiteSpace(id) &&
        VideoObjectiveSettings.TryGetValue(id, out var settings)
            ? settings
            : null;

    public static bool UsesTwoPassMaximumQuality(string? id) =>
        string.Equals(
            id,
            "max-quality",
            StringComparison.OrdinalIgnoreCase);
}
