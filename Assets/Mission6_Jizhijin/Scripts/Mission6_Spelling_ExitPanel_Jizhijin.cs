using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
public class Mission6_Spelling_ExitPanel_Jizhijin : MonoBehaviour
{
    public Mission6_Letters_GameManager_Jizhijin inventory;
    public Mission6_Grid_Level_Jizhijin level;
    public TMP_Dropdown[] letterSlots;
    public Vector2Int[] slotCells;
    public TMP_Text[] wordLabels;
    public TMP_Text resultText, bagText, timeText;
    public GameObject playAgainButton, nextLevelButton;
    public string nextSceneName;
    public int CorrectWords { get; private set; }
    public bool Solved => level != null && CorrectWords >= level.Data.minimumWords;
    int[] selectedTiles;
    readonly List<List<int>> optionTiles = new List<List<int>>();
    bool updating;
    public IReadOnlyList<int> SelectedTiles => selectedTiles;
    void Awake()
    {
        for (int i = 0; i < letterSlots.Length; i++) { int slotIndex = i; letterSlots[i].onValueChanged.AddListener(value => SelectionChanged(slotIndex, value)); }
    }
    void OnEnable()
    {
        if (level == null || inventory == null) return;
        if (selectedTiles == null || selectedTiles.Length != letterSlots.Length) selectedTiles = Enumerable.Repeat(-1, letterSlots.Length).ToArray();
        playAgainButton.SetActive(true); nextLevelButton.SetActive(true);
        resultText.text = level.stageNumber == 1 ? "Optional puzzle. NEXT LEVEL enters level 2 even with no answers." : "Need " + level.Data.minimumWords + " / " + level.Data.words.Length + " words. Answer time counts toward your grade.";
        RefreshMenus();
        foreach (var label in wordLabels) label.color = Color.white;
    }
    void SelectionChanged(int slot, int option)
    {
        if (updating || slot >= optionTiles.Count || option >= optionTiles[slot].Count) return;
        selectedTiles[slot] = optionTiles[slot][option];
        RefreshMenus();
    }
    void RefreshMenus()
    {
        updating = true; optionTiles.Clear();
        var used = new HashSet<int>(selectedTiles.Where(i => i >= 0));
        for (int slot = 0; slot < letterSlots.Length; slot++)
        {
            int own = selectedTiles[slot];
            var ids = new List<int> { -1 }; var names = new List<string> { "-" }; int selectedOption = 0;
            var groups = Enumerable.Range(0, inventory.Letters.Count).Where(i => !used.Contains(i) || i == own).GroupBy(i => inventory.Letters[i]);
            foreach (var group in groups)
            {
                int tile = group.Contains(own) ? own : group.First(); ids.Add(tile);
                names.Add(group.Key + (group.Count() > 1 ? " x" + group.Count() : ""));
                if (tile == own) selectedOption = ids.Count - 1;
            }
            optionTiles.Add(ids); var menu = letterSlots[slot];
            menu.ClearOptions(); menu.AddOptions(names); menu.SetValueWithoutNotify(selectedOption); menu.RefreshShownValue();
            if (own >= 0) menu.captionText.text = inventory.Letters[own];
        }
        bagText.text = "AVAILABLE: " + string.Join("  ", Enumerable.Range(0, inventory.Letters.Count).Where(i => !used.Contains(i)).GroupBy(i => inventory.Letters[i]).Select(g => g.Key + (g.Count() > 1 ? " x" + g.Count() : "")));
        updating = false;
    }
    public void ClearAnswers()
    {
        selectedTiles = Enumerable.Repeat(-1, letterSlots.Length).ToArray(); RefreshMenus();
    }
    public bool SelectLetter(int slot, char letter)
    {
        int option = optionTiles[slot].FindIndex(i => i >= 0 && inventory.Letters[i][0] == letter);
        if (option < 0) return false;
        letterSlots[slot].value = option; return true;
    }
    public void CheckAnswer()
    {
        var data = level.Data; CorrectWords = 0;
        var selected = new Dictionary<Vector2Int, char>(); var used = new HashSet<int>();
        for (int i = 0; i < slotCells.Length; i++)
        {
            int index = selectedTiles[i];
            if (index < 0 || index >= inventory.Letters.Count) continue;
            if (!used.Add(index)) { resultText.text = "Use each collected tile in only one square."; return; }
            selected[slotCells[i]] = inventory.Letters[index][0];
        }
        for (int wi = 0; wi < data.words.Length; wi++)
        {
            var word = data.words[wi]; bool correct = true;
            for (int i = 0; i < word.word.Length; i++)
            {
                var pos = word.CellAt(i); var cell = data.crossword[pos];
                char answer = cell.missing ? (selected.TryGetValue(pos, out var letter) ? letter : '_') : cell.answer;
                correct &= answer == word.word[i];
            }
            if (correct) CorrectWords++;
            wordLabels[wi].color = correct ? new Color(.4f, 1, .7f) : new Color(1, .7f, .3f);
        }
        var score = Mission6_Score_Jizhijin.Calculate(data.number, CorrectWords, data.words.Length, data.minimumWords, level.campaign.Elapsed, data.timeLimit);
        resultText.text = CorrectWords + " / " + data.words.Length + " correct   |   " + (score.passed ? "PASS   GRADE " + score.rank + "   " + score.points.ToString("0.0") + "/100" : data.number == 1 ? "OPTIONAL / NO FIRST-LEVEL GRADE" : "Need " + data.minimumWords + " words to pass") + "\n" + (score.passed ? (data.number == 1 ? "NEXT LEVEL" : "FINISH LEVEL") + " confirms this score. More words and less time improve it." : data.number == 1 ? "NEXT LEVEL enters level 2 even with no grade. Solve 7 words to earn a first-level grade." : "Partial answers count. Return to the maze for more letters.");
    }
    public void NextLevel() { level.campaign.CompleteCurrentLevel(); }
    public void PlayAgain() { level.campaign.Replay(); }
}
