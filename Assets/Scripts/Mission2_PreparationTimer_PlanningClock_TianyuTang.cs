using TMPro;
using UnityEngine;

/// <summary>
/// Mission 2's safe planning period. When it ends, the 50-second escape timer begins.
/// Keep this on the PlanningClock object in Mission 2 only.
/// </summary>
public class Mission2_PreparationTimer_PlanningClock_TianyuTang : MonoBehaviour
{
    [Header("Preparation")]
    [SerializeField, Min(1f)] private float preparationSeconds = 15f;
    [SerializeField] private Mission2_TimeScore_GameManager_TianyuTang timeScoreManager;

    [Header("Optional Clock Visuals")]
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private Transform clockHand;
    [SerializeField] private string label = "PLAN: ";

    private float timeRemaining;
    private bool planning;

    private void Awake()
    {
        if (timeScoreManager == null)
            timeScoreManager = FindFirstObjectByType<Mission2_TimeScore_GameManager_TianyuTang>();
    }

    private void Start()
    {
        timeRemaining = preparationSeconds;
        planning = true;
        RefreshClock();
    }

    private void Update()
    {
        if (!planning)
            return;

        timeRemaining -= Time.deltaTime;
        RefreshClock();

        if (timeRemaining <= 0f)
        {
            planning = false;
            timeScoreManager?.BeginEscape();
        }
    }

    public void StopPlanning()
    {
        if (!planning)
            return;

        planning = false;

        if (clockText != null)
            clockText.text = "ESCAPE!";
    }

    private void RefreshClock()
    {
        float shownTime = Mathf.Max(0f, timeRemaining);

        if (clockText != null)
            clockText.text = label + Mathf.CeilToInt(shownTime) + "s";

        if (clockHand != null)
        {
            float progress = 1f - (shownTime / preparationSeconds);
            clockHand.localRotation = Quaternion.Euler(0f, 0f, -360f * progress);
        }
    }
}
