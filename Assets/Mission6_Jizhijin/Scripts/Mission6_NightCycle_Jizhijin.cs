using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(250)]
public class Mission6_NightCycle_Jizhijin : MonoBehaviour
{
    public const float DaySeconds = 30, NightSeconds = 10, EnemyMultiplier = 1.6f;
    public Mission6_Campaign_Jizhijin campaign;
    public Light sunlight;
    public TMP_Text countdown;
    public Renderer[] letterRenderers;
    public RawImage photoMap;
    public Texture dayMap, nightMap;
    public float CycleTime { get; private set; }
    public bool IsNight { get; private set; }
    public float SecondsLeft => IsNight ? DaySeconds + NightSeconds - CycleTime : DaySeconds - CycleTime;
    public string Status => IsNight ? "NIGHT ENDS IN " + Mathf.CeilToInt(SecondsLeft) + "s   /   ENEMIES x1.6" : "NIGHT IN " + Mathf.CeilToInt(SecondsLeft) + "s";
    Color dayAmbient, dayBackground, dayFogColor;
    float dayIntensity, dayFogStart, dayFogEnd;
    bool dayFog;
    FogMode dayFogMode;
    Camera playerCamera;
    void Awake()
    {
        playerCamera = campaign.moving.GetComponentInChildren<Camera>();
        dayAmbient = RenderSettings.ambientLight; dayBackground = playerCamera.backgroundColor;
        dayIntensity = sunlight.intensity; dayFog = RenderSettings.fog; dayFogMode = RenderSettings.fogMode;
        dayFogColor = RenderSettings.fogColor; dayFogStart = RenderSettings.fogStartDistance; dayFogEnd = RenderSettings.fogEndDistance;
    }
    public void ResetCycle() { CycleTime = 0; Apply(false); }
    void Update()
    {
        if (campaign.HazardsActive)
        {
            CycleTime = (CycleTime + Time.deltaTime) % (DaySeconds + NightSeconds);
            bool dark = CycleTime >= DaySeconds;
            if (dark != IsNight) Apply(dark);
        }
        bool show = campaign.CurrentLevel != null && campaign.CurrentLevel.stageNumber == 2 && campaign.Phase == Mission6_Phase_Jizhijin.Exploring && !campaign.PuzzleOpen && !campaign.photo.IsVisible;
        countdown.gameObject.SetActive(show);
        countdown.text = Status;
        countdown.color = IsNight ? new Color(1, .65f, .3f) : new Color(.8f, .9f, 1);
    }
    void Apply(bool dark)
    {
        IsNight = dark;
        RenderSettings.ambientLight = dark ? new Color(.14f, .17f, .27f) : dayAmbient;
        sunlight.intensity = dark ? .1f : dayIntensity;
        playerCamera.backgroundColor = dark ? new Color(.015f, .025f, .055f) : dayBackground;
        RenderSettings.fog = dark || dayFog;
        RenderSettings.fogMode = dark ? FogMode.Linear : dayFogMode;
        RenderSettings.fogColor = dark ? new Color(.025f, .035f, .07f) : dayFogColor;
        RenderSettings.fogStartDistance = dark ? 3.5f : dayFogStart;
        RenderSettings.fogEndDistance = dark ? 11 : dayFogEnd;
        foreach (var renderer in letterRenderers) if (renderer != null) renderer.enabled = !dark;
        photoMap.texture = dark ? nightMap : dayMap;
    }
    public void RestoreDay() { Apply(false); if (countdown != null) countdown.gameObject.SetActive(false); }
}
