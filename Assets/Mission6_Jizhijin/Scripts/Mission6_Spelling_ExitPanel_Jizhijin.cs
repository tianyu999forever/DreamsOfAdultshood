using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Mission6_Spelling_ExitPanel_Jizhijin : MonoBehaviour
{
    public Mission6_Letters_GameManager_Jizhijin inventory;
    public TMP_Dropdown[] letterSlots;
    public Vector2Int[] slotCells;
    public TMP_Text[] wordLabels;
    public TMP_Text resultText;
    public TMP_Text bagText;
    public GameObject playAgainButton;
    public GameObject nextLevelButton;
    [Tooltip("Optional scene name or path. Leave empty to load the next enabled scene in Build Settings.")]
    public string nextSceneName;
    public bool Solved { get; private set; }
    void Awake()
    {
        foreach (var slot in letterSlots)
        {
            var captured = slot;
            slot.onValueChanged.AddListener(_ => SelectionChanged(captured));
        }
    }
    void OnEnable()
    {
        if (inventory == null || letterSlots == null) return;
        ResetCompletion();
        resultText.text = "Fill the crossword. A shared cell completes both directions.";
        var options = new List<string> { "-" };
        foreach (string letter in inventory.Letters) options.Add(letter);
        if (bagText != null) bagText.text = "COLLECTED: " + (inventory.Letters.Count == 0 ? "-" : string.Join("   ", inventory.Letters));
        foreach (var slot in letterSlots)
        { slot.ClearOptions(); slot.AddOptions(options); slot.SetValueWithoutNotify(0); slot.RefreshShownValue(); }
        if (wordLabels != null) foreach (var label in wordLabels) label.color = Color.white;
    }
    void SelectionChanged(TMP_Dropdown changed)
    {
        ResetCompletion();
        if (changed.value == 0) return;
        foreach (var slot in letterSlots)
            if (slot != changed && slot.value == changed.value)
            {
                changed.SetValueWithoutNotify(0); changed.RefreshShownValue();
                resultText.text = "That tile is already used in another cell."; return;
            }
        resultText.text = "One intersection, one letter: it counts for Across and Down.";
    }
    public void CheckAnswer()
    {
        if (inventory == null) return;
        ResetCompletion();
        var used = new HashSet<int>();
        var answers = new Dictionary<Vector2Int, char>();
        for (int i = 0; i < letterSlots.Length; i++)
        {
            int index = letterSlots[i].value - 1;
            if (index < 0 || index >= inventory.Letters.Count)
            { resultText.text = "Fill all " + letterSlots.Length + " blank cells, or return to find more letters."; return; }
            if (!used.Add(index)) { resultText.text = "Use each collected tile in only one cell."; return; }
            answers[slotCells[i]] = inventory.Letters[index][0];
        }
        int correct = 0;
        for (int w = 0; w < Mission6_Grid_Level_Jizhijin.Words.Length; w++)
        {
            var clue = Mission6_Grid_Level_Jizhijin.Words[w]; string answer = "";
            for (int i = 0; i < clue.word.Length; i++)
            {
                var cell = clue.CellAt(i);
                answer += answers.TryGetValue(cell, out char selected) ? selected : Mission6_Grid_Level_Jizhijin.Crossword[cell].answer;
            }
            bool matches = answer == clue.word; if (matches) correct++;
            if (wordLabels != null && w < wordLabels.Length)
                wordLabels[w].color = matches ? new Color(0.4f, 1, 0.7f) : new Color(1, 0.7f, 0.3f);
        }
        Solved = correct == Mission6_Grid_Level_Jizhijin.Words.Length;
        resultText.text = Solved ? "CROSSWORD COMPLETE!  8 / 8\nYou escaped. Well done!" : correct + " / 8 correct. Check the orange clues.";
        playAgainButton.SetActive(Solved);
        if (nextLevelButton != null) nextLevelButton.SetActive(Solved);
    }
    void ResetCompletion()
    {
        Solved = false;
        if (playAgainButton != null) playAgainButton.SetActive(false);
        if (nextLevelButton != null) nextLevelButton.SetActive(false);
    }
    public void NextLevel()
    {
        if (!Solved) return;
        string destination = nextSceneName == null ? "" : nextSceneName.Trim();
        if (destination.Length == 0)
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (SceneManager.GetActiveScene().buildIndex < 0 || nextIndex >= SceneManager.sceneCountInBuildSettings)
            { resultText.text = "All available missions complete!"; return; }
            destination = SceneUtility.GetScenePathByBuildIndex(nextIndex);
        }
        if (!Application.CanStreamedLevelBeLoaded(destination))
        { resultText.text = "The next mission is not available in this build."; return; }
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        SceneManager.LoadScene(destination);
    }
    public void PlayAgain()
    {
        Time.timeScale = 1; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }
}
