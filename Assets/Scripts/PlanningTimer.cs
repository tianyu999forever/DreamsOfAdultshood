using TMPro;
using UnityEngine;

public class PlanningTimer : MonoBehaviour
{
    [Header("prepare time")]
    [Min(1f)]
    public float planningTime = 20f;

    [Header("change text")]
    public string planningLabel = "PLAN NOW: ";
    public string leaveLabel = "GO!";

    [Header("showed on wall")]
    public TMP_Text countdownText;
    public Transform clockHandPivot;

    [Tooltip(
        "choose virtical to lacal watch board：" +
        "Z=(0,0,1)，X=(1,0,0)，Y=(0,1,0)"
    )]
    public Vector3 handRotationAxis =
        Vector3.forward;

    [Header("death system now had")]
    public PocketWatch pocketWatch;

    [Header("text color")]
    public Color normalColor = Color.white;
    public Color dangerColor =
        new Color(1f, 0.15f, 0.05f);

    private float timeLeft;
    private bool counting;
    private Quaternion handStartRotation;

    private void Start()
    {
        timeLeft = planningTime;
        counting = true;

        if (clockHandPivot != null)
        {
            handStartRotation =
                clockHandPivot.localRotation;
        }

        if (pocketWatch == null)
        {
            pocketWatch =
                FindFirstObjectByType<PocketWatch>();
        }

        UpdateClock();
    }

    private void Update()
    {
        if (!counting)
            return;

        timeLeft -= Time.deltaTime;
        timeLeft = Mathf.Max(0f, timeLeft);

        UpdateClock();

        if (timeLeft <= 0f)
        {
            counting = false;

            if (pocketWatch != null)
            {
                pocketWatch.PlayerDead();
            }
            else
            {
                Debug.LogError(
                    "noy found PocketWatch"
                );
            }
        }
    }

    private void UpdateClock()
    {
        int seconds = Mathf.CeilToInt(timeLeft);

        if (countdownText != null)
        {
            countdownText.text =
                planningLabel + seconds;

            countdownText.color =
                timeLeft <= 5f
                ? dangerColor
                : normalColor;
        }

        if (clockHandPivot != null)
        {
            float progress =
                1f - timeLeft / planningTime;

            Vector3 axis =
                handRotationAxis.sqrMagnitude > 0.001f
                ? handRotationAxis.normalized
                : Vector3.forward;

            clockHandPivot.localRotation =
                handStartRotation *
                Quaternion.AngleAxis(
                    -progress * 360f,
                    axis
                );
        }
    }

    public void PlayerLeftStartRoom()
    {
        if (!counting)
            return;

        counting = false;

        if (countdownText != null)
        {
            countdownText.text = leaveLabel;
            countdownText.color = Color.green;
        }
    }
}