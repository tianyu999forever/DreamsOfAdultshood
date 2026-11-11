using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Mission2_Flashlight_MainCamera_TianyuTang : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light flashlight;

    [Header("Battery — measured in seconds")]
    [SerializeField, Min(1f)] private float maxBattery = 90f;
    [SerializeField, Min(0f)] private float startingBattery = 90f;
    [SerializeField, Min(0f)] private float lowBatteryThreshold = 15f;

    [Header("Low battery flicker")]
    [SerializeField, Min(0.01f)] private float minimumFlickerInterval = 0.05f;
    [SerializeField, Min(0.01f)] private float maximumFlickerInterval = 0.25f;

    private float battery;
    private float flickerTimer;
    private bool isOn;
    private bool flickerVisible = true;

    public float BatteryRemaining => battery;
    public float BatteryPercent => battery / maxBattery;
    public bool IsOn => isOn;

    private void Awake()
    {
        if (flashlight == null)
        {
            Debug.LogError(
                "Assign the flashlight Light in the Inspector.", this);

            enabled = false;
            return;
        }

        battery = Mathf.Clamp(startingBattery, 0f, maxBattery);
        isOn = false;
        flashlight.enabled = false;
    }

    private void Update()
    {
        if (PressedToggle())
        {
            ToggleFlashlight();
        }

        if (!isOn)
        {
            return;
        }

        // Battery drains even during the dark part of a flicker.
        battery = Mathf.Max(0f, battery - Time.deltaTime);

        if (battery <= 0f)
        {
            isOn = false;
            flashlight.enabled = false;
            return;
        }

        if (battery <= lowBatteryThreshold)
        {
            UpdateFlicker();
        }
        else
        {
            flickerVisible = true;
            flickerTimer = 0f;
            flashlight.enabled = true;
        }
    }

    private bool PressedToggle()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null
            && Keyboard.current.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Q);
#endif
    }

    public void ToggleFlashlight()
    {
        if (!isOn && battery <= 0f)
        {
            return;
        }

        isOn = !isOn;
        flickerVisible = true;
        flickerTimer = 0f;
        flashlight.enabled = isOn;
    }

    private void UpdateFlicker()
    {
        flickerTimer -= Time.deltaTime;

        if (flickerTimer <= 0f)
        {
            flickerVisible = !flickerVisible;

            flickerTimer = Random.Range(
                minimumFlickerInterval,
                Mathf.Max(
                    minimumFlickerInterval,
                    maximumFlickerInterval));
        }

        flashlight.enabled = flickerVisible;
    }

    // Call this when the player collects a battery.
    public void AddBattery(float seconds)
    {
        battery = Mathf.Clamp(
            battery + Mathf.Max(0f, seconds),
            0f,
            maxBattery);

        if (isOn && battery > lowBatteryThreshold)
        {
            flickerVisible = true;
            flickerTimer = 0f;
            flashlight.enabled = true;
        }
    }

    private void OnDisable()
    {
        isOn = false;

        if (flashlight != null)
        {
            flashlight.enabled = false;
        }
    }
}