using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Mission6_Photo_Player_Jizhijin : MonoBehaviour
{
    public GameObject photoPanel;
    public TMP_Text statusText;
    public TMP_Text countdownText;
    public Moving moving;
    public Mission6_Grid_Level_Jizhijin level;
    public const float Duration = 5f;
    public bool Used { get; private set; }
    public bool IsVisible => photoPanel != null && photoPanel.activeSelf;
    float expiresAt;

    void Start()
    {
        Used = false;
        photoPanel.SetActive(false);
        RefreshStatus();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame) TryUsePhoto();
        if (!IsVisible) return;
        // Close on death or when the exit opens, even though the game is paused there.
        if (!CanUse() || Time.unscaledTime >= expiresAt)
        {
            photoPanel.SetActive(false);
            RefreshStatus();
            return;
        }
        countdownText.text = "MAP PHOTO  /  " + Mathf.CeilToInt(expiresAt - Time.unscaledTime) + "s";
    }

    bool CanUse() => Time.timeScale > 0f && moving != null && moving.enabled && (level == null || !level.IsPlanning);

    public bool TryUsePhoto()
    {
        if (Used || !CanUse() || photoPanel == null) return false;
        Used = true;
        expiresAt = Time.unscaledTime + Duration;
        countdownText.text = "MAP PHOTO  /  5s";
        photoPanel.SetActive(true);
        RefreshStatus();
        return true;
    }

    void RefreshStatus()
    {
        if (statusText != null) statusText.text = Used ? "PHOTO USED  /  0 LEFT" : "Z  MAP PHOTO  /  1 USE  /  5s";
    }

    void OnDisable()
    {
        if (photoPanel != null) photoPanel.SetActive(false);
    }
}
