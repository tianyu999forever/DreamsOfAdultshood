using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Mission 5's session-only star count; shared inventories are not used.</summary>
public class Mission5_StarInventory_ZhuohengWu : MonoBehaviour
{
    public const int RequiredStars = 3;
    public const int PointsPerStar = 100;
    [SerializeField] private TMP_Text hudText;
    private readonly HashSet<string> collectedStars = new HashSet<string>();
    public int StarCount => collectedStars.Count;
    public int Score => StarCount * PointsPerStar;
    public bool ExitUnlocked => StarCount >= RequiredStars;
    public bool Completed { get; private set; }

    private void Start() => RefreshHud();

    internal bool Register(string starId)
    {
        if (Completed || Time.timeScale <= 0f || string.IsNullOrEmpty(starId) ||
            !collectedStars.Add(starId)) return false;
        RefreshHud();
        return true;
    }

    private void RefreshHud()
    {
        if (hudText != null)
            hudText.text = "STARS " + StarCount + " / " + RequiredStars + " TO EXIT\nSCORE " + Score;
    }

    public bool TryComplete()
    {
        if (Completed || !ExitUnlocked || Time.timeScale <= 0f) return false;
        Completed = true;
        return true;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        string path = SceneManager.GetActiveScene().path;
#if UNITY_EDITOR
        // Play-in-Editor also supports this scene when it is not in the build list.
        if (SceneManager.GetActiveScene().buildIndex < 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path,
                new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(path);
    }
}
