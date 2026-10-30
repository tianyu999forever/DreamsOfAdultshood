using System.Collections.Generic;
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
    void Awake()
    {
        foreach (var slot in letterSlots) { var captured = slot; slot.onValueChanged.AddListener(_ => SelectionChanged(captured)); }
    }
    void OnEnable()
    {
        if (level == null || inventory == null) return;
        CorrectWords = 0; playAgainButton.SetActive(true); nextLevelButton.SetActive(true);
        resultText.text = "Need " + level.Data.minimumWords + " / " + level.Data.words.Length + " words. Answer time counts toward your grade.";
        var options = new List<string> { "-" };
        foreach (var letter in inventory.Letters) options.Add(letter);
        bagText.text = "BAG: " + string.Join("  ", inventory.Letters);
        foreach (var slot in letterSlots) { slot.ClearOptions(); slot.AddOptions(options); slot.SetValueWithoutNotify(0); slot.RefreshShownValue(); }
        foreach (var label in wordLabels) label.color = Color.white;
    }
    void SelectionChanged(TMP_Dropdown changed)
    {
        if (changed.value == 0) return;
        foreach (var slot in letterSlots) if (slot != changed && slot.value == changed.value)
        { changed.SetValueWithoutNotify(0); changed.RefreshShownValue(); resultText.text = "That tile is already used in another square."; return; }
    }
    public void CheckAnswer()
    {
        var data = level.Data; CorrectWords = 0;
        var selected = new Dictionary<Vector2Int, char>(); var used = new HashSet<int>();
        for (int i = 0; i < slotCells.Length; i++)
        {
            int index = letterSlots[i].value - 1;
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
        resultText.text = CorrectWords + " / " + data.words.Length + " correct   |   " + (score.passed ? "PASS   GRADE " + score.rank + "   " + score.points.ToString("0.0") + "/100" : "Need " + data.minimumWords + " words to pass") + "\n" + (score.passed ? (data.number == 1 ? "NEXT LEVEL" : "FINISH LEVEL") + " confirms this score. More words and less time improve it." : "Partial answers count. Return to the maze for more letters.");
    }
    public void NextLevel() { level.campaign.CompleteCurrentLevel(); }
    public void PlayAgain() { level.campaign.Replay(); }
}
