using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Mission2_EscapeScore_GameManager_TianyuTang : MonoBehaviour
{
    [Header("Score Settings")]
    public float escapeLimit = 50f;
    public float fullScoreTime = 20f;

    [Header("Result UI")]
    public GameObject resultPanel;
    public TMP_Text resultText;

    [Header("Player Scripts")]
    public Moving moving;
    public MouseLook mouseLook;
    public HeadBob headBob;
    public PocketWatch pocketWatch;

    private float escapeTime;
    private bool escapeStarted;
    private bool escaped;

    void Start()
    {
        Time.timeScale = 1f;

        if (moving == null) moving = FindFirstObjectByType<Moving>();
        if (mouseLook == null) mouseLook = FindFirstObjectByType<MouseLook>();
        if (headBob == null) headBob = FindFirstObjectByType<HeadBob>();
        if (pocketWatch == null) pocketWatch = FindFirstObjectByType<PocketWatch>();

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    void Update()
    {
        if (escapeStarted && !escaped)
            escapeTime += Time.deltaTime;
    }

    // 玩家离开起始房间时调用
    public void StartEscape()
    {
        if (escapeStarted || escaped)
            return;

        escapeStarted = true;
        escapeTime = 0f;
    }

    // 玩家真正走出出口时调用
    public void FinishEscape()
    {
        if (!escapeStarted || escaped)
            return;

        escaped = true;

        float scorePercent = Mathf.Clamp01(
            (escapeLimit - escapeTime) / (escapeLimit - fullScoreTime)
        );

        int score = Mathf.RoundToInt(scorePercent * 100);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
            resultText.text =
                "YOU ESCAPED!\n\n" +
                "TIME: " + escapeTime.ToString("0.0") + "s\n" +
                "SCORE: " + score + " / 100";

        if (moving != null) moving.enabled = false;
        if (mouseLook != null) mouseLook.enabled = false;
        if (headBob != null) headBob.enabled = false;
        if (pocketWatch != null) pocketWatch.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}