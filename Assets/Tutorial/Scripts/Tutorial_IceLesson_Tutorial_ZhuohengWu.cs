using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>A self-contained, untimed lesson using Mission 5's fire-box and ice-shrink mechanics.</summary>
public class Tutorial_IceLesson_Tutorial_ZhuohengWu : MonoBehaviour
{
    [Header("Player and Fire")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform viewCamera;
    [SerializeField] private Transform fireOrigin;
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Color normalColor = new Color32(40, 46, 58, 255);
    [SerializeField] private Color selectedColor = new Color32(184, 116, 24, 255);
    [SerializeField] private Vector3 fireBoxSize = new Vector3(1.4f, 1.8f, 4f);

    [Header("Stationary Ice and Star")]
    [SerializeField] private Transform iceVisual;
    [SerializeField] private SphereCollider iceCollider;
    [SerializeField] private Tutorial_StarCollectible_Star_ZhuohengWu star;
    [SerializeField] private Vector3 starLandingPosition;
    [SerializeField, Min(0.1f)] private float meltDuration = 0.6f;
    [SerializeField, Min(0.1f)] private float dropDuration = 0.4f;

    [Header("Guidance")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private CanvasGroup instructionGroup;
    [SerializeField] private TMP_Text continuePrompt;
    [SerializeField, Min(0.1f)] private float fadeDuration = 0.25f;
    [SerializeField, Min(1f)] private float approachDistance = 4.5f;

    public bool IsMelting { get; private set; }
    public bool StarReady { get; private set; }
    public bool Completed { get; private set; }
    public bool FireActive { get; private set; }
    private int introductionStep;
    private int lastDialogueClickFrame = -1;
    private bool triedCardOne;
    private bool experimentPromptShown;
    private float experimentPromptTime;
    private float discoveryMessageTime;
    private string desiredMessage;
    private bool emitting;

    private void Awake()
    {
        Time.timeScale = 1f;
        if (player == null || viewCamera == null || fireOrigin == null || fireParticles == null ||
            cardBackground == null || iceVisual == null || iceCollider == null || star == null ||
            instructionText == null || instructionGroup == null || continuePrompt == null)
        {
            Debug.LogError("Tutorial: all lesson references must be assigned.", this);
            enabled = false;
            return;
        }
        star.SetPickupEnabled(false);
        cardBackground.color = normalColor;
        instructionGroup.alpha = 0f;
        instructionGroup.blocksRaycasts = false;
        instructionGroup.interactable = false;
        instructionText.raycastTarget = false;
        continuePrompt.text = "Click to continue";
        continuePrompt.raycastTarget = false;
        desiredMessage = instructionText.text = "WASD to move. Use your mouse to look around.";
        StopFire();
    }

    private void Update()
    {
        if (!Completed)
        {
            if (StarReady)
            {
                discoveryMessageTime -= Time.deltaTime;
                ShowMessage(discoveryMessageTime > 0f ? "Looks like you've figured something out." : string.Empty);
            }
            else if (!IsMelting)
            {
                if (introductionStep < 2)
                {
                    ShowMessage(introductionStep == 0
                        ? "WASD to move. Use your mouse to look around."
                        : "Your cards have different abilities, but their effects are yours to discover.");
                    // Ignore clicks during fades; a held button cannot advance again.
                    if (Application.isFocused && Mouse.current != null &&
                        Mouse.current.leftButton.wasPressedThisFrame && lastDialogueClickFrame != Time.frameCount &&
                        instructionText.text == desiredMessage && instructionGroup.alpha >= 0.99f)
                    {
                        lastDialogueClickFrame = Time.frameCount;
                        introductionStep++;
                        ShowMessage(introductionStep == 1
                            ? "Your cards have different abilities, but their effects are yours to discover."
                            : string.Empty);
                    }
                }
                else
                {
                    // Offer experimentation once, without repeating it when the player moves away.
                    if (!experimentPromptShown && !triedCardOne &&
                        Vector3.Distance(player.position, iceCollider.transform.position) <= approachDistance)
                    {
                        experimentPromptShown = true;
                        experimentPromptTime = 5f;
                    }
                    experimentPromptTime -= Time.deltaTime;
                    ShowMessage(!triedCardOne && experimentPromptTime > 0f
                        ? "Try holding 1 and see what happens." : string.Empty);
                }
            }
        }
        UpdateMessageFade();
        continuePrompt.gameObject.SetActive(introductionStep < 2 && !IsMelting && !Completed &&
            instructionText.text == desiredMessage && !string.IsNullOrEmpty(desiredMessage));
    }

    private void LateUpdate()
    {
        FireActive = !Completed && Time.timeScale > 0f && Keyboard.current != null &&
            Keyboard.current.digit1Key.isPressed && Application.isFocused;
        if (FireActive) triedCardOne = true;
        cardBackground.color = FireActive ? selectedColor : normalColor;
        if (!FireActive) { StopFire(); return; }
        if (!emitting) { fireParticles.Play(true); emitting = true; }
        if (IsMelting) return;

        // Preserve the original fire ability's oriented overlap box and held-key behavior.
        Vector3 center = fireOrigin.position + fireOrigin.forward * (fireBoxSize.z * 0.5f);
        foreach (Collider hit in Physics.OverlapBox(center, fireBoxSize * 0.5f,
            fireOrigin.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit != iceCollider) continue;
            TryMelt();
            break;
        }
    }

    public void TryMelt()
    {
        if (!isActiveAndEnabled || IsMelting || Completed) return;
        introductionStep = 2; // Progress takes priority; never resume stale introductory dialogue.
        continuePrompt.gameObject.SetActive(false);
        IsMelting = true;
        iceCollider.enabled = false;
        ShowMessage(string.Empty);
        StartCoroutine(MeltAndRelease());
    }

    private IEnumerator MeltAndRelease()
    {
        Vector3 fullScale = iceVisual.localScale;
        float elapsed = 0f;
        while (elapsed < meltDuration)
        {
            elapsed += Time.deltaTime;
            iceVisual.localScale = Vector3.Lerp(fullScale, Vector3.zero, elapsed / meltDuration);
            yield return null;
        }
        iceVisual.gameObject.SetActive(false);
        Vector3 start = star.transform.position;
        elapsed = 0f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropDuration);
            star.transform.position = Vector3.Lerp(start, starLandingPosition, t * t);
            yield return null;
        }
        star.transform.position = starLandingPosition;
        Physics.SyncTransforms();
        StarReady = true;
        star.SetPickupEnabled(true);
        discoveryMessageTime = 5f;
        ShowMessage("Looks like you've figured something out.");
    }

    public bool CompleteTutorial()
    {
        if (!StarReady || Completed || !isActiveAndEnabled) return false;
        Completed = true;
        FireActive = false;
        cardBackground.color = normalColor;
        StopFire();
        ShowMessage("Well done! Keep experimenting. There's more to discover.");
        return true;
    }

    private void ShowMessage(string message) => desiredMessage = message;

    private void UpdateMessageFade()
    {
        bool changing = instructionText.text != desiredMessage;
        instructionGroup.alpha = Mathf.MoveTowards(instructionGroup.alpha,
            changing ? 0f : 1f, Time.unscaledDeltaTime / fadeDuration);
        if (changing && instructionGroup.alpha <= 0f) instructionText.text = desiredMessage;
    }

    private void StopFire()
    {
        if (fireParticles != null && (emitting || fireParticles.isPlaying || fireParticles.particleCount > 0))
            fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        emitting = false;
    }

    private void OnDisable()
    {
        FireActive = false;
        StopFire();
        if (cardBackground != null) cardBackground.color = normalColor;
    }
}
