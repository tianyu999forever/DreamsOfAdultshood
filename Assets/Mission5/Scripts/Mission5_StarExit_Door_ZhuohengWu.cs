using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Keeps the existing exit hinge locked until three stars, using the original F interaction.</summary>
public class Mission5_StarExit_Door_ZhuohengWu : MonoBehaviour
{
    [SerializeField] private Mission5_StarInventory_ZhuohengWu inventory;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private float openAngle = -60f;
    [SerializeField] private float openSpeed = 120f;
    private Collider nearbyPlayer;
    private Quaternion closedRotation, openRotation;
    public bool IsOpen { get; private set; }
    public bool IsFullyOpen => IsOpen && Quaternion.Angle(transform.localRotation, openRotation) < 2f;

    private void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        if (promptText != null) promptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        bool nearby = nearbyPlayer != null && nearbyPlayer.gameObject.activeInHierarchy;
        if (Time.timeScale > 0f && nearby && inventory != null && !inventory.Completed &&
            inventory.ExitUnlocked && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            IsOpen = !IsOpen;
        if (!Mission5_TimeFreeze_Environment_ZhuohengWu.IsFrozenIn(this))
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation,
            IsOpen ? openRotation : closedRotation, openSpeed * Time.deltaTime);
        if (promptText == null) return;
        promptText.gameObject.SetActive(nearby && inventory != null && !inventory.Completed && Time.timeScale > 0f);
        if (inventory != null)
            promptText.text = !inventory.ExitUnlocked
                ? "Collect at least 3 stars to unlock the exit"
                : IsOpen ? "Press F to Close Door" : "Press F to Open Door";
    }

    private bool IsPlayer(Collider other) => other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    private void OnTriggerEnter(Collider other) { if (IsPlayer(other)) nearbyPlayer = other; }
    private void OnTriggerStay(Collider other) { if (IsPlayer(other)) nearbyPlayer = other; }
    private void OnTriggerExit(Collider other) { if (other == nearbyPlayer) nearbyPlayer = null; }
}
