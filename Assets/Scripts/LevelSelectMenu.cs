using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectMenu : MonoBehaviour
{
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private GameObject deathPanel;

    // 仅在 Mission1 勾选；其他关卡不显示启动选关面板
    [SerializeField] private bool showLevelSelectAtStart = true;

    [SerializeField]
    private string[] sceneNames =
    {
        "Mission1",
        "Mission2_TianyuTang",
        "Mission3",
        "Mission4",
        "ZhuohengWu Mission5",
        "Mission6_Jizhijin"
    };

    private readonly List<Behaviour> pausedControls = new List<Behaviour>();
    private bool controlsArePaused;

    private void Start()
    {
        Time.timeScale = 1f;

        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(showLevelSelectAtStart);
    }

    private void Update()
    {
        bool deathIsOpen = deathPanel != null && deathPanel.activeInHierarchy;

        // 死亡面板一出现，同时显示选关按钮
        if (deathIsOpen && levelSelectPanel != null)
            levelSelectPanel.SetActive(true);

        bool menuIsOpen =
            (levelSelectPanel != null && levelSelectPanel.activeInHierarchy) ||
            deathIsOpen;

        if (menuIsOpen)
        {
            Time.timeScale = 0f;
            PausePlayerScripts();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (controlsArePaused)
        {
            ResumePlayerScripts();
            Time.timeScale = 1f;
            LockCursor();
        }
    }

    public void SelectLevel(int index)
    {
        if (sceneNames == null || index < 0 || index >= sceneNames.Length)
        {
            Debug.LogError("选关索引无效，请检查按钮 On Click 的索引。");
            return;
        }

        string targetScene = sceneNames[index].Trim();

        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(false);

        if (deathPanel != null)
            deathPanel.SetActive(false);

        ResumePlayerScripts();
        Time.timeScale = 1f;
        LockCursor();

        // 选当前关：关掉面板，直接开始
        if (SceneManager.GetActiveScene().name == targetScene)
            return;

        SceneManager.LoadScene(targetScene);
    }

    public void RetryCurrentScene()
    {
        Time.timeScale = 1f;
        ResumePlayerScripts();
        LockCursor();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void PausePlayerScripts()
    {
        if (controlsArePaused) return;

        MonoBehaviour[] scripts =
            FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (MonoBehaviour script in scripts)
        {
            if (script == null || !script.enabled) continue;

            string typeName = script.GetType().Name;

            if (typeName == "Moving" ||
                typeName == "MouseLook" ||
                typeName == "HeadBob")
            {
                script.enabled = false;
                pausedControls.Add(script);
            }
        }

        controlsArePaused = true;
    }

    private void ResumePlayerScripts()
    {
        foreach (Behaviour script in pausedControls)
        {
            if (script != null)
                script.enabled = true;
        }

        pausedControls.Clear();
        controlsArePaused = false;
    }

    private void LockCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}