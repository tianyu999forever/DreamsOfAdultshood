using UnityEngine;
[System.Serializable]
public struct Mission6_Score_Jizhijin
{
    public int level, correctWords, totalWords, minimumWords;
    public float seconds, timeLimit, points;
    public string rank;
    public bool passed;
    public static Mission6_Score_Jizhijin Calculate(int level, int correct, int total, int minimum, float seconds, float limit)
    {
        float points = 70f * Mathf.Clamp01((float)correct / Mathf.Max(1, total)) + 30f * Mathf.Clamp01(1f - Mathf.Max(0, seconds) / Mathf.Max(1, limit));
        bool passed = correct >= minimum && seconds < limit;
        return new Mission6_Score_Jizhijin { level = level, correctWords = correct, totalWords = total, minimumWords = minimum, seconds = seconds, timeLimit = limit, points = points, passed = passed,
            rank = !passed ? "--" : points >= 90 ? "S" : points >= 80 ? "A" : points >= 70 ? "B" : "C" };
    }
    public string Summary => "LEVEL " + level + "   " + rank + "   " + points.ToString("0.0") + "/100\n" + correctWords + "/" + totalWords + " words   |   " + seconds.ToString("0.0") + "s";
}
