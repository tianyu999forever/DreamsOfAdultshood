using TMPro;
using UnityEngine;

/// <summary>One non-blocking notification for every successful Mission 5 star registration.</summary>
public class Mission5_StarFeedback_UI_ZhuohengWu : MonoBehaviour
{
    [SerializeField] private Mission5_StarInventory_ZhuohengWu inventory;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private CanvasGroup messageGroup;
    private const float Duration = 3f;
    private const float FadeDuration = 0.2f;
    private float startedAt = -1f;
    private float initialAlpha;

    private void Awake()
    {
        if (inventory == null || messageText == null || messageGroup == null)
        {
            Debug.LogError("Mission 5 star feedback: assign Inventory, Text and Canvas Group.", this);
            enabled = false;
            return;
        }
        messageText.text = string.Empty;
        messageText.raycastTarget = false;
        messageGroup.alpha = 0f;
        messageGroup.blocksRaycasts = false;
        messageGroup.interactable = false;
        messageGroup.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (inventory != null) inventory.StarCollected += Show;
    }

    private void Show()
    {
        // Rapid collections refresh the same message without creating overlapping UI.
        initialAlpha = messageGroup.gameObject.activeSelf ? messageGroup.alpha : 0f;
        messageText.text = "Star Collected!";
        messageGroup.gameObject.SetActive(true);
        startedAt = Time.unscaledTime;
    }

    private void Update()
    {
        if (startedAt < 0f) return;
        float elapsed = Time.unscaledTime - startedAt;
        if (elapsed >= Duration)
        {
            Hide();
            return;
        }
        float fadeIn = Mathf.Lerp(initialAlpha, 1f, Mathf.Clamp01(elapsed / FadeDuration));
        messageGroup.alpha = Mathf.Min(fadeIn, Mathf.Clamp01((Duration - elapsed) / FadeDuration));
    }

    private void Hide()
    {
        startedAt = -1f;
        if (messageText != null) messageText.text = string.Empty;
        if (messageGroup != null)
        {
            messageGroup.alpha = 0f;
            messageGroup.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.StarCollected -= Show;
        Hide();
    }
}
