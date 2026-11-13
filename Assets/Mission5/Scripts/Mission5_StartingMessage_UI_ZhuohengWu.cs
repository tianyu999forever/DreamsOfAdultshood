using TMPro;
using UnityEngine;

/// <summary>Shows the starting-room message until the existing countdown trigger activates.</summary>
public class Mission5_StartingMessage_UI_ZhuohengWu : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private CanvasGroup messageGroup;
    [SerializeField] private StartTimerTrigger startTimerTrigger;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.35f;
    private Collider triggerVolume;
    private bool countdownStarted;

    private void Awake()
    {
        triggerVolume = startTimerTrigger != null ? startTimerTrigger.GetComponent<Collider>() : null;
        if (messageText == null || messageGroup == null || triggerVolume == null)
        {
            Debug.LogError("Mission 5 starting message: assign the text, Canvas Group and timer trigger.", this);
            enabled = false;
            return;
        }
        messageText.text = "The countdown starts when you leave this room.";
        messageText.raycastTarget = false;
        messageGroup.alpha = 1f;
        messageGroup.blocksRaycasts = false;
        messageGroup.interactable = false;
    }

    private void LateUpdate()
    {
        // The original trigger disables its collider only after starting the PocketWatch.
        if (!countdownStarted && triggerVolume != null && !triggerVolume.enabled)
            countdownStarted = true;
        if (!countdownStarted) return;

        messageGroup.alpha = Mathf.MoveTowards(messageGroup.alpha, 0f,
            Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration));
        if (messageGroup.alpha <= 0f) messageText.text = string.Empty;
    }
}
