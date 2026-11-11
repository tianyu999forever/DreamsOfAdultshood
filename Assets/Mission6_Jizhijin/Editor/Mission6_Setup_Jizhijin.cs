using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class Mission6_Setup_Jizhijin
{
    public const string Root = "Assets/Mission6_Jizhijin";
    public const string ScenePath = Root + "/Scenes/Mission6_Jizhijin.unity";

    [MenuItem("Mission6 Jizhijin/Open 20x20 scene")]
    public static void Open()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
    }
}
