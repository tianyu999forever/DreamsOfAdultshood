using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DoorController : MonoBehaviour
{
    public float openAngle = 90f;
    public float openSpeed = 120f;

    public TMP_Text promptText;

    bool playerNearby;
    bool isOpen;

    Quaternion closedRotation;
    Quaternion openRotation;

    void Start()
    {
        closedRotation = transform.localRotation;

        openRotation =
            closedRotation *
            Quaternion.Euler(0f, openAngle, 0f);

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (playerNearby &&
            Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            isOpen = !isOpen;
            UpdatePrompt();
        }

        Quaternion targetRotation =
            isOpen ? openRotation : closedRotation;

        transform.localRotation =
            Quaternion.RotateTowards(
                transform.localRotation,
                targetRotation,
                openSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerNearby = true;

        if (promptText != null)
        {
            UpdatePrompt();
            promptText.gameObject.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerNearby = false;

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void UpdatePrompt()
    {
        if (promptText == null)
            return;

        promptText.text =
            isOpen
            ? "F  CLOSE DOOR"
            : "F  OPEN DOOR";
    }

    bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") ||
               other.transform.root.CompareTag("Player");
    }
}