using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class PocketWatch : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text timerText;
    public TMP_Text hintText;
    public GameObject deathPanel;

    [Header("Watch Visual")]
    // 这里只能放 WatchVisuals 子物体，不能放 WatchHolder 自己
    public GameObject watchVisuals;
    public float fullyHiddenDistance = 0.01f;

    [Header("Timer")]
    public float totalTime = 60f;
    public float hintDuration = 2f;
    public float hintFadeDuration = 0.5f;

    [Header("Watch Animation")]
    public float animationSpeed = 8f;

    public Vector3 hiddenPosition =
        new Vector3(0.35f, -0.55f, 0.75f);

    public Vector3 shownPosition =
        new Vector3(0.15f, -0.18f, 0.6f);

    public Vector3 hiddenRotation =
        new Vector3(30f, -20f, 15f);

    public Vector3 shownRotation = Vector3.zero;

    [Header("Player Scripts")]
    public Moving moving;
    public MouseLook mouseLook;
    public HeadBob headBob;

    float timeLeft;

    bool showing;
    bool started;
    bool finished;

    void Start()
    {
        Time.timeScale = 1f;

        timeLeft = totalTime;
        showing = false;
        started = false;
        finished = false;

        // 开始时把怀表放到隐藏位置
        transform.localPosition = hiddenPosition;
        transform.localRotation =
            Quaternion.Euler(hiddenRotation);

        // 游戏开始时完全关闭手和怀表的画面
        SetWatchVisuals(false);

        if (hintText != null)
            hintText.gameObject.SetActive(false);

        if (deathPanel != null)
            deathPanel.SetActive(false);

        if (moving == null)
            moving = FindFirstObjectByType<Moving>();

        if (mouseLook == null)
            mouseLook = FindFirstObjectByType<MouseLook>();

        if (headBob == null)
            headBob = FindFirstObjectByType<HeadBob>();

        UpdateText();
    }

    void Update()
    {
        // 时间开始前和开始后都可以按 E 查看怀表
        if (!finished &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            showing = !showing;

            // 掏出怀表前，先显示手和怀表模型
            if (showing)
            {
                SetWatchVisuals(true);
            }
        }

        MoveWatch();

        if (!started || finished)
            return;

        timeLeft -= Time.deltaTime;

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            UpdateText();
            PlayerDead();
            return;
        }

        UpdateText();
    }

    void MoveWatch()
    {
        Vector3 targetPosition =
            showing ? shownPosition : hiddenPosition;

        Vector3 targetRotation =
            showing ? shownRotation : hiddenRotation;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            animationSpeed * Time.deltaTime
        );

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            Quaternion.Euler(targetRotation),
            animationSpeed * Time.deltaTime
        );

        // 怀表完全收回以后，停止渲染手和怀表
        if (!showing &&
            Vector3.Distance(
                transform.localPosition,
                hiddenPosition) <= fullyHiddenDistance)
        {
            transform.localPosition = hiddenPosition;
            transform.localRotation =
                Quaternion.Euler(hiddenRotation);

            SetWatchVisuals(false);
        }
    }

    void SetWatchVisuals(bool visible)
    {
        if (watchVisuals == null)
            return;

        // 防止误把 WatchHolder 自己拖进来
        if (watchVisuals == gameObject)
        {
            Debug.LogError(
                "Watch Visuals 不能设置为 WatchHolder 自己！" +
                "请创建一个 WatchVisuals 子物体。"
            );

            return;
        }

        watchVisuals.SetActive(visible);
    }

    // 由门口的 StartTimerTrigger 调用
    public void StartCountdown()
    {
        if (started || finished)
            return;

        started = true;
        showing = true;
        timeLeft = totalTime;

        // 出门后自动显示怀表
        SetWatchVisuals(true);

        UpdateText();
        StartCoroutine(ShowHint());
    }

    IEnumerator ShowHint()
    {
        if (hintText == null)
            yield break;

        Color color = hintText.color;
        float maxAlpha = color.a;
        float fadeTime = Mathf.Max(
            0.01f,
            hintFadeDuration
        );

        // 从完全透明开始
        color.a = 0f;
        hintText.color = color;
        hintText.gameObject.SetActive(true);

        // 淡入
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.unscaledDeltaTime;

            color.a = Mathf.Lerp(
                0f,
                maxAlpha,
                timer / fadeTime
            );

            hintText.color = color;

            yield return null;
        }

        color.a = maxAlpha;
        hintText.color = color;

        // 保持显示
        yield return new WaitForSecondsRealtime(
            hintDuration
        );

        // 淡出
        timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.unscaledDeltaTime;

            color.a = Mathf.Lerp(
                maxAlpha,
                0f,
                timer / fadeTime
            );

            hintText.color = color;

            yield return null;
        }

        hintText.gameObject.SetActive(false);

        // 恢复原来的透明度
        color.a = maxAlpha;
        hintText.color = color;
    }

    void UpdateText()
    {
        if (timerText == null)
            return;

        int totalSeconds =
            Mathf.CeilToInt(timeLeft);

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text =
            minutes.ToString("00") + ":" +
            seconds.ToString("00");
    }

    public void PlayerDead()
    {
        finished = true;
        showing = false;

        Debug.Log("Time is up!");

        // 死亡时隐藏手和怀表
        SetWatchVisuals(false);

        if (deathPanel != null)
            deathPanel.SetActive(true);

        if (moving != null)
            moving.enabled = false;

        if (mouseLook != null)
            mouseLook.enabled = false;

        if (headBob != null)
            headBob.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}