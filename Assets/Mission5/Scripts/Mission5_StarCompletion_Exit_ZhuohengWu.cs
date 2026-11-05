using TMPro;
using UnityEngine;

/// <summary>Completes only after the unlocked door is opened and the player crosses the existing exit.</summary>
public class Mission5_StarCompletion_Exit_ZhuohengWu : MonoBehaviour
{
    [SerializeField] private Mission5_StarInventory_ZhuohengWu inventory;
    [SerializeField] private Mission5_StarExit_Door_ZhuohengWu exitDoor;
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Moving moving;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private HeadBob headBob;
    [SerializeField] private PocketWatch pocketWatch;

    private void Start() { if (completionPanel != null) completionPanel.SetActive(false); }
    private void OnTriggerEnter(Collider other) => TryFinish(other);
    private void OnTriggerStay(Collider other) => TryFinish(other);
    private void TryFinish(Collider other)
    {
        if (!(other.CompareTag("Player") || other.transform.root.CompareTag("Player")) ||
            inventory == null || inventory.Completed || exitDoor == null || !exitDoor.IsFullyOpen ||
            Time.timeScale <= 0f || completionPanel == null || resultText == null || !inventory.TryComplete()) return;
        resultText.text = "MISSION 5 COMPLETE\n\nSTARS COLLECTED: " + inventory.StarCount + "\nFINAL SCORE: " + inventory.Score;
        completionPanel.SetActive(true);
        if (moving != null) moving.enabled = false;
        if (mouseLook != null) mouseLook.enabled = false;
        if (headBob != null) headBob.enabled = false;
        if (pocketWatch != null) pocketWatch.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }
}
