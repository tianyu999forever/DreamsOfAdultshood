using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Mission6_Letters_GameManager_Jizhijin : MonoBehaviour
{
    public TMP_Text inventoryText;
    public string targetWord = "DREAM";
    readonly List<string> letters = new List<string>();
    public IReadOnlyList<string> Letters => letters;

    void Start() => Refresh();

    public bool AddLetter(string letter)
    {
        if (string.IsNullOrWhiteSpace(letter)) return false;
        letter = letter.Trim().ToUpperInvariant();
        if (letter.Length != 1 || letter[0] < 'A' || letter[0] > 'Z') return false;
        letters.Add(letter);
        Refresh();
        return true;
    }

    public void Refresh()
    {
        if (inventoryText != null)
            inventoryText.text = "LETTER BAG  " + letters.Count + "\n" +
                (letters.Count == 0 ? "Collect the missing letters" : string.Join("   ", letters));
    }
}
