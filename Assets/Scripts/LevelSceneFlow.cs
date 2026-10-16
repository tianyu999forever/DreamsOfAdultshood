using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSceneFlow : MonoBehaviour
{
    public void RestartCurrentLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.Log("Already final level.");
        }
    }
    public void LoadFirstLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}