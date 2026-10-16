using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public sealed class Mission6_WordClue_Jizhijin
{
    public readonly string word, mask, hint;
    public readonly int x, row, number;
    public readonly bool down;
    public Mission6_WordClue_Jizhijin(string word, string mask, int x, int row, int number, bool down, string hint)
    { this.word = word; this.mask = mask; this.x = x; this.row = row; this.number = number; this.down = down; this.hint = hint; }
    public Vector2Int CellAt(int index) => new Vector2Int(x + (down ? 0 : index), row - (down ? index : 0));
}
public sealed class Mission6_CrosswordCell_Jizhijin
{
    public char answer;
    public bool missing;
    public int number;
    public readonly List<int> words = new List<int>();
}

[DefaultExecutionOrder(100)]
public class Mission6_Grid_Level_Jizhijin : MonoBehaviour
{
    public const int Size = 20;
    public const float CellSize = 2.4f;
    public static readonly Vector2Int StartCell = new Vector2Int(1, 1);
    public static readonly Vector2Int ExitCell = new Vector2Int(18, 18);
    // The connected shape follows the user's reference. Down advances toward smaller map rows.
    public static readonly Mission6_WordClue_Jizhijin[] Words =
    {
        new Mission6_WordClue_Jizhijin("COME", "C_M_", 9, 17, 1, true, "Move toward me"),
        new Mission6_WordClue_Jizhijin("FROM", "F_OM", 11, 17, 2, true, "A starting point"),
        new Mission6_WordClue_Jizhijin("YOUR", "Y_U_", 8, 16, 3, false, "Belonging to you"),
        new Mission6_WordClue_Jizhijin("MAKE", "M_KE", 7, 15, 4, true, "Create something"),
        new Mission6_WordClue_Jizhijin("OF", "OF", 11, 15, 5, false, "A part __ a whole"),
        new Mission6_WordClue_Jizhijin("HAVE", "H_V_", 6, 14, 6, false, "To own"),
        new Mission6_WordClue_Jizhijin("HER", "HER", 6, 12, 7, false, "Belonging to a woman"),
        new Mission6_WordClue_Jizhijin("HE", "HE", 6, 12, 7, true, "A male person")
    };
    public static readonly Dictionary<Vector2Int, Mission6_CrosswordCell_Jizhijin> Crossword = CreateCrossword();
    public static readonly Vector2Int[] BlankCells = Crossword.Where(p => p.Value.missing).OrderByDescending(p => p.Key.y).ThenBy(p => p.Key.x).Select(p => p.Key).ToArray();
    public static readonly Vector2Int[] LetterCells =
    {
        new Vector2Int(9, 2), new Vector2Int(13, 3), new Vector2Int(18, 6), new Vector2Int(12, 9),
        new Vector2Int(8, 10), new Vector2Int(4, 14), new Vector2Int(14, 15), new Vector2Int(9, 18),
        new Vector2Int(17, 18)
    };
    public const string PickupLetters = "KARFEOHUV";
    public float planningDuration = 30;
    public GameObject planPanel;
    public GameObject explorationBag, photoHint;
    public TMP_Text planCountdown, objectiveText;
    public Moving moving;
    public MouseLook mouseLook;
    public HeadBob headBob;
    public PocketWatch watch;
    public bool IsPlanning { get; private set; } = true;
    public float PlanTimeLeft { get; private set; }

    static Dictionary<Vector2Int, Mission6_CrosswordCell_Jizhijin> CreateCrossword()
    {
        var cells = new Dictionary<Vector2Int, Mission6_CrosswordCell_Jizhijin>();
        for (int w = 0; w < Words.Length; w++)
            for (int i = 0; i < Words[w].word.Length; i++)
            {
                var clue = Words[w]; var p = clue.CellAt(i);
                if (!cells.TryGetValue(p, out var cell)) cells[p] = cell = new Mission6_CrosswordCell_Jizhijin { answer = clue.word[i] };
                if (cell.answer != clue.word[i]) throw new InvalidOperationException("Conflicting crossword intersection at " + p);
                cell.missing |= clue.mask[i] == '_'; cell.words.Add(w);
                if (i == 0) cell.number = clue.number;
            }
        return cells;
    }
    public static bool IsWall(int x, int row)
    {
        if (x < 0 || row < 0 || x >= Size || row >= Size) return true;
        if (Crossword.ContainsKey(new Vector2Int(x, row))) return false;
        if (x == 4 && row < 4 && row != 1) return true;
        if (row == 4 || row == 12) return x < 6 || x > 8;
        if (row == 8 || row == 16) return x < 11 || x > 13;
        return false;
    }
    public static float Height(int x, int row)
    {
        if (row >= 9 && row <= 11 && x >= 15 && x <= 17) return 0.36f;
        if (row >= 13 && row <= 15 && x >= 2 && x <= 4) return 0.36f;
        return 0;
    }
    public static int RampDirection(int x, int row)
    {
        if (row >= 9 && row <= 11) { if (x == 14) return 1; if (x == 18) return -1; }
        if (row >= 13 && row <= 15) { if (x == 1) return 1; if (x == 5) return -1; }
        return 0;
    }
    public static Vector3 Center(int x, int row, float y = 0) => new Vector3((x + 0.5f) * CellSize, y + Height(x, row), (row + 0.5f) * CellSize);

    void Start()
    {
        RenderSettings.fog = false;
        PlanTimeLeft = planningDuration; IsPlanning = true; planPanel.SetActive(true);
        if (explorationBag != null) explorationBag.SetActive(false);
        if (photoHint != null) photoHint.SetActive(false);
        moving.GetComponent<Rigidbody>().isKinematic = false;
        moving.enabled = mouseLook.enabled = headBob.enabled = true;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        RefreshPlan();
    }
    void Update()
    {
        if (!IsPlanning || Time.timeScale <= 0) return;
        PlanTimeLeft = Mathf.Max(0, PlanTimeLeft - Time.deltaTime); RefreshPlan();
        if (PlanTimeLeft <= 0)
        {
            IsPlanning = false; watch.PlayerDead();
        }
    }
    void RefreshPlan()
    {
        string text = "PLAN " + Mathf.CeilToInt(PlanTimeLeft) + "s / LEAVE THROUGH THE OPENING";
        if (planCountdown != null) planCountdown.text = text;
        if (objectiveText != null) objectiveText.text = text;
    }
    public void BeginExploration()
    {
        if (!IsPlanning || Time.timeScale <= 0) return;
        IsPlanning = false; planPanel.SetActive(false);
        if (explorationBag != null) explorationBag.SetActive(true);
        if (photoHint != null) photoHint.SetActive(true);
        if (objectiveText != null) objectiveText.text = "FILL " + BlankCells.Length + " BLANKS / " + PickupLetters.Length + " LETTERS ON MAP / REACH THE EXIT";
        watch.StartCountdown();
    }
}
