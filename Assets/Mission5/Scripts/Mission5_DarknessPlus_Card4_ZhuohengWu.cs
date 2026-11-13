using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Card 4 dims the assigned room lights and reveals the hidden star pickup.</summary>
// Restore last frame's lighting before the existing FlickeringLight coroutines run.
[DefaultExecutionOrder(-1000)]
public class Mission5_DarknessPlus_Card4_ZhuohengWu : MonoBehaviour
{
    [Header("Existing Scene References")]
    [SerializeField] private Mission5_CardHotbar_CardHotbar_ZhuohengWu hotbar;
    [SerializeField] private Transform[] roomLightRoots;
    [SerializeField] private Light[] additionalRoomLights;
    [SerializeField] private Mission5_StarCollectible_ZhuohengWu hiddenStar;
    [SerializeField] private GameObject starVisual;
    [SerializeField] private Collider starPickupCollider;
    [SerializeField] private Light starGlow;

    [Header("Dark Room")]
    [SerializeField, Range(0.01f, 1f)] private float darkIntensityMultiplier = 0.04f;
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.6f;
    [SerializeField] private Color darkAmbientColor = new Color(0.035f, 0.045f, 0.055f, 1f);
    [SerializeField, Range(0f, 1f)] private float darkReflectionMultiplier = 0.15f;

    [Header("Star Glow")]
    [SerializeField, Min(0f)] private float glowIntensity = 0.35f;

    public bool StarCollected { get; private set; }

    private readonly List<Light> lights = new List<Light>();
    private readonly List<LensFlareComponentSRP> flares = new List<LensFlareComponentSRP>();
    private readonly List<Renderer> panels = new List<Renderer>();
    private float[] lightIntensities;
    private float[] flareIntensities;
    private MaterialPropertyBlock[] normalPanelBlocks;
    private MaterialPropertyBlock panelBlock;
    private Color ambientSky, ambientEquator, ambientGround;
    private float ambientIntensity, reflectionIntensity;
    private float darkness;
    private bool initialized, lightingApplied, revealed;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void OnEnable()
    {
        if (hotbar != null) hotbar.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(int slot)
    {
        if (slot != 2 && slot != 4) return;
        SetStarVisible(false);
        darkness = 0f;
        RestoreLighting();
    }

    private void Awake()
    {
        // Fail closed even if another required reference was missed during manual setup.
        SetStarVisible(false);
        if (hotbar == null || hiddenStar == null || starVisual == null ||
            starPickupCollider == null || starGlow == null ||
            starPickupCollider.gameObject != hiddenStar.gameObject ||
            !starPickupCollider.isTrigger ||
            !starVisual.transform.IsChildOf(hiddenStar.transform) ||
            starVisual == hiddenStar.gameObject ||
            transform.IsChildOf(hiddenStar.transform))
        {
            Debug.LogError("Mission 5 Card 4: assign Hotbar, the HiddenStar Star Collectible, " +
                "its child Visual, its root trigger Collider, and its glow Light. " +
                "Put this controller on a separate active GameObject.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (roomLightRoots != null)
        {
            foreach (Transform root in roomLightRoots)
            {
                if (root == null) continue;
                foreach (Light light in root.GetComponentsInChildren<Light>(true)) AddLight(light);
                foreach (LensFlareComponentSRP flare in root.GetComponentsInChildren<LensFlareComponentSRP>(true))
                    if (!flares.Contains(flare)) flares.Add(flare);
                foreach (Renderer panel in root.GetComponentsInChildren<Renderer>(true))
                    if (panel.sharedMaterial != null && panel.sharedMaterial.HasProperty(EmissionColor) &&
                        !panels.Contains(panel)) panels.Add(panel);
            }
        }
        if (additionalRoomLights != null)
            foreach (Light light in additionalRoomLights) AddLight(light);

        if (lights.Count == 0)
        {
            Debug.LogError("Mission 5 Card 4: assign CeilinglampGroup to Room Light Roots.", this);
            enabled = false;
            return;
        }

        lightIntensities = new float[lights.Count];
        flareIntensities = new float[flares.Count];
        normalPanelBlocks = new MaterialPropertyBlock[panels.Count];
        for (int i = 0; i < panels.Count; i++) normalPanelBlocks[i] = new MaterialPropertyBlock();
        panelBlock = new MaterialPropertyBlock();
        ambientSky = RenderSettings.ambientSkyColor;
        ambientEquator = RenderSettings.ambientEquatorColor;
        ambientGround = RenderSettings.ambientGroundColor;
        ambientIntensity = RenderSettings.ambientIntensity;
        reflectionIntensity = RenderSettings.reflectionIntensity;
        initialized = true;
    }

    private void AddLight(Light light)
    {
        if (light != null && light != starGlow && !lights.Contains(light)) lights.Add(light);
    }

    private void Update()
    {
        // No scaled value is ever used as the next frame's baseline. Existing flicker
        // scripts may now write their normal/off values, which LateUpdate will capture.
        RestoreLighting();
    }

    private void LateUpdate()
    {
        if (!initialized) return;

        // Collectible disables its root collider synchronously after adding a star to the HUD.
        // Observe that handshake BEFORE applying our own hide/release state. Never
        // re-enable a collected trigger during its 0.5-second destruction animation.
        if (!StarCollected && (hiddenStar == null || starPickupCollider == null ||
            hiddenStar.Collected)) StarCollected = true;

        bool held = hotbar != null && hotbar.isActiveAndEnabled &&
            hotbar.SelectedSlotIndex == 3 && Time.timeScale > 0f;
        SetStarVisible(held && !StarCollected && hiddenStar != null &&
            hiddenStar.isActiveAndEnabled);

        darkness = Mathf.MoveTowards(darkness, held ? 1f : 0f,
            Time.deltaTime / Mathf.Max(0.01f, transitionDuration));
        if (darkness > 0f) ApplyLighting();
    }

    private void SetStarVisible(bool visible)
    {
        if (starVisual != null) starVisual.SetActive(visible);
        if (starPickupCollider != null) starPickupCollider.enabled = visible;
        if (starGlow != null)
        {
            starGlow.intensity = Mathf.Max(0f, glowIntensity);
            starGlow.enabled = visible;
        }
        revealed = visible;
    }

    private void ApplyLighting()
    {
        float multiplier = Mathf.Lerp(1f, darkIntensityMultiplier, darkness);
        for (int i = 0; i < lights.Count; i++)
        {
            if (lights[i] == null) continue;
            lightIntensities[i] = lights[i].intensity;
            lights[i].intensity = lightIntensities[i] * multiplier;
        }
        for (int i = 0; i < flares.Count; i++)
        {
            if (flares[i] == null) continue;
            flareIntensities[i] = flares[i].intensity;
            flares[i].intensity = flareIntensities[i] * multiplier;
        }
        for (int i = 0; i < panels.Count; i++)
        {
            Renderer panel = panels[i];
            if (panel == null) continue;
            panel.GetPropertyBlock(normalPanelBlocks[i]);
            panel.GetPropertyBlock(panelBlock);
            Color emission = panelBlock.HasColor(EmissionColor) ? panelBlock.GetColor(EmissionColor) :
                panel.sharedMaterial.GetColor(EmissionColor);
            panelBlock.SetColor(EmissionColor, emission * multiplier);
            panel.SetPropertyBlock(panelBlock);
        }
        // Mission5 currently uses flat black ambient lighting. A faint cool floor
        // keeps navigation possible; the original values return exactly on release.
        RenderSettings.ambientSkyColor = Color.Lerp(ambientSky, darkAmbientColor, darkness);
        RenderSettings.ambientEquatorColor = Color.Lerp(ambientEquator, darkAmbientColor, darkness);
        RenderSettings.ambientGroundColor = Color.Lerp(ambientGround, darkAmbientColor, darkness);
        RenderSettings.ambientIntensity = Mathf.Lerp(ambientIntensity, 1f, darkness);
        RenderSettings.reflectionIntensity = reflectionIntensity *
            Mathf.Lerp(1f, darkReflectionMultiplier, darkness);
        lightingApplied = true;
    }

    private void RestoreLighting()
    {
        if (!initialized || !lightingApplied) return;
        for (int i = 0; i < lights.Count; i++)
            if (lights[i] != null) lights[i].intensity = lightIntensities[i];
        for (int i = 0; i < flares.Count; i++)
            if (flares[i] != null) flares[i].intensity = flareIntensities[i];
        for (int i = 0; i < panels.Count; i++)
            if (panels[i] != null) panels[i].SetPropertyBlock(normalPanelBlocks[i]);
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;
        RenderSettings.ambientIntensity = ambientIntensity;
        RenderSettings.reflectionIntensity = reflectionIntensity;
        lightingApplied = false;
    }

    private void OnDisable()
    {
        if (hotbar != null) hotbar.SelectionChanged -= OnSelectionChanged;
        // Preserve the collection handshake if this component is disabled that frame.
        if (hiddenStar != null && hiddenStar.Collected) StarCollected = true;
        RestoreLighting();
        darkness = 0f;
        SetStarVisible(false);
    }
}
