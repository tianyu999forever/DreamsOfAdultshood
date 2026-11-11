using UnityEngine;
using TMPro;

public class Mission2_BatteryUI_Canvas_TianyuTang : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Mission2_Flashlight_MainCamera_TianyuTang flashlight;

    [SerializeField]
    private TMP_Text batteryText;

    [Header("Display")]
    [SerializeField, Min(0f)]
    private float lowBatteryThreshold = 3f;

    [SerializeField]
    private Color normalColor = Color.white;

    [SerializeField]
    private Color lowBatteryColor = new Color(1f, 0.25f, 0.2f);

    private void Start()
    {
        if (flashlight == null || batteryText == null)
        {
            Debug.LogError(
                "Assign Flashlight and Battery Text in the Inspector.",
                this);

            enabled = false;
            return;
        }

        UpdateDisplay();
    }

    private void Update()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        float remaining = flashlight.BatteryRemaining;

        if (remaining <= 0f)
        {
            batteryText.text = "BATTERY EMPTY";
            batteryText.color = lowBatteryColor;
            return;
        }

        int seconds = Mathf.CeilToInt(remaining);

        batteryText.text = $"BATTERY {seconds}s";

        batteryText.color = remaining <= lowBatteryThreshold
            ? lowBatteryColor
            : normalColor;
    }
}