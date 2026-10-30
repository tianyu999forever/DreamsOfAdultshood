using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

// Integration checks against the supplied main project; editor-only.
[InitializeOnLoad]
public static class Mission6_MainAudit_Jizhijin
{
    const string Out = "docs/Mission6_Jizhijin/", Flag = "Mission6.MainAudit";
    static readonly List<string> checks = new List<string>(), errors = new List<string>();
    static double next, deadline;
    static int step, sceneIndex;
    static string[] otherScenes;
    static bool oldBackground;
    static Mission6_MainAudit_Jizhijin()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += Changed;
        Application.logMessageReceived += (message, trace, type) =>
        {
            if (SessionState.GetBool(Flag, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                errors.Add(SceneManager.GetActiveScene().name + ": " + message + "\n" + trace);
        };
    }
    static void Save() { Directory.CreateDirectory(Out); File.WriteAllLines(Out + "main-runtime-verification.txt", checks.Concat(errors.Select(e => "RUNTIME ERROR: " + e))); }
    static void Check(bool ok, string text) { checks.Add((ok ? "PASS: " : "FAIL: ") + text); Save(); if (!ok) throw new Exception(text); }
    public static void Inspect()
    {
        var lines = new List<string>();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var item in EditorBuildSettings.scenes.Where(s => s.enabled))
            {
                var scene = EditorSceneManager.OpenScene(item.path, OpenSceneMode.Single);
                int missing = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                lines.Add((missing == 0 ? "PASS: " : "FAIL: ") + item.path + " | missing scripts: " + missing);
            }
            lines.Add("Build scene order: " + string.Join(" -> ", EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => Path.GetFileNameWithoutExtension(s.path))));
        }
        finally
        {
            Directory.CreateDirectory(Out); File.WriteAllLines(Out + "main-scene-inspection.txt", lines);
            if (setup.Any(s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
    public static void Prepare()
    {
        Inspect();
        EditorSceneManager.OpenScene("Assets/Mission6_Jizhijin/Scenes/Mission6_Jizhijin.unity", OpenSceneMode.Single);
    }
    public static void StartSmoke()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        checks.Clear(); errors.Clear();
        SessionState.SetString(Flag + ".previous", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool(Flag, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes.First(s => s.enabled).path);
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Flag, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            step = 0; next = EditorApplication.timeSinceStartup + 1.5; deadline = next + 120;
            oldBackground = Application.runInBackground; Application.runInBackground = true;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Flag + ".previous", ""));
            SessionState.SetBool(Flag, false); Save();
        }
    }
    static void Advance(int value, double seconds = 1.2) { step = value; next = EditorApplication.timeSinceStartup + seconds; }
    static void Finish()
    {
        checks.Add(errors.Count == 0 ? "PASS: no runtime errors across the main integration smoke test" : "FAIL: runtime errors captured");
        Save(); Time.timeScale = 1; Application.runInBackground = oldBackground; step = 100;
        EditorApplication.ExitPlaymode();
    }
    public static void BuildWindows()
    {
        string location = Path.GetFullPath("../WindowsPlayer/MainCompatibility.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(location));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = location, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
        });
        Directory.CreateDirectory(Out);
        File.WriteAllText(Out + "main-player-build.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings + "\nOutput: " + location);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Main player build failed");
    }
    static void Tick()
    {
        if (!EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            foreach (string action in new[] { "inspect", "smoke", "build" })
            {
                string path = "Temp/Mission6_MainAudit." + action;
                if (!File.Exists(path)) continue;
                File.Delete(path);
                if (action == "inspect") Prepare(); else if (action == "smoke") StartSmoke(); else BuildWindows();
                return;
            }
        }
        if (!SessionState.GetBool(Flag, false) || !EditorApplication.isPlaying || step == 100 || EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException();
            switch (step)
            {
                case 0:
                    Check(SceneManager.GetActiveScene().buildIndex == 0, "main startup scene loads in the supplied build order");
                    var startupAgents = UnityEngine.Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None);
                    Check(startupAgents.Length > 0 && startupAgents.All(a => a.isOnNavMesh), "main Mission2 enemies bind to their own NavMesh after startup");
                    var select = UnityEngine.Object.FindFirstObjectByType<LevelSelectMenu>();
                    Check(select != null, "main level selection menu exists");
                    var names = new SerializedObject(select).FindProperty("sceneNames");
                    string[] expectedNames = { "Mission1", "Mission2_TianyuTang", "Mission3", "Mission4", "ZhuohengWu Mission5", "Mission6_Jizhijin" };
                    Check(Enumerable.Range(0, names.arraySize).Select(i => names.GetArrayElementAtIndex(i).stringValue).SequenceEqual(expectedNames), "MISSION 1 through 6 labels correspond to the correct serialized scenes");
                    int targetIndex = Enumerable.Range(0, names.arraySize).First(i => names.GetArrayElementAtIndex(i).stringValue == "Mission6_Jizhijin");
                    UnityEngine.UI.Button linked = null;
                    foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        var calls = new SerializedObject(button).FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                        for (int i = 0; i < calls.arraySize; i++)
                        {
                            var call = calls.GetArrayElementAtIndex(i);
                            if (call.FindPropertyRelative("m_Target").objectReferenceValue == select && call.FindPropertyRelative("m_MethodName").stringValue == "SelectLevel" && call.FindPropertyRelative("m_Arguments.m_IntArgument").intValue == targetIndex) linked = button;
                        }
                    }
                    Check(linked != null, "main menu has a button bound to the serialized Mission6 entry");
                    string label = linked.GetComponentInChildren<TMPro.TMP_Text>(true)?.text ?? linked.GetComponentInChildren<UnityEngine.UI.Text>(true)?.text ?? "";
                    Check(label.Replace(" ", "").ToUpperInvariant() == "MISSION6", "the actual MISSION 6 button is the Mission6 entry");
                    linked.onClick.Invoke(); Advance(1); break;
                case 1:
                    var c = UnityEngine.Object.FindFirstObjectByType<Mission6_Campaign_Jizhijin>();
                    Check(SceneManager.GetActiveScene().name == "Mission6_Jizhijin" && c != null, "main menu selects the updated Mission6 scene");
                    Check(c.Phase == Mission6_Phase_Jizhijin.Planning && c.moving.enabled && Time.timeScale == 1, "entering Mission6 resets time and player controls");
                    c.BeginExploration(); c.OpenPuzzle(c.CurrentLevel); Advance(2, .2); break;
                case 2:
                    var campaign = UnityEngine.Object.FindFirstObjectByType<Mission6_Campaign_Jizhijin>();
                    Check(campaign.PuzzleOpen && Time.timeScale == 0 && !campaign.moving.enabled, "main scripts do not override Mission6 answer pause");
                    campaign.ClosePuzzle(); Check(Time.timeScale == 1 && campaign.moving.enabled, "main scripts do not block return to maze");
                    campaign.NextMission(); Advance(3); break;
                case 3:
                    Check(SceneManager.GetActiveScene().name == "Mission1", "Mission6 NEXT MISSION follows main build order to Mission1");
                    Check(UnityEngine.Object.FindFirstObjectByType<Moving>() != null && Time.timeScale == 1, "Mission1 retains its player and unpaused time after returning from Mission6");
                    otherScenes = EditorBuildSettings.scenes.Where(s => s.enabled && !s.path.Contains("Mission6_Jizhijin") && !s.path.EndsWith("/Mission1.unity")).Select(s => s.path).ToArray();
                    sceneIndex = 0; Advance(4, .1); break;
                case 4:
                    if (sceneIndex == otherScenes.Length) { Finish(); break; }
                    Time.timeScale = 1; SceneManager.LoadScene(otherScenes[sceneIndex]); Advance(5); break;
                case 5:
                    Check(SceneManager.GetActiveScene().path == otherScenes[sceneIndex], "main scene loads: " + SceneManager.GetActiveScene().name);
                    Check(SceneManager.GetActiveScene().GetRootGameObjects().Any(r => r.GetComponentInChildren<Camera>(true) != null), "main scene retains a camera: " + SceneManager.GetActiveScene().name);
                    sceneIndex++; Advance(4, .1); break;
            }
        }
        catch (Exception ex) { checks.Add("FAIL: " + ex); Finish(); }
    }
}
