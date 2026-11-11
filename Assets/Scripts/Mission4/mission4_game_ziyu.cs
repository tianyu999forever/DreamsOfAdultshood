using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Mission 4 总管理：观察阶段(60s) -> 迷宫阶段(180s) -> 通关 / 失败
public class Mission4_Game_ziyu : MonoBehaviour
{
    public enum Phase { Observe, Maze, Won, Lost }

    [Header("Times (seconds)")]
    public float observeTime = 60f;
    public float mazeTime = 180f;

    [Header("Score (finish within X seconds of entering the maze)")]
    public float time100 = 120f;
    public float time80 = 150f;
    public float time60 = 180f;

    [Header("HUD")]
    public TMP_Text timerText;      // 左上角倒计时
    public TMP_Text orderText;      // 右上角订单清单
    public TMP_Text toastText;      // 屏幕中间的短提示
    public float toastTime = 2.5f;

    [Header("Start-room clock (like Mission 1)")]
    public TMP_Text clockText;
    public Transform clockHandPivot;
    public Color normalColor = Color.white;
    public Color dangerColor = new Color(1f, 0.15f, 0.05f);

    [Header("Map (R key)")]
    public GameObject mapPanel;
    public RectTransform mapImageRect;
    public RectTransform mapPlayerDot;
    public int gridWidth = 31;
    public int gridHeight = 23;
    public float cellSize = 3f;

    [Header("Result panel")]
    public GameObject resultPanel;
    public TMP_Text resultText;

    [Header("References")]
    public Mission4_Bag_ziyu bag;
    public Transform player;
    public Moving moving;
    public MouseLook mouseLook;
    public HeadBob headBob;

    Phase phase = Phase.Observe;
    float observeLeft;
    float mazeLeft;
    float toastLeft;
    Quaternion handStartRotation = Quaternion.identity;

    public bool MazeRunning { get { return phase == Phase.Maze; } }
    public bool Ended { get { return phase == Phase.Won || phase == Phase.Lost; } }

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        observeLeft = observeTime;
        mazeLeft = mazeTime;

        if (clockHandPivot != null)
            handStartRotation = clockHandPivot.localRotation;

        if (moving == null) moving = FindFirstObjectByType<Moving>();
        if (mouseLook == null) mouseLook = FindFirstObjectByType<MouseLook>();
        if (headBob == null) headBob = FindFirstObjectByType<HeadBob>();
        if (player == null && moving != null) player = moving.transform;

        if (mapPanel != null) mapPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (toastText != null) toastText.text = "";

        UpdateTimerUI();
        UpdateOrderUI();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;

        if (Ended)
        {
            if (kb != null &&
                (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                Restart();
            return;
        }

        // R：看地图
        if (kb != null && kb.rKey.wasPressedThisFrame && mapPanel != null)
            mapPanel.SetActive(!mapPanel.activeSelf);

        UpdateMapDot();

        if (phase == Phase.Observe)
        {
            observeLeft -= Time.deltaTime;
            if (observeLeft <= 0f)
            {
                observeLeft = 0f;
                Toast("Observation time is over - go!");
                StartMaze();
            }
        }
        else if (phase == Phase.Maze)
        {
            mazeLeft -= Time.deltaTime;
            if (mazeLeft <= 0f)
            {
                mazeLeft = 0f;
                Lose("Time is up! You did not hand in a complete burger.");
            }
        }

        // 提示文字淡出
        if (toastLeft > 0f)
        {
            toastLeft -= Time.deltaTime;
            if (toastLeft <= 0f && toastText != null) toastText.text = "";
        }

        UpdateTimerUI();
        UpdateOrderUI();
    }

    // ---------- 流程 ----------

    public void StartMaze()
    {
        if (phase != Phase.Observe) return;

        phase = Phase.Maze;
        mazeLeft = mazeTime;

        if (clockText != null)
        {
            clockText.text = "GO!";
            clockText.color = Color.green;
        }

        Toast("GO! Make a burger and hand it in at the exit window.");
    }

    public void Win()
    {
        if (Ended) return;

        float used = mazeTime - mazeLeft;
        int score = ScoreFor(used);

        phase = Phase.Won;
        FreezePlayer();

        ShowResult(
            "BURGER DONE!\n\n" +
            "Time: " + FormatTime(used) + "\n" +
            "Score: " + score + "\n\n" +
            "Press ENTER to play again");
    }

    public void Lose(string reason)
    {
        if (Ended) return;

        phase = Phase.Lost;
        FreezePlayer();

        ShowResult(
            "YOU FAILED\n\n" + reason + "\n\n" +
            "Press ENTER to try again");
    }

    int ScoreFor(float seconds)
    {
        if (seconds <= time100) return 100;
        if (seconds <= time80) return 80;
        return 60;
    }

    void FreezePlayer()
    {
        if (moving != null)
        {
            Rigidbody rb = moving.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
            moving.enabled = false;
        }
        if (mouseLook != null) mouseLook.enabled = false;
        if (headBob != null) headBob.enabled = false;

        if (bag != null) bag.SetOpen(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (toastText != null) toastText.text = "";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ShowResult(string text)
    {
        if (resultText != null) resultText.text = text;
        if (resultPanel != null) resultPanel.SetActive(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ---------- UI ----------

    public void Toast(string msg)
    {
        if (toastText == null) return;
        toastText.text = msg;
        toastLeft = toastTime;
    }

    static string FormatTime(float seconds)
    {
        int s = Mathf.CeilToInt(seconds);
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    void UpdateTimerUI()
    {
        if (phase == Phase.Observe)
        {
            int sec = Mathf.CeilToInt(observeLeft);
            bool danger = observeLeft <= 10f;

            if (timerText != null)
            {
                timerText.text = "OBSERVE  " + FormatTime(observeLeft);
                timerText.color = danger ? dangerColor : normalColor;
            }

            if (clockText != null)
            {
                clockText.text = "OBSERVE: " + sec;
                clockText.color = observeLeft <= 5f ? dangerColor : normalColor;
            }

            if (clockHandPivot != null)
            {
                float progress = 1f - observeLeft / Mathf.Max(0.01f, observeTime);
                clockHandPivot.localRotation =
                    handStartRotation * Quaternion.AngleAxis(-progress * 360f, Vector3.forward);
            }
        }
        else if (phase == Phase.Maze)
        {
            if (timerText != null)
            {
                timerText.text = FormatTime(mazeLeft);
                timerText.color = mazeLeft <= 30f ? dangerColor : normalColor;
            }
        }
    }

    void UpdateOrderUI()
    {
        if (orderText == null || bag == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("ORDER: 1 BURGER");
        sb.AppendLine(Line(bag.Has(M4Food.Bun), "Burger Bun"));
        sb.AppendLine(Line(bag.Has(M4Food.Lettuce, M4State.Cut), "Lettuce (cut)"));
        sb.AppendLine(Line(bag.Has(M4Food.Tomato, M4State.Cut), "Tomato (cut)"));
        sb.AppendLine(Line(bag.Has(M4Food.Patty, M4State.Cooked), "Patty (cut + fried)"));
        sb.AppendLine();
        sb.Append("In bag: " + bag.Summary());
        orderText.text = sb.ToString();
    }

    static string Line(bool done, string name)
    {
        return (done ? "[x] " : "[  ] ") + name;
    }

    void UpdateMapDot()
    {
        if (mapPanel == null || !mapPanel.activeSelf ||
            mapPlayerDot == null || mapImageRect == null || player == null)
            return;

        float u = (player.position.x / cellSize + 0.5f) / gridWidth;
        float v = (player.position.z / cellSize + 0.5f) / gridHeight;

        Rect r = mapImageRect.rect;
        // 小红点是地图图片的子物体，位置按图片中心计算
        mapPlayerDot.anchoredPosition =
            new Vector2((u - 0.5f) * r.width, (v - 0.5f) * r.height);
    }
}
