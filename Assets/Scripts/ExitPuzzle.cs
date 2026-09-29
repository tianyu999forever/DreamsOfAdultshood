using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class ExitPuzzle : MonoBehaviour
{
    public TMP_Dropdown number1Dropdown;
    public TMP_Dropdown operator1Dropdown;
    public TMP_Dropdown number2Dropdown;
    public TMP_Dropdown operator2Dropdown;
    public TMP_Dropdown number3Dropdown;
    public GameObject playAgainButton;
    public TMP_Text resultText;

    void Awake()
    {
        number1Dropdown.onValueChanged.AddListener(
            _ => PreventNumberRepeat(number1Dropdown));

        number2Dropdown.onValueChanged.AddListener(
            _ => PreventNumberRepeat(number2Dropdown));

        number3Dropdown.onValueChanged.AddListener(
            _ => PreventNumberRepeat(number3Dropdown));

        operator1Dropdown.onValueChanged.AddListener(
            _ => PreventOperatorRepeat(operator1Dropdown));

        operator2Dropdown.onValueChanged.AddListener(
            _ => PreventOperatorRepeat(operator2Dropdown));
    }

    void OnEnable()
    {
        if (resultText != null)
            resultText.text = "";

        void OnEnable()
        {
            if (resultText != null)
                resultText.text = "";

            if (playAgainButton != null)
                playAgainButton.SetActive(false);

            FillDropdowns();
        }
        FillDropdowns();
    }

    void FillDropdowns()
    {
        if (GameManager.instance == null)
            return;

        List<string> numberOptions = new List<string>();
        List<string> operatorOptions = new List<string>();

        numberOptions.Add("SELECT");
        operatorOptions.Add("SELECT");

        foreach (int number in GameManager.instance.numbers)
        {
            numberOptions.Add(number.ToString());
        }

        foreach (string symbol in GameManager.instance.operators)
        {
            operatorOptions.Add(DisplaySymbol(symbol));
        }

        SetOptions(number1Dropdown, numberOptions);
        SetOptions(number2Dropdown, numberOptions);
        SetOptions(number3Dropdown, numberOptions);

        SetOptions(operator1Dropdown, operatorOptions);
        SetOptions(operator2Dropdown, operatorOptions);
    }

    void SetOptions(
        TMP_Dropdown dropdown,
        List<string> options)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(0);
        dropdown.RefreshShownValue();
    }

    void PreventNumberRepeat(TMP_Dropdown changedDropdown)
    {
        if (changedDropdown.value == 0)
            return;

        bool repeated =
            changedDropdown != number1Dropdown &&
            changedDropdown.value == number1Dropdown.value;

        repeated = repeated ||
            changedDropdown != number2Dropdown &&
            changedDropdown.value == number2Dropdown.value;

        repeated = repeated ||
            changedDropdown != number3Dropdown &&
            changedDropdown.value == number3Dropdown.value;

        if (repeated)
        {
            changedDropdown.SetValueWithoutNotify(0);
            changedDropdown.RefreshShownValue();

            resultText.text =
                "That collected number is already used";
        }
    }

    void PreventOperatorRepeat(TMP_Dropdown changedDropdown)
    {
        if (changedDropdown.value == 0)
            return;

        TMP_Dropdown otherDropdown =
            changedDropdown == operator1Dropdown
            ? operator2Dropdown
            : operator1Dropdown;

        if (changedDropdown.value == otherDropdown.value)
        {
            changedDropdown.SetValueWithoutNotify(0);
            changedDropdown.RefreshShownValue();

            resultText.text =
                "That collected operator is already used";
        }
    }

    public void CheckAnswer()
    {
        if (GameManager.instance.numbers.Count < 3 ||
            GameManager.instance.operators.Count < 2)
        {
            resultText.text =
                "Collect at least 3 numbers and 2 operators";
            return;
        }

        if (number1Dropdown.value == 0 ||
            number2Dropdown.value == 0 ||
            number3Dropdown.value == 0 ||
            operator1Dropdown.value == 0 ||
            operator2Dropdown.value == 0)
        {
            resultText.text = "Complete every selection";
            return;
        }

        // 下拉框索引减1，对应物品栏中的收集品
        int number1 =
            GameManager.instance.numbers[
                number1Dropdown.value - 1];

        int number2 =
            GameManager.instance.numbers[
                number2Dropdown.value - 1];

        int number3 =
            GameManager.instance.numbers[
                number3Dropdown.value - 1];

        string operator1 =
            GameManager.instance.operators[
                operator1Dropdown.value - 1];

        string operator2 =
            GameManager.instance.operators[
                operator2Dropdown.value - 1];

        float answer = Calculate(
            number1,
            operator1,
            number2,
            operator2,
            number3);

        if (float.IsNaN(answer))
        {
            resultText.text = "Cannot divide by zero";
            return;
        }

        float difference = Mathf.Abs(answer - 12f);
        int lostScore =
            Mathf.RoundToInt(difference * 10f);

        int score = Mathf.Clamp(
            100 - lostScore,
            0,
            100);

        resultText.text =
            "RESULT: " + answer.ToString("0.##") +
            "\nYOUR CREDICT IS " + score + " ";

        playAgainButton.SetActive(true);
    }

    float Calculate(
        float number1,
        string operator1,
        float number2,
        string operator2,
        float number3)
    {
        if (Priority(operator2) > Priority(operator1))
        {
            float right =
                Apply(number2, operator2, number3);

            return Apply(number1, operator1, right);
        }

        float left =
            Apply(number1, operator1, number2);

        return Apply(left, operator2, number3);
    }

    float Apply(float left, string symbol, float right)
    {
        if (symbol == "+") return left + right;
        if (symbol == "-") return left - right;
        if (symbol == "*") return left * right;

        if (symbol == "/")
        {
            if (Mathf.Abs(right) < 0.001f)
                return float.NaN;

            return left / right;
        }

        return 0;
    }

    int Priority(string symbol)
    {
        if (symbol == "*" || symbol == "/")
            return 2;

        return 1;
    }

    string DisplaySymbol(string symbol)
    {
        if (symbol == "*") return "×";
        if (symbol == "/") return "÷";

        return symbol;
    }
    public void PlayAgain()
    {
        // 必须先恢复时间，否则新游戏仍可能暂停
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex);
    }
}