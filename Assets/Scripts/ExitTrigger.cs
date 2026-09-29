using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    public GameObject exitPanel;

    public Moving moving;
    public MouseLook mouseLook;
    public HeadBob headBob;
    public PocketWatch pocketWatch;

    bool panelOpened;

    void Start()
    {
        Time.timeScale = 1f;

        // 没有手动连接时自动寻找
        if (moving == null)
            moving = FindFirstObjectByType<Moving>();

        if (mouseLook == null)
            mouseLook = FindFirstObjectByType<MouseLook>();

        if (headBob == null)
            headBob = FindFirstObjectByType<HeadBob>();

        if (pocketWatch == null)
            pocketWatch = FindFirstObjectByType<PocketWatch>();

        if (exitPanel != null)
            exitPanel.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        bool isPlayer =
            other.CompareTag("Player") ||
            other.transform.root.CompareTag("Player");

        if (!isPlayer || panelOpened)
            return;

        OpenPanel();
    }

    void OpenPanel()
    {
        panelOpened = true;
        exitPanel.SetActive(true);

        if (moving != null)
            moving.enabled = false;

        if (mouseLook != null)
            mouseLook.enabled = false;

        if (headBob != null)
            headBob.enabled = false;

        if (pocketWatch != null)
            pocketWatch.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 停止物理、动画和倒计时
        Time.timeScale = 0f;
    }

    public void ClosePanel()
    {
        Time.timeScale = 1f;

        panelOpened = false;
        exitPanel.SetActive(false);

        if (moving != null)
            moving.enabled = true;

        if (mouseLook != null)
            mouseLook.enabled = true;

        if (headBob != null)
            headBob.enabled = true;

        if (pocketWatch != null)
            pocketWatch.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}