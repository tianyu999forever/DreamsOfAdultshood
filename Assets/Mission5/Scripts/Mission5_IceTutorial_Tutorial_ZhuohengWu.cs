using TMPro;
using UnityEngine;

/// <summary>Contextual ice hints; only the reserved ice star starts the PocketWatch.</summary>
public class Mission5_IceTutorial_Tutorial_ZhuohengWu : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Mission5_IcePuzzle_IceSphere_ZhuohengWu ice;
    [SerializeField] private Transform iceVisual;
    [SerializeField] private Mission5_StarCollectible_ZhuohengWu tutorialStar;
    [SerializeField] private Mission5_CardHotbar_CardHotbar_ZhuohengWu hotbar;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private PocketWatch pocketWatch;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private CanvasGroup hintGroup;

    [Header("Guidance")]
    [SerializeField, Min(0f)] private float openingDelay = 2.5f;
    [SerializeField, Min(1f)] private float searchReminderDelay = 20f;
    [SerializeField, Min(1f)] private float sphereReminderDelay = 45f;
    [SerializeField, Min(1f)] private float approachDistance = 7f;
    [SerializeField, Min(0f)] private float experimentDelay = 4f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.35f;
    [SerializeField, Min(1f)] private float hintDuration = 4f;

    public bool CountdownStarted { get; private set; }
    private float explorationTime;
    private float nearbyTime;
    private float messageTime;
    private float messageDuration;
    private bool openingShown;
    private bool searchShown;
    private bool sphereShown;
    private bool approached;
    private bool cardHintShown;
    private bool triedCardOne;
    private bool meltNoticed;
    private bool releaseNoticed;
    private bool messageActive;

    private void Awake()
    {
        if (ice == null || iceVisual == null || tutorialStar == null || hotbar == null ||
            viewCamera == null || pocketWatch == null || hintText == null || hintGroup == null)
        {
            Debug.LogError("Mission 5 ice tutorial: assign all scene references.", this);
            enabled = false;
            return;
        }
        hintText.text = string.Empty;
        hintText.raycastTarget = false;
        hintGroup.alpha = 0f;
        hintGroup.blocksRaycasts = false;
        hintGroup.interactable = false;
    }

    private void OnEnable()
    {
        if (tutorialStar != null) tutorialStar.CollectedSuccessfully += OnTutorialStarCollected;
    }

    private void OnDisable()
    {
        if (tutorialStar != null) tutorialStar.CollectedSuccessfully -= OnTutorialStarCollected;
        ClearHint();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) { ClearHint(); return; }
        AdvanceFade();
        if (CountdownStarted) return;

        explorationTime += Time.deltaTime;
        if (ice.IsMelting)
        {
            if (!meltNoticed) { meltNoticed = true; ClearHint(); }
            if (!iceVisual.gameObject.activeSelf && !releaseNoticed)
            {
                releaseNoticed = true;
                ShowHint("There we go...");
            }
            return;
        }

        bool near = CanSeeNearbyIce();
        if (hotbar.SelectedSlotIndex == 0) triedCardOne = true;
        if (near)
        {
            nearbyTime += Time.deltaTime;
            if (!approached)
            {
                approached = true;
                ShowHint("Something's trapped inside...");
            }
            else if (!cardHintShown && !triedCardOne && nearbyTime >= experimentDelay && !messageActive)
            {
                cardHintShown = true;
                ShowHint("Try pressing 1. See what happens.");
            }
        }
        else if (approached && Vector3.Distance(viewCamera.transform.position, ice.transform.position) > approachDistance * 1.5f)
        {
            // Location-specific hints stop when the player walks away.
            ClearHint();
        }

        if (approached || messageActive) return;
        if (explorationTime >= sphereReminderDelay && !sphereShown)
        {
            sphereShown = true;
            searchShown = true;
            openingShown = true;
            ShowHint("Look for a pale, moving sphere. Something's inside.");
        }
        else if (explorationTime >= searchReminderDelay && !searchShown)
        {
            searchShown = true;
            openingShown = true;
            ShowHint("Take a look around. Something seems out of place.");
        }
        else if (explorationTime >= openingDelay && !openingShown)
        {
            openingShown = true;
            ShowHint("Something unusual is hiding in this room...");
        }
    }

    private bool CanSeeNearbyIce()
    {
        Vector3 offset = ice.transform.position - viewCamera.transform.position;
        if (offset.sqrMagnitude > approachDistance * approachDistance) return false;
        Vector3 viewport = viewCamera.WorldToViewportPoint(ice.transform.position);
        if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
            return false;
        // Raycast from the player's eyes so walls cannot reveal a nearby hidden puzzle.
        return Physics.Raycast(viewCamera.transform.position, offset.normalized, out RaycastHit hit,
            offset.magnitude + 0.8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
            hit.collider.GetComponentInParent<Mission5_IcePuzzle_IceSphere_ZhuohengWu>() == ice;
    }

    private void OnTutorialStarCollected()
    {
        if (CountdownStarted) return;
        CountdownStarted = true;
        pocketWatch.StartCountdown();
        ShowHint("One down. The clock starts now.");
    }

    private void ShowHint(string message)
    {
        hintText.text = message;
        messageTime = 0f;
        messageDuration = hintDuration + 2f * fadeDuration;
        messageActive = true;
        hintGroup.alpha = 0f;
    }

    private void AdvanceFade()
    {
        if (!messageActive) return;
        messageTime += Time.unscaledDeltaTime;
        hintGroup.alpha = Mathf.Clamp01(Mathf.Min(messageTime / fadeDuration,
            (messageDuration - messageTime) / fadeDuration));
        if (messageTime >= messageDuration) ClearHint();
    }

    private void ClearHint()
    {
        messageActive = false;
        if (hintGroup != null) hintGroup.alpha = 0f;
        if (hintText != null) hintText.text = string.Empty;
    }
}
