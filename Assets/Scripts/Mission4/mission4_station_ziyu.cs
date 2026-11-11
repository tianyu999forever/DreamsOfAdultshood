using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum M4StationType { Bun, Lettuce, Tomato, Patty, Board, Stove, Window }

// 取菜点 / 取肉点 / 取汉堡胚点 / 案板 / 灶台 / 出口窗口，都用这个脚本
public class Mission4_Station_ziyu : MonoBehaviour
{
    public static readonly List<Mission4_Station_ziyu> All = new List<Mission4_Station_ziyu>();

    public M4StationType type;
    public Transform itemSpot;          // 案板/灶台上放食物的位置
    public TMP_Text statusLabel;        // 头顶的文字（可选）
    public Material foodMaterial;       // 食物外观用的基础材质

    [Header("Timing (seconds)")]
    public float cutTimePatty = 5f;
    public float cutTimeVeg = 3f;
    public float cookTime = 10f;

    [Header("Exit window only")]
    public Mission4_Door_ziyu exitDoor;

    bool hasItem;
    M4Item slotItem;
    float progress;
    GameObject visual;
    Mission4_Game_ziyu game;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        game = FindFirstObjectByType<Mission4_Game_ziyu>();
        UpdateLabel();
    }

    void Update()
    {
        if (type == M4StationType.Stove && hasItem &&
            slotItem.state == M4State.Cut && game != null && game.MazeRunning)
        {
            progress += Time.deltaTime;
            if (progress >= cookTime)
            {
                slotItem.state = M4State.Cooked;
                progress = 0f;
                RefreshVisual();
                Toast("The patty is fried! Click the stove to take it.");
            }
        }

        UpdateLabel();
    }

    // ---------- 查询 ----------

    public bool IsSource
    {
        get
        {
            return type == M4StationType.Bun || type == M4StationType.Lettuce ||
                   type == M4StationType.Tomato || type == M4StationType.Patty;
        }
    }

    M4Food SourceFood
    {
        get
        {
            switch (type)
            {
                case M4StationType.Bun: return M4Food.Bun;
                case M4StationType.Lettuce: return M4Food.Lettuce;
                case M4StationType.Tomato: return M4Food.Tomato;
                default: return M4Food.Patty;
            }
        }
    }

    public bool IsCuttingNow
    {
        get { return type == M4StationType.Board && hasItem && slotItem.state == M4State.Raw; }
    }

    float CutNeeded
    {
        get { return slotItem.food == M4Food.Patty ? cutTimePatty : cutTimeVeg; }
    }

    public string CutProgressText()
    {
        return "Hold [C] to cut: " + progress.ToString("0.0") + " / " +
               CutNeeded.ToString("0.0") + " s";
    }

    public string GetPrompt(Mission4_Bag_ziyu bag)
    {
        if (IsSource)
        {
            return bag.Has(SourceFood)
                ? "You already have " + SourceFood
                : "[Click] Take " + SourceFood;
        }

        switch (type)
        {
            case M4StationType.Board:
                if (!hasItem) return "Cutting board  -  Q, then 1-4 to place an item";
                if (slotItem.state == M4State.Raw) return CutProgressText() + "   ([Click] to take it back)";
                return "[Click] Take " + slotItem.DisplayName;

            case M4StationType.Stove:
                if (!hasItem) return "Stove  -  Q, then 1-4 to place a cut patty";
                if (slotItem.state == M4State.Cut)
                    return "Frying... " + Mathf.CeilToInt(cookTime - progress) + " s   ([Click] to take it back)";
                return "[Click] Take " + slotItem.DisplayName;

            default:
                return "[Click] Hand in the burger ingredients";
        }
    }

    // ---------- 交互 ----------

    public void OnClick(Mission4_Bag_ziyu bag, Mission4_Game_ziyu g)
    {
        game = g;

        if (IsSource)
        {
            M4Food f = SourceFood;
            if (bag.Has(f)) { Toast("You already have a " + f + "."); return; }
            if (!bag.TryAdd(new M4Item(f, M4State.Raw))) { Toast("Your bag is full."); return; }
            Toast("Picked up: " + f + "   (bag " + bag.Count + "/" + bag.capacity + ")");
            return;
        }

        if (type == M4StationType.Window)
        {
            Deliver(bag);
            return;
        }

        // 案板 / 灶台：取回物品
        if (!hasItem)
        {
            Toast("Open the bag (Q) and press 1-4 to place an item here.");
            return;
        }

        M4Item item = slotItem;
        if (!bag.TryAdd(item)) { Toast("Your bag cannot hold that."); return; }

        ClearSlot();
        Toast("Took " + item.DisplayName + ".");
    }

    public void TryPlace(int bagIndex, Mission4_Bag_ziyu bag)
    {
        M4Item item;
        if (!bag.TryGet(bagIndex, out item))
        {
            Toast("Slot " + (bagIndex + 1) + " is empty.");
            return;
        }

        if (hasItem)
        {
            Toast("Already busy. Take the item off first (click).");
            return;
        }

        if (type == M4StationType.Board)
        {
            if (item.food == M4Food.Bun || item.state != M4State.Raw)
            {
                Toast("The board is for raw lettuce, tomato or patty only.");
                return;
            }
        }
        else if (type == M4StationType.Stove)
        {
            if (item.food != M4Food.Patty)
            {
                Toast("Only patty can be fried.");
                return;
            }
            if (item.state == M4State.Raw)
            {
                Toast("Cut the patty on the board first.");
                return;
            }
            if (item.state == M4State.Cooked)
            {
                Toast("That patty is already fried.");
                return;
            }
        }
        else
        {
            return;
        }

        bag.RemoveAt(bagIndex);
        slotItem = item;
        hasItem = true;
        progress = 0f;
        RefreshVisual();

        Toast(type == M4StationType.Board
            ? "Hold [C] near the board to cut."
            : "Frying for " + cookTime.ToString("0") + " s...");
    }

    // 案板：玩家按住 C 时每帧调用，累计时间
    public void Cut(float dt)
    {
        if (!IsCuttingNow) return;

        progress += dt;
        if (progress >= CutNeeded)
        {
            slotItem.state = M4State.Cut;
            progress = 0f;
            RefreshVisual();
            Toast("Done! Click the board to take the " + slotItem.DisplayName + ".");
        }
    }

    void Deliver(Mission4_Bag_ziyu bag)
    {
        List<string> missing = new List<string>();
        if (!bag.Has(M4Food.Bun)) missing.Add("Burger Bun");
        if (!bag.Has(M4Food.Lettuce, M4State.Cut)) missing.Add("Cut Lettuce");
        if (!bag.Has(M4Food.Tomato, M4State.Cut)) missing.Add("Cut Tomato");
        if (!bag.Has(M4Food.Patty, M4State.Cooked)) missing.Add("Fried Patty");

        if (missing.Count > 0)
        {
            Toast("Missing: " + string.Join(", ", missing));
            return;
        }

        bag.Clear();
        if (exitDoor != null) exitDoor.Open();
        if (game != null) game.Win();
    }

    // ---------- 显示 ----------

    void ClearSlot()
    {
        hasItem = false;
        progress = 0f;
        if (visual != null) Destroy(visual);
        visual = null;
    }

    void RefreshVisual()
    {
        if (visual != null) Destroy(visual);

        Transform spot = itemSpot != null ? itemSpot : transform;
        visual = Mission4_Visual_ziyu.Make(slotItem, foodMaterial);
        visual.transform.SetParent(spot, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
    }

    void UpdateLabel()
    {
        if (statusLabel == null) return;

        switch (type)
        {
            case M4StationType.Board:
                if (!hasItem) statusLabel.text = "CUTTING BOARD";
                else if (slotItem.state == M4State.Raw)
                    statusLabel.text = "CUTTING  " + progress.ToString("0.0") + " / " + CutNeeded.ToString("0") + "s";
                else statusLabel.text = "READY - CLICK TO TAKE";
                break;

            case M4StationType.Stove:
                if (!hasItem) statusLabel.text = "STOVE";
                else if (slotItem.state == M4State.Cut)
                    statusLabel.text = "FRYING  " + Mathf.CeilToInt(cookTime - progress) + "s";
                else statusLabel.text = "READY - CLICK TO TAKE";
                break;
        }
    }

    void Toast(string msg)
    {
        if (game == null) game = FindFirstObjectByType<Mission4_Game_ziyu>();
        if (game != null) game.Toast(msg);
    }
}
