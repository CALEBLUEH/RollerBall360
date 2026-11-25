using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class NeonThemeController : MonoBehaviour
{
    private const float RescanInterval = 0.6f;

    private readonly List<Graphic> themedGraphics = new List<Graphic>();
    private readonly List<Outline> themedOutlines = new List<Outline>();
    private readonly List<Light> cyclingLights = new List<Light>();
    private readonly List<ParticleSystem> cyclingParticleSystems = new List<ParticleSystem>();
    private readonly List<Renderer> themedRenderers = new List<Renderer>();
    private readonly List<Renderer> trailRenderers = new List<Renderer>();

    private Material rimMaterial;
    private Material pipeMaterial;
    private Material trailMaterial;
    private MaterialPropertyBlock colorProperties;
    private MaterialPropertyBlock trailColorProperties;
    private float nextRescanTime;
    private Color neonColor = Color.cyan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<NeonThemeController>() != null)
            return;

        GameObject themeObject = new GameObject(nameof(NeonThemeController));
        DontDestroyOnLoad(themeObject);
        themeObject.AddComponent<NeonThemeController>();
    }

    private void Awake()
    {
        rimMaterial = Resources.Load<Material>("Neon/Materials/NeonRim");
        pipeMaterial = Resources.Load<Material>("Neon/Materials/NeonPipe");
        trailMaterial = Resources.Load<Material>("Neon/Materials/WhiteTrail");
        if (rimMaterial == null || pipeMaterial == null || trailMaterial == null)
        {
            Debug.LogError("Run Tools > MGD2 > Bake Editable Neon Theme to create the neon material assets.");
            enabled = false;
            return;
        }

        colorProperties = new MaterialPropertyBlock();
        trailColorProperties = new MaterialPropertyBlock();

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        CacheSceneObjects();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        float hue = Mathf.Repeat(Time.unscaledTime / 7.5f, 1f);
        neonColor = Color.HSVToRGB(hue, 0.88f, 1f, true);
        UpdateAnimatedColors();

        if (Time.unscaledTime >= nextRescanTime)
        {
            nextRescanTime = Time.unscaledTime + RescanInterval;
            CacheInterface();
            CacheParticles();
            CacheRenderers();
            RemoveDestroyedReferences();
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        nextRescanTime = 0f;
        CacheSceneObjects();
    }

    private void CacheSceneObjects()
    {
        if (!enabled)
            return;

        cyclingLights.Clear();
        Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Light sceneLight in lights)
        {
            if (sceneLight.type != LightType.Directional)
                cyclingLights.Add(sceneLight);
        }

        CacheInterface();
        CacheParticles();
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        themedRenderers.Clear();
        trailRenderers.Clear();
        Renderer[] renderers = FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Renderer sceneRenderer in renderers)
        {
            foreach (Material sharedMaterial in sceneRenderer.sharedMaterials)
            {
                if (sharedMaterial == rimMaterial || sharedMaterial == pipeMaterial)
                {
                    themedRenderers.Add(sceneRenderer);
                    break;
                }

                if (sharedMaterial == trailMaterial)
                {
                    trailRenderers.Add(sceneRenderer);
                    break;
                }
            }
        }
    }

    private void CacheParticles()
    {
        ParticleSystem[] particles = FindObjectsByType<ParticleSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (ParticleSystem particleSystem in particles)
        {
            if (!cyclingParticleSystems.Contains(particleSystem))
                cyclingParticleSystems.Add(particleSystem);
        }
    }

    private void CacheInterface()
    {
        themedGraphics.Clear();
        themedOutlines.Clear();

        Graphic[] graphics = FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null)
                continue;

            themedGraphics.Add(graphic);
            Outline outline = graphic.GetComponent<Outline>();

            // Static UI is baked and editable. This fallback is for the joystick,
            // which PlayerController creates only when the game runs.
            if (outline == null)
            {
                outline = graphic.gameObject.AddComponent<Outline>();
                outline.useGraphicAlpha = true;
                outline.effectDistance = graphic is TMP_Text
                    ? new Vector2(2f, -2f)
                    : new Vector2(3f, -3f);
            }

            themedOutlines.Add(outline);
            ApplyGraphicColor(graphic);
        }
    }

    private void UpdateAnimatedColors()
    {
        if (rimMaterial == null || pipeMaterial == null)
            return;

        colorProperties.Clear();
        colorProperties.SetColor("_NeonColor", neonColor);
        foreach (Renderer sceneRenderer in themedRenderers)
        {
            if (sceneRenderer != null)
                sceneRenderer.SetPropertyBlock(colorProperties);
        }

        trailColorProperties.Clear();
        trailColorProperties.SetColor("_Color", neonColor);
        foreach (Renderer trailRenderer in trailRenderers)
        {
            if (trailRenderer != null)
                trailRenderer.SetPropertyBlock(trailColorProperties);
        }

        foreach (Light cyclingLight in cyclingLights)
        {
            if (cyclingLight != null)
                cyclingLight.color = neonColor;
        }

        foreach (ParticleSystem particles in cyclingParticleSystems)
        {
            if (particles == null)
                continue;

            ParticleSystem.MainModule main = particles.main;
            main.startColor = neonColor;
        }

        foreach (Outline outline in themedOutlines)
        {
            if (outline != null)
                outline.effectColor = new Color(neonColor.r, neonColor.g, neonColor.b, 0.85f);
        }

        foreach (Graphic graphic in themedGraphics)
        {
            if (graphic != null)
                ApplyGraphicColor(graphic);
        }
    }

    private void ApplyGraphicColor(Graphic graphic)
    {
        string lowerName = graphic.name.ToLowerInvariant();
        bool isDarkSurface = lowerName.Contains("panel") || lowerName.Contains("background") ||
                             lowerName.Contains("backdrop") || lowerName.Contains("button") ||
                             graphic.GetComponent<Button>() != null;

        if (graphic is TMP_Text || graphic is Text)
        {
            graphic.color = neonColor;
        }
        else if (isDarkSurface)
        {
            graphic.color = Color.black;
        }
        else
        {
            float originalAlpha = graphic.color.a;
            graphic.color = new Color(neonColor.r, neonColor.g, neonColor.b, originalAlpha);
        }

        Button button = graphic.GetComponent<Button>();
        if (button == null)
            return;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }

    private void RemoveDestroyedReferences()
    {
        themedGraphics.RemoveAll(item => item == null);
        themedOutlines.RemoveAll(item => item == null);
        cyclingLights.RemoveAll(item => item == null);
        cyclingParticleSystems.RemoveAll(item => item == null);
        themedRenderers.RemoveAll(item => item == null);
        trailRenderers.RemoveAll(item => item == null);
    }
}
