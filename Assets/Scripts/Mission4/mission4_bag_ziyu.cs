using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 背包：容量 4，每种食材最多一个，按 Q 打开
public class Mission4_Bag_ziyu : MonoBehaviour
{
    public int capacity = 4;
    public GameObject bagPanel;
    public TMP_Text bagText;
    public Mission4_Game_ziyu game;

    readonly List<M4Item> items = new List<M4Item>();
    bool open;

    public bool IsOpen { get { return open; } }
    public int Count { get { return items.Count; } }

    void Start()
    {
        SetOpen(false);
        Refresh();
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (game != null && game.Ended) { SetOpen(false); return; }

        if (Keyboard.current.qKey.wasPressedThisFrame)
            SetOpen(!open);
    }

    public void SetOpen(bool value)
    {
        open = value;
        if (bagPanel != null) bagPanel.SetActive(open);
        if (open) Refresh();
    }

    public bool Has(M4Food food)
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i].food == food) return true;
        return false;
    }

    public bool Has(M4Food food, M4State state)
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i].food == food && items[i].state == state) return true;
        return false;
    }

    public bool TryAdd(M4Item item)
    {
        if (items.Count >= capacity) return false;
        if (Has(item.food)) return false;
        items.Add(item);
        Refresh();
        return true;
    }

    public bool TryGet(int index, out M4Item item)
    {
        if (index < 0 || index >= items.Count)
        {
            item = default(M4Item);
            return false;
        }
        item = items[index];
        return true;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= items.Count) return;
        items.RemoveAt(index);
        Refresh();
    }

    public void Clear()
    {
        items.Clear();
        Refresh();
    }

    public string Summary()
    {
        if (items.Count == 0) return "(empty)";
        List<string> names = new List<string>();
        for (int i = 0; i < items.Count; i++) names.Add(items[i].DisplayName);
        return string.Join(", ", names);
    }

    public void Refresh()
    {
        if (bagText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("BAG  " + items.Count + "/" + capacity + "   (Q to close)");
        sb.AppendLine();
        for (int i = 0; i < capacity; i++)
        {
            sb.Append("[" + (i + 1) + "] ");
            sb.AppendLine(i < items.Count ? items[i].DisplayName : "--");
        }
        sb.AppendLine();
        sb.Append("Press 1-4 near the board / stove\nto put that item down.");
        bagText.text = sb.ToString();
    }
}
