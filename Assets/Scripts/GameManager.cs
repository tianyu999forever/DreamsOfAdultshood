using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public TMP_Text inventoryText;

    public List<int> numbers = new List<int>();
    public List<string> operators = new List<string>();
    public List<string> collectedItems = new List<string>();
    public string inventoryTitle = "BAG";
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        UpdateInventoryUI();
    }

    public void AddNumber(int number)
    {
        numbers.Add(number);
        collectedItems.Add(number.ToString());

        UpdateInventoryUI();

        Debug.Log("Collected number: " + number);
    }

    public void AddOperator(string symbol)
    {
        operators.Add(symbol);
        collectedItems.Add(DisplaySymbol(symbol));

        UpdateInventoryUI();

        Debug.Log("Collected operator: " + symbol);
    }

    string DisplaySymbol(string symbol)
    {
        if (symbol == "*")
            return "×";

        if (symbol == "/")
            return "÷";

        return symbol;
    }

    void UpdateInventoryUI()
    {
        if (inventoryText == null)
            return;

        inventoryText.text =
    inventoryTitle + "\n" +
    string.Join("   ", collectedItems);
    }
}