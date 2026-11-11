using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Mission2_TimeScore_GameManager_TianyuTang : MonoBehaviour
{
    [Header("Existing Timer")]
    [SerializeField] private PocketWatch pocketWatch;
    [SerializeField] private PlanningTimer planningTimer;

    [Header("Result UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Mission2_PreparationTimer_PlanningClock_TianyuTang preparationTimer;

    [Header("Scoring")]
    [SerializeField, Min(1f)] private float escapeLimit = 50f;
    [SerializeField, Min(0f)] private float fullScoreTime = 20f;

    [Header("Player Controls")]
    [SerializeField] private Moving moving;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private HeadBob headBob;
    [Tooltip("Optional: flashlight controller and other gameplay scripts to stop.")]
    [SerializeField] private Behaviour[] additionalControls;

    private double startedAt;
    private bool started;
    private bool finished;

    private void Awake()
    {
        if (pocketWatch == null)
            pocketWatch = FindFirstObjectByType<PocketWatch>();
        if (planningTimer == null)
            planningTimer = FindFirstObjectByType<PlanningTimer>();
        if (preparationTimer == null)
            preparationTimer = FindFirstObjectByType<Mission2_PreparationTimer_PlanningClock_TianyuTang>();
        if (moving == null)
            moving = FindFirstObjectByType<Moving>();
        if (mouseLook == null)
            mouseLook = FindFirstObjectByType<MouseLook>();
        if (headBob == null)
            headBob = FindFirstObjectByType<HeadBob>();

        escapeLimit = Mathf.Max(1f, escapeLimit);
        fullScoreTime = Mathf.Clamp(fullScoreTime, 0f, escapeLimit - 0.01f);

        // Configure this scene's existing watch, not a second countdown.
        if (pocketWatch != null)
            pocketWatch.totalTime = escapeLimit;

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    public void BeginEscape()
    {
        if (started || finished || Time.timeScale <= 0f)
            return;

        if (pocketWatch == null || !pocketWatch.isActiveAndEnabled)
        {
            Debug.LogError("Assign an enabled PocketWatch.", this);
            return;
        }

        if (preparationTimer != null)
            preparationTimer.StopPlanning();

        if (planningTimer != null)
            planningTimer.PlayerLeftStartRoom();

        pocketWatch.StartCountdown();
        startedAt = Time.timeAsDouble;
        started = true;
    }

    public void FinishEscape()
    {
        // Do not award a result before departure or after the watch's death UI.
        if (!started || finished || Time.timeScale <= 0f)
            return;

        float elapsed = (float)(Time.timeAsDouble - startedAt);

        if (elapsed >= escapeLimit)
        {
            finished = true;
            pocketWatch.PlayerDead();
            return;
        }

        if (resultPanel == null || resultText == null)
        {
            Debug.LogError("Assign Result Panel and Result Text.", this);
            return;
        }

        finished = true;
        int score = CalculateScore(elapsed, escapeLimit, fullScoreTime);
        resultPanel.SetActive(true);
        resultText.text = $"ESCAPED!\nTIME: {elapsed:0.0}s\nSCORE: {score} / 100";

        if (moving != null) moving.enabled = false;
        if (mouseLook != null) mouseLook.enabled = false;
        if (headBob != null) headBob.enabled = false;
        if (pocketWatch != null) pocketWatch.enabled = false;

        if (additionalControls != null)
            foreach (Behaviour control in additionalControls)
                if (control != null && control != this)
                    control.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public static int CalculateScore(float elapsed, float limit, float perfectTime)
    {
        float denominator = Mathf.Max(0.01f, limit - perfectTime);
        return Mathf.RoundToInt(
            Mathf.Clamp01((limit - elapsed) / denominator) * 100f);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }
}
