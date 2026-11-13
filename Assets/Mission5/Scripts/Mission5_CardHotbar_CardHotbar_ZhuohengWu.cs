using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// Selects held card slots and gives gravity exclusive priority over all other cards.
/// </summary>
[DefaultExecutionOrder(-2000)]
public class Mission5_CardHotbar_CardHotbar_ZhuohengWu : MonoBehaviour
{
    [Header("Slot Backgrounds (Left to Right)")]
    [SerializeField] private Image slot1Background;
    [SerializeField] private Image slot2Background;
    [SerializeField] private Image slot3Background;
    [SerializeField] private Image slot4Background;
    [SerializeField] private Image slot5Background;

    [Header("Highlight Colors")]
    [SerializeField] private Color normalColor = new Color32(40, 46, 58, 255);
    [SerializeField] private Color selectedColor = new Color32(184, 116, 24, 255);

    private Image[] slotBackgrounds;
    private readonly bool[] requiresFreshPress = new bool[5];

    // -1 means no selection; 0–4 correspond to the slots labeled 1–5.
    public int SelectedSlotIndex { get; private set; } = -1;
    public event System.Action<int> SelectionChanged;

    private void Awake()
    {
        slotBackgrounds = new[]
        {
            slot1Background, slot2Background, slot3Background, slot4Background, slot5Background
        };

        for (int i = 0; i < slotBackgrounds.Length; i++)
        {
            if (slotBackgrounds[i] == null)
            {
                Debug.LogError("Mission 5 Card Hotbar: assign all five Slot Background fields.", this);
                enabled = false;
                return;
            }

            for (int j = 0; j < i; j++)
            {
                if (slotBackgrounds[i] == slotBackgrounds[j])
                {
                    Debug.LogError("Mission 5 Card Hotbar: each slot needs a different Image.", this);
                    enabled = false;
                    return;
                }
            }
        }

        foreach (Image background in slotBackgrounds)
        {
            background.color = normalColor;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || Time.timeScale == 0f)
        {
            SelectSlot(-1);
            return;
        }

        // Gravity owns selection until 3 is released, including simultaneous presses.
        if (keyboard.digit3Key.isPressed)
        {
            requiresFreshPress[0] = requiresFreshPress[1] = requiresFreshPress[3] = requiresFreshPress[4] = true;
            SelectSlot(2);
            return;
        }

        // Evaluate every key, even lower-priority ones, so held keys cannot queue an ability.
        bool card1 = CanSelect(keyboard.digit1Key, 0);
        bool card2 = CanSelect(keyboard.digit2Key, 1);
        bool card4 = CanSelect(keyboard.digit4Key, 3);
        bool card5 = CanSelect(keyboard.digit5Key, 4);
        if (card1) SelectSlot(0);
        else if (card2) SelectSlot(1);
        else if (card4) SelectSlot(3);
        else if (card5) SelectSlot(4);
        else SelectSlot(-1);
    }

    private bool CanSelect(KeyControl key, int slot)
    {
        // A fresh press on the same frame as releasing 3 is valid; a continuing hold is not.
        if (!key.isPressed || key.wasPressedThisFrame) requiresFreshPress[slot] = false;
        return key.isPressed && !requiresFreshPress[slot];
    }

    public void SelectSlot(int index)
    {
        if (!isActiveAndEnabled || slotBackgrounds == null ||
            index < -1 || index >= slotBackgrounds.Length)
            return;

        // Keep invalid Inspector setup from causing a partial highlight update.
        foreach (Image background in slotBackgrounds)
        {
            if (background == null)
                return;
        }

        bool changed = SelectedSlotIndex != index;
        SelectedSlotIndex = index;

        for (int i = 0; i < slotBackgrounds.Length; i++)
        {
            slotBackgrounds[i].color = i == index ? selectedColor : normalColor;
        }
        // Abilities stop synchronously before gravity's later movement callbacks run.
        if (changed) SelectionChanged?.Invoke(index);
    }
}
