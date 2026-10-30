using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class Mission6_WordClue_Jizhijin
{
    public readonly string word, mask, hint;
    public readonly int x, row, number;
    public readonly bool down;
    public Mission6_WordClue_Jizhijin(string word, string mask, int x, int row, int number, bool down, string hint)
    { this.word = word; this.mask = mask; this.x = x; this.row = row; this.number = number; this.down = down; this.hint = hint; }
    public Vector2Int CellAt(int i) => new Vector2Int(x + (down ? 0 : i), row - (down ? i : 0));
}
public sealed class Mission6_CrosswordCell_Jizhijin
{
    public char answer; public bool missing; public int number;
    public readonly List<int> words = new List<int>();
}
public sealed class Mission6_StageDefinition_Jizhijin
{
    public const float CellSize = 2.4f;
    public int number, size, minimumWords;
    public float timeLimit;
    public Vector3 origin;
    public Mission6_WordClue_Jizhijin[] words;
    public Dictionary<Vector2Int, Mission6_CrosswordCell_Jizhijin> crossword;
    public Vector2Int[] blanks, pickups, enemies;
    public string letters;
    public Vector2Int Start => new Vector2Int(1, 1);
    public Vector2Int Exit => new Vector2Int(size - 2, size - 2);
    public Vector3 Center(int x, int row, float y = 0) => origin + new Vector3((x + .5f) * CellSize, y + Height(x, row), (row + .5f) * CellSize);
    public bool IsWall(int x, int row)
    {
        if (x < 0 || row < 0 || x >= size || row >= size) return true;
        if (crossword.ContainsKey(new Vector2Int(x, row))) return false;
        if (x == 4 && row < 4 && row != 1) return true;
        if (number == 1)
        {
            if (row == 4 || row == 12) return x < 6 || x > 8;
            if (row == 8 || row == 16) return x < 11 || x > 13;
        }
        else
        {
            if (row == 4) return x < 6 || x > 8;
            if (row == 10) return x < 16 || x > 18;
            if (row == 16) return x < 9 || x > 11;
            if (row == 22) return x < 22 || x > 24;
        }
        return false;
    }
    public float Height(int x, int row)
    {
        if (number == 1 && ((row >= 9 && row <= 11 && x >= 15 && x <= 17) || (row >= 13 && row <= 15 && x >= 2 && x <= 4))) return .36f;
        if (number == 2 && row >= 11 && row <= 13 && x >= 23 && x <= 25) return .36f;
        return 0;
    }
    public int Ramp(int x, int row)
    {
        if (number == 1)
        {
            if (row >= 9 && row <= 11) { if (x == 14) return 1; if (x == 18) return -1; }
            if (row >= 13 && row <= 15) { if (x == 1) return 1; if (x == 5) return -1; }
        }
        if (number == 2 && row >= 11 && row <= 13) { if (x == 22) return 1; if (x == 26) return -1; }
        return 0;
    }
    void Prepare()
    {
        var starts = words.Select(w => w.CellAt(0)).Distinct().OrderByDescending(p => p.y).ThenBy(p => p.x).ToArray();
        words = words.Select(w => new Mission6_WordClue_Jizhijin(w.word, w.mask, w.x, w.row, Array.IndexOf(starts, w.CellAt(0)) + 1, w.down, w.hint)).ToArray();
        crossword = new Dictionary<Vector2Int, Mission6_CrosswordCell_Jizhijin>();
        for (int wi = 0; wi < words.Length; wi++) for (int i = 0; i < words[wi].word.Length; i++)
        {
            var w = words[wi]; var p = w.CellAt(i);
            if (!crossword.TryGetValue(p, out var c)) crossword[p] = c = new Mission6_CrosswordCell_Jizhijin { answer = w.word[i] };
            if (c.answer != w.word[i]) throw new InvalidOperationException("Conflicting crossword at " + p);
            c.missing |= w.mask[i] == '_'; c.words.Add(wi); if (i == 0) c.number = w.number;
        }
        blanks = crossword.Where(p => p.Value.missing).OrderByDescending(p => p.Key.y).ThenBy(p => p.Key.x).Select(p => p.Key).ToArray();
        if (number == 2)
        {
            var list = blanks.Select(p => crossword[p].answer).Concat("XZJVQU").ToArray(); var rng = new System.Random(602);
            for (int i = list.Length - 1; i > 0; i--) { int j = rng.Next(i + 1); char v = list[i]; list[i] = list[j]; list[j] = v; }
            letters = new string(list);
        }
        if (letters.Length != pickups.Length) throw new InvalidOperationException("Pickup layout mismatch for level " + number + ": " + letters.Length);
    }
    static Mission6_WordClue_Jizhijin W(string word, string mask, int x, int row, bool down, string hint) => new Mission6_WordClue_Jizhijin(word, mask, x, row, 0, down, hint);
    public static readonly Mission6_StageDefinition_Jizhijin First = Build(false);
    public static readonly Mission6_StageDefinition_Jizhijin Second = Build(true);
    static Mission6_StageDefinition_Jizhijin Build(bool second)
    {
        var d = new Mission6_StageDefinition_Jizhijin { number = second ? 2 : 1, size = second ? 28 : 20, minimumWords = second ? 11 : 7, timeLimit = second ? 300 : 180, origin = second ? new Vector3(57.6f, 0, 40.8f) : Vector3.zero };
        d.words = second ? new[] {
            W("RIVER", "_IVE_", 11, 24, false, "Water flowing to the sea"), W("DREAM", "D__AM", 11, 25, true, "Images during sleep"),
            W("GREEN", "G_EEN", 15, 25, true, "The color of grass"), W("MAP", "MA_", 11, 21, false, "A drawing for navigation"),
            W("PLAN", "_LAN", 13, 21, true, "Decide what to do"), W("TRAIN", "TRAI_", 11, 19, false, "Transport on rails"),
            W("GATE", "G_T_", 8, 23, false, "An opening in a fence"), W("PATH", "P_T_", 9, 24, true, "A route to follow"),
            W("EARTH", "_A_T_", 5, 21, false, "Our planet"), W("WATER", "WAT_R", 5, 24, true, "A liquid we drink"),
            W("BRIDGE", "B_ID_E", 7, 22, true, "Cross a river on this"), W("LIGHT", "LI_H_", 5, 18, false, "Makes things visible"),
            W("NIGHT", "_IGHT", 15, 19, true, "The time after sunset"), W("STONE", "S_ON_", 9, 19, true, "A piece of rock"),
            W("HOUSE", "HOUS_", 5, 15, false, "A building to live in")
        } : new[] {
            W("COME", "C_M_", 9, 17, true, "Move toward me"), W("FROM", "F_OM", 11, 17, true, "A starting point"),
            W("YOUR", "Y_U_", 8, 16, false, "Belonging to you"), W("MAKE", "M__E", 7, 15, true, "Create something"),
            W("OF", "O_", 11, 15, false, "A part __ a whole"), W("HAVE", "H_V_", 6, 14, false, "To own"),
            W("HER", "_ER", 6, 12, false, "Belonging to a woman"), W("HE", "_E", 6, 12, true, "A male person"),
            W("KEY", "_EY", 7, 13, false, "Opens a lock"), W("FEW", "_EW", 12, 15, true, "A small number")
        };
        d.pickups = second ? new[] { new Vector2Int(8,2),new Vector2Int(16,3),new Vector2Int(25,6),new Vector2Int(4,7),new Vector2Int(12,8),new Vector2Int(19,9),new Vector2Int(23,12),new Vector2Int(15,13),new Vector2Int(6,14),new Vector2Int(3,17),new Vector2Int(11,18),new Vector2Int(20,19),new Vector2Int(25,20),new Vector2Int(18,23),new Vector2Int(23,25),new Vector2Int(6,26),new Vector2Int(16,26),new Vector2Int(25,26) }
            : new[] { new Vector2Int(9,2),new Vector2Int(13,3),new Vector2Int(18,6),new Vector2Int(12,9),new Vector2Int(8,10),new Vector2Int(4,14),new Vector2Int(14,15),new Vector2Int(9,18),new Vector2Int(17,18) };
        d.letters = "KARFEOHUV"; d.enemies = second ? new[] { new Vector2Int(18,7), new Vector2Int(20,13) } : Array.Empty<Vector2Int>();
        d.Prepare(); return d;
    }
}
