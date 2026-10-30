using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public class Mission6_Photo_Player_Jizhijin : MonoBehaviour
{
    public GameObject photoPanel;
    public TMP_Text statusText, countdownText;
    public Moving moving;
    public Mission6_Grid_Level_Jizhijin level;
    public const float Duration = 5f;
    public bool Used { get; private set; }
    public bool IsVisible => photoPanel != null && photoPanel.activeSelf;
    float expiresAt;
    void Start() { Close(); Refresh(); }
    public void SetLevel(Mission6_Grid_Level_Jizhijin next)
    {
        Close(); level = next; photoPanel = next.photoPanel; countdownText = next.photoTitle; Used = false; Refresh();
    }
    public void Close() { if (photoPanel != null) photoPanel.SetActive(false); Refresh(); }
    bool CanUse() => level != null && level.campaign.Phase == Mission6_Phase_Jizhijin.Exploring && !level.campaign.PuzzleOpen && Time.timeScale > 0 && moving.enabled;
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame) TryUsePhoto();
        if (!IsVisible) return;
        if (!CanUse() || Time.unscaledTime >= expiresAt) { Close(); return; }
        countdownText.text = "MAP PHOTO / " + Mathf.CeilToInt(expiresAt - Time.unscaledTime) + "s";
    }
    public bool TryUsePhoto()
    {
        if (Used || !CanUse() || photoPanel == null) return false;
        Used = true; expiresAt = Time.unscaledTime + Duration; photoPanel.SetActive(true); countdownText.text = "MAP PHOTO / 5s"; Refresh(); return true;
    }
    void Refresh() { if (statusText != null) statusText.text = Used ? "PHOTO USED / 0 LEFT" : "Z MAP PHOTO / ONCE THIS LEVEL / 5s"; }
    void OnDisable() { Close(); }
}
