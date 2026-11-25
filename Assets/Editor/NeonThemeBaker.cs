using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NeonThemeBaker
{
    private const string MaterialFolder = "Assets/Resources/Neon/Materials";
    private const string GameplayScenePath = "Assets/Scenes/Endless.unity";
    private const string HomeScenePath = "Assets/Scenes/Home.unity";
    private const string VolumeProfilePath = "Assets/Settings/NeonVolumeProfile.asset";
    private static readonly Color DefaultNeonColor = new Color(0.05f, 0.9f, 1f, 1f);

    private sealed class ThemeAssets
    {
        public Material Rim;
        public Material Pipe;
        public Material Ball;
        public Material Trail;
        public VolumeProfile VolumeProfile;
    }

    [MenuItem("Tools/MGD2/Bake Editable Neon Theme")]
    public static void BakeEditableTheme()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play mode before baking the editable neon theme.");
            return;
        }

        string originalScenePath = SceneManager.GetActiveScene().path;
        try
        {
            ThemeAssets assets = CreateOrUpdateAssets();
            ApplyToPrefabs(assets);
            ApplyToScene(GameplayScenePath, assets, false);
            ApplyToScene(HomeScenePath, assets, true);

            SaveOwnedAssets(assets);
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(originalScenePath))
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);

            Debug.Log(
                "Editable neon theme baked successfully. Materials are in " +
                "Assets/Resources/Neon/Materials and all scene/prefab assignments are serialized.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (!string.IsNullOrEmpty(originalScenePath))
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }
    }

    private static ThemeAssets CreateOrUpdateAssets()
    {
        EnsureFolder("Assets/Resources/Neon", "Materials");

        Shader rimShader = AssetDatabase.LoadAssetAtPath<Shader>(
            "Assets/Resources/Neon/Shaders/NeonRim.shader");
        Shader ballShader = AssetDatabase.LoadAssetAtPath<Shader>(
            "Assets/Resources/Neon/Shaders/NeonBall.shader");
        Shader additiveShader = AssetDatabase.LoadAssetAtPath<Shader>(
            "Assets/Resources/Neon/Shaders/NeonAdditive.shader");

        if (rimShader == null || ballShader == null || additiveShader == null)
            throw new InvalidOperationException("One or more MGD2 neon shaders could not be loaded.");

        Material rim = GetOrCreateMaterial(MaterialFolder + "/NeonRim.mat", rimShader);
        rim.SetColor("_BaseColor", new Color(0.004f, 0.008f, 0.02f, 1f));
        rim.SetColor("_NeonColor", DefaultNeonColor);
        rim.SetFloat("_EmissionStrength", 2.5f);
        rim.SetFloat("_RimPower", 1.8f);
        rim.SetFloat("_RimThreshold", 0.12f);
        rim.SetFloat("_FullEmission", 0f);

        Material pipe = GetOrCreateMaterial(MaterialFolder + "/NeonPipe.mat", rimShader);
        pipe.SetColor("_BaseColor", new Color(0.006f, 0.012f, 0.027f, 1f));
        pipe.SetColor("_NeonColor", DefaultNeonColor);
        pipe.SetFloat("_EmissionStrength", 0.38f);
        pipe.SetFloat("_RimPower", 2.7f);
        pipe.SetFloat("_RimThreshold", 0.28f);
        pipe.SetFloat("_FullEmission", 0.08f);

        Material ball = GetOrCreateMaterial(MaterialFolder + "/WhiteRollerBall.mat", ballShader);
        ball.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Resources/Neon/Ball/MetalPlates_Color.jpg"));
        ball.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Resources/Neon/Ball/MetalPlates_NormalDX.jpg"));
        ball.SetTexture("_MetalnessMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Resources/Neon/Ball/MetalPlates_Metalness.jpg"));
        ball.SetTexture("_RoughnessMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Resources/Neon/Ball/MetalPlates_Roughness.jpg"));
        ball.SetColor("_NeonColor", Color.white);
        ball.SetFloat("_EmissionStrength", 1.25f);
        ball.SetFloat("_Tiling", 2f);

        Material trail = GetOrCreateMaterial(MaterialFolder + "/WhiteTrail.mat", additiveShader);
        trail.SetColor("_Color", DefaultNeonColor);
        trail.SetFloat("_Intensity", 1.35f);
        trail.SetTexture("_MainTex", GetOrCreateSoftParticleTexture());

        EditorUtility.SetDirty(rim);
        EditorUtility.SetDirty(pipe);
        EditorUtility.SetDirty(ball);
        EditorUtility.SetDirty(trail);

        return new ThemeAssets
        {
            Rim = rim,
            Pipe = pipe,
            Ball = ball,
            Trail = trail,
            VolumeProfile = GetOrCreateVolumeProfile()
        };
    }

    private static Material GetOrCreateMaterial(string path, Shader shader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        return material;
    }

    private static Texture2D GetOrCreateSoftParticleTexture()
    {
        string path = MaterialFolder + "/SoftParticleTexture.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture != null)
            return texture;

        const int size = 32;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = "Soft Particle Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Pow(1f - Mathf.Clamp01(distance), 1.8f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }

    private static VolumeProfile GetOrCreateVolumeProfile()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }

        if (!profile.TryGet(out Bloom bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.threshold.Override(0.55f);
        bloom.intensity.Override(0.72f);
        bloom.scatter.Override(0.68f);
        bloom.highQualityFiltering.Override(true);

        if (!profile.TryGet(out Tonemapping tonemapping))
            tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        if (!profile.TryGet(out ColorAdjustments colorAdjustments))
            colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.active = true;
        colorAdjustments.contrast.Override(15f);
        colorAdjustments.saturation.Override(8f);

        if (!profile.TryGet(out Vignette vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.22f);
        vignette.smoothness.Override(0.35f);

        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void ApplyToPrefabs(ThemeAssets assets)
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        foreach (string prefabGuid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            try
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Material target = GetTargetMaterial(renderer, assets);
                    if (target != null)
                        changed |= AssignAllSlots(renderer, target);
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void ApplyToScene(string scenePath, ThemeAssets assets, bool isHomeScene)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();

        if (isHomeScene)
            AddEditableHomeCubeComponents(roots);

        foreach (GameObject root in roots)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || renderer is TrailRenderer)
                    continue;

                Material target = GetTargetMaterial(renderer, assets);
                if (target != null)
                    AssignAllSlots(renderer, target);
            }
        }

        if (!isHomeScene)
            CreateOrUpdateBallTrail(roots, assets.Trail);

        ConfigureSceneCamerasAndLights(scene, roots);
        ConfigureVolume(scene, roots, assets.VolumeProfile);
        ConfigureEditableInterface(roots);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Material GetTargetMaterial(Renderer renderer, ThemeAssets assets)
    {
        if (renderer.GetComponentInParent<PlayerController>(true) != null)
            return assets.Rim;
        if (renderer.GetComponentInParent<ObstacleBehaviour>(true) != null)
            return assets.Rim;
        if (renderer.GetComponentInParent<PipeSegment>(true) != null ||
            renderer.name.Equals("Bg1", StringComparison.OrdinalIgnoreCase))
            return assets.Pipe;
        if (renderer.GetComponentInParent<HomeCubeFloat>(true) != null)
            return assets.Rim;
        return null;
    }

    private static bool AssignAllSlots(Renderer renderer, Material material)
    {
        Material[] slots = renderer.sharedMaterials;
        if (slots.Length == 0)
            slots = new Material[1];

        bool changed = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == material)
                continue;
            slots[i] = material;
            changed = true;
        }

        if (changed)
        {
            renderer.sharedMaterials = slots;
            EditorUtility.SetDirty(renderer);
        }

        return changed;
    }

    private static void AddEditableHomeCubeComponents(GameObject[] roots)
    {
        Transform background = FindTransform(roots, "Background");
        if (background == null)
            return;

        foreach (Transform child in background)
        {
            if (child.GetComponentInChildren<Renderer>(true) == null)
                continue;
            if (child.GetComponent<HomeCubeFloat>() == null)
                child.gameObject.AddComponent<HomeCubeFloat>();
        }
    }

    private static void CreateOrUpdateBallTrail(GameObject[] roots, Material trailMaterial)
    {
        PlayerController player = FindComponent<PlayerController>(roots);
        if (player == null)
            return;

        Renderer ballRenderer = player.GetComponentInChildren<Renderer>(true);
        if (ballRenderer == null)
            return;

        Transform trailTransform = player.transform.Find("WhiteNeonTrail");
        if (trailTransform == null)
        {
            GameObject trailObject = new GameObject("WhiteNeonTrail");
            trailTransform = trailObject.transform;
            trailTransform.SetParent(player.transform, false);
        }

        Vector3 radialDirection = ballRenderer.bounds.center - player.transform.position;
        if (radialDirection.sqrMagnitude < 0.0001f)
            radialDirection = -player.transform.up;
        radialDirection.Normalize();

        float ballRadius = Mathf.Min(
            ballRenderer.bounds.extents.x,
            Mathf.Min(ballRenderer.bounds.extents.y, ballRenderer.bounds.extents.z));
        Vector3 bottomOfBall = ballRenderer.bounds.center + radialDirection * ballRadius * 0.9f;
        trailTransform.localPosition = player.transform.InverseTransformPoint(bottomOfBall);

        TrailRenderer trail = GetOrAdd<TrailRenderer>(trailTransform.gameObject);
        trail.time = 0.55f;
        trail.minVertexDistance = 0.025f;
        trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.22f),
            new Keyframe(0.45f, 0.12f),
            new Keyframe(1f, 0f));
        trail.numCornerVertices = 4;
        trail.numCapVertices = 4;
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.startColor = Color.white;
        trail.endColor = new Color(1f, 1f, 1f, 0f);
        trail.sharedMaterial = trailMaterial;

        ParticleSystem particles = GetOrAdd<ParticleSystem>(trailTransform.gameObject);
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.72f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.08f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 180;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 38f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.z = -4.2f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        ParticleSystem.TrailModule particleTrails = particles.trails;
        particleTrails.enabled = true;
        particleTrails.mode = ParticleSystemTrailMode.PerParticle;
        particleTrails.ratio = 1f;
        particleTrails.lifetime = 0.7f;
        particleTrails.minVertexDistance = 0.035f;
        particleTrails.worldSpace = true;
        particleTrails.dieWithParticles = false;
        particleTrails.widthOverTrail = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));
        Gradient trailFade = new Gradient();
        trailFade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
        particleTrails.colorOverTrail = trailFade;

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sharedMaterial = trailMaterial;
        particleRenderer.trailMaterial = trailMaterial;

        Light ballLight = GetOrAdd<Light>(trailTransform.gameObject);
        ballLight.type = LightType.Point;
        ballLight.color = DefaultNeonColor;
        ballLight.intensity = 1.8f;
        ballLight.range = 5f;
        ballLight.shadows = LightShadows.None;
    }

    private static void ConfigureSceneCamerasAndLights(Scene scene, GameObject[] roots)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.008f, 0.012f, 0.028f, 1f);

        foreach (GameObject root in roots)
        {
            foreach (Camera sceneCamera in root.GetComponentsInChildren<Camera>(true))
            {
                sceneCamera.backgroundColor = new Color(0.001f, 0.002f, 0.008f, 1f);
                sceneCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }

            foreach (Light sceneLight in root.GetComponentsInChildren<Light>(true))
            {
                if (sceneLight.type != LightType.Directional)
                    continue;
                sceneLight.color = new Color(0.62f, 0.72f, 1f);
                sceneLight.intensity = 0.35f;
                sceneLight.shadows = LightShadows.Soft;
            }
        }

        GameObject accent = FindRoot(roots, "Neon Accent Light");
        if (accent == null)
            accent = new GameObject("Neon Accent Light");
        SceneManager.MoveGameObjectToScene(accent, scene);
        Light accentLight = GetOrAdd<Light>(accent);
        accentLight.type = LightType.Point;
        accentLight.color = DefaultNeonColor;
        accentLight.intensity = 1.2f;
        accentLight.range = 12f;
        accentLight.shadows = LightShadows.None;
    }

    private static void ConfigureVolume(Scene scene, GameObject[] roots, VolumeProfile profile)
    {
        Volume volume = FindComponent<Volume>(roots);
        if (volume == null)
        {
            GameObject volumeObject = new GameObject("Neon Global Volume");
            SceneManager.MoveGameObjectToScene(volumeObject, scene);
            volume = volumeObject.AddComponent<Volume>();
        }

        volume.isGlobal = true;
        volume.priority = 50f;
        volume.sharedProfile = profile;
    }

    private static void ConfigureEditableInterface(GameObject[] roots)
    {
        foreach (GameObject root in roots)
        {
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                Outline outline = graphic.GetComponent<Outline>();
                if (outline == null)
                    outline = graphic.gameObject.AddComponent<Outline>();
                outline.useGraphicAlpha = true;
                outline.effectColor = new Color(DefaultNeonColor.r, DefaultNeonColor.g, DefaultNeonColor.b, 0.85f);
                outline.effectDistance = graphic is TMP_Text
                    ? new Vector2(2f, -2f)
                    : new Vector2(3f, -3f);

                string lowerName = graphic.name.ToLowerInvariant();
                bool isDarkSurface = lowerName.Contains("panel") || lowerName.Contains("background") ||
                                     lowerName.Contains("backdrop") || lowerName.Contains("button") ||
                                     graphic.GetComponent<Button>() != null;

                if (graphic is TMP_Text || graphic is Text)
                    graphic.color = DefaultNeonColor;
                else if (isDarkSurface)
                    graphic.color = Color.black;
                else
                    graphic.color = new Color(DefaultNeonColor.r, DefaultNeonColor.g, DefaultNeonColor.b, graphic.color.a);

                Button button = graphic.GetComponent<Button>();
                if (button == null)
                    continue;

                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
                colors.selectedColor = Color.white;
                button.colors = colors;
            }
        }
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static T FindComponent<T>(GameObject[] roots) where T : Component
    {
        foreach (GameObject root in roots)
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }
        return null;
    }

    private static Transform FindTransform(GameObject[] roots, string objectName)
    {
        foreach (GameObject root in roots)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child;
            }
        }
        return null;
    }

    private static GameObject FindRoot(GameObject[] roots, string objectName)
    {
        foreach (GameObject root in roots)
        {
            if (root.name == objectName)
                return root;
        }
        return null;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void SaveOwnedAssets(ThemeAssets assets)
    {
        AssetDatabase.SaveAssetIfDirty(assets.Rim);
        AssetDatabase.SaveAssetIfDirty(assets.Pipe);
        AssetDatabase.SaveAssetIfDirty(assets.Ball);
        AssetDatabase.SaveAssetIfDirty(assets.Trail);
        AssetDatabase.SaveAssetIfDirty(assets.VolumeProfile);

        Texture trailTexture = assets.Trail.GetTexture("_MainTex");
        if (trailTexture != null)
            AssetDatabase.SaveAssetIfDirty(trailTexture);
    }
}
