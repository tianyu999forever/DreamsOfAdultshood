using System.Linq;
using UnityEditor;

// Only repairs Mission3's stale GUID after a branch switch/merge; keeps the team's scene order and enabled flags.
[InitializeOnLoad]
public static class mission3SceneLink_Editor_ShengChiehLee
{
    const string Scene = "Assets/Scenes/Mission3.unity";
    static mission3SceneLink_Editor_ShengChiehLee() { EditorApplication.update += OnceReady; }
    static void OnceReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= OnceReady;
        Repair();
    }
    public static void Repair()
    {
        string guid = AssetDatabase.AssetPathToGUID(Scene);
        if (string.IsNullOrEmpty(guid)) return;
        var scenes = EditorBuildSettings.scenes;
        bool changed = false;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path != Scene || scenes[i].guid.ToString() == guid) continue;
            scenes[i] = new EditorBuildSettingsScene(Scene, scenes[i].enabled);
            scenes[i].guid = new GUID(guid);
            changed = true;
        }
        if (changed)
        {
            // Unity compares scene paths/enabled flags and can ignore a GUID-only update.
            // Reinsert only Mission3, then restore the exact original order and flags.
            EditorBuildSettings.scenes = scenes.Where(entry => entry.path != Scene).ToArray();
            EditorBuildSettings.scenes = scenes;
        }
    }
}