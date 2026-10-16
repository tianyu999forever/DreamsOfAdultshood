using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Selects one of four manually configured UI slots. Does not use card effects.
/// </summary>
public class Mission5_CardHotbar_CardHotbar_ZhuohengWu : MonoBehaviour
{
    [Header("Slot Backgrounds (Left to Right)")]
    [SerializeField] private Image slot1Background;
    [SerializeField] private Image slot2Background;
    [SerializeField] private Image slot3Background;
    [SerializeField] private Image slot4Background;

    [Header("Highlight Colors")]
    [SerializeField] private Color normalColor = new Color32(40, 46, 58, 255);
    [SerializeField] private Color selectedColor = new Color32(184, 116, 24, 255);

    private Image[] slotBackgrounds;

    // -1 means no selection; 0–3 correspond to the slots labeled 1–4.
    public int SelectedSlotIndex { get; private set; } = -1;

    private void Awake()
    {
        slotBackgrounds = new[]
        {
            slot1Background, slot2Background, slot3Background, slot4Background
        };

        for (int i = 0; i < slotBackgrounds.Length; i++)
        {
            if (slotBackgrounds[i] == null)
            {
                Debug.LogError("Mission 5 Card Hotbar: assign all four Slot Background fields.", this);
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

        // Held keys take priority in order: 1, 2, 3, 4.
        if (keyboard.digit1Key.isPressed) SelectSlot(0);
        else if (keyboard.digit2Key.isPressed) SelectSlot(1);
        else if (keyboard.digit3Key.isPressed) SelectSlot(2);
        else if (keyboard.digit4Key.isPressed) SelectSlot(3);
        else SelectSlot(-1);
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

        SelectedSlotIndex = index;

        for (int i = 0; i < slotBackgrounds.Length; i++)
        {
            slotBackgrounds[i].color = i == index ? selectedColor : normalColor;
        }
    }
}
