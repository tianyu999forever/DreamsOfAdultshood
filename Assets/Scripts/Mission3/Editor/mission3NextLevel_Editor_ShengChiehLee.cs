using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class mission3NextLevel_Editor_ShengChiehLee
{
    public static Button Create(GameObject lift, mission3Hotel_Manager_ShengChiehLee manager)
    {
        var existing = lift.transform.Find("Next Level Canvas");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        var go = new GameObject("Next Level Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        go.transform.SetParent(lift.transform, false);
        go.transform.localPosition = new Vector3(0, 2.12f, 1.64f);
        go.transform.localScale = Vector3.one * .002f;
        var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = manager.playerCamera;
        ((RectTransform)go.transform).sizeDelta = new Vector2(560, 190);

        var buttonGo = new GameObject("NEXT LEVEL", typeof(RectTransform), typeof(Image), typeof(Button), typeof(BoxCollider));
        buttonGo.transform.SetParent(go.transform, false);
        var rect = (RectTransform)buttonGo.transform; rect.sizeDelta = new Vector2(550, 110);
        var image = buttonGo.GetComponent<Image>(); image.color = new Color(.96f, .94f, .86f);
        var button = buttonGo.GetComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1, .86f, .5f);
        colors.pressedColor = new Color(.7f, .58f, .34f); button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        buttonGo.GetComponent<BoxCollider>().size = new Vector3(550, 110, 12);
        var action = buttonGo.AddComponent<mission3Interact_Prop_ShengChiehLee>();
        action.kind = mission3Interact_Prop_ShengChiehLee.Kind.NextLevel;
        action.displayName = "NEXT LEVEL / click to continue";
        UnityEventTools.AddPersistentListener(button.onClick, manager.LoadNextLevel);
        Label(buttonGo.transform, "Text (TMP)", "NEXT LEVEL", 44, Vector2.zero, new Vector2(530, 100), new Color(.07f, .06f, .045f));
        manager.nextLevelStatus = Label(go.transform, "Next level status", "COMPLETE CHECKOUT FIRST", 19, new Vector2(0, -80), new Vector2(550, 44), new Color(1, .9f, .65f));
        manager.nextLevelButton = button;
        if (string.IsNullOrWhiteSpace(manager.nextSceneName)) manager.nextSceneName = "Mission4";

        var events = manager.gameObject.scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<EventSystem>(true)).FirstOrDefault();
        if (events == null)
        {
            var eventsGo = new GameObject("Mission3 EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(eventsGo, manager.gameObject.scene);
        }
        EditorUtility.SetDirty(manager);
        return button;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Vector2 position, Vector2 dimensions, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        label.text = text; label.fontSize = size; label.alignment = TextAlignmentOptions.Center;
        label.color = color; label.raycastTarget = false;
        label.rectTransform.anchoredPosition = position; label.rectTransform.sizeDelta = dimensions;
        return label;
    }

    [MenuItem("Mission3/Add NEXT LEVEL Inside Lift")]
    public static void Upgrade()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before editing the lift.");
        var original = SceneManager.GetActiveScene();
        var paths = new HashSet<string> { mission3Paths_Editor_ShengChiehLee.Scene };
        // Only the canonical scene is upgraded; archived versions stay untouched.
        foreach (var path in paths)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var manager = roots.SelectMany(x => x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).FirstOrDefault();
                var lift = roots.SelectMany(x => x.GetComponentsInChildren<mission3Lift_Prop_ShengChiehLee>(true)).FirstOrDefault();
                if (manager == null || lift == null) continue;
                Create(lift.gameObject, manager);
                Validate(manager);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + path);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        // Keep existing scene order; register the user's active mission without adding duplicate variants.
        var activeManager = original.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).FirstOrDefault();
        if (activeManager != null && original.path == mission3Paths_Editor_ShengChiehLee.Scene)
        {
            var build = EditorBuildSettings.scenes.ToList();
            var entry = build.Find(x => x.path == original.path);
            if (entry == null) build.Add(new EditorBuildSettingsScene(original.path, true)); else entry.enabled = true;
            EditorBuildSettings.scenes = build.ToArray();
            Selection.activeGameObject = activeManager.nextLevelButton.gameObject;
            EditorGUIUtility.PingObject(Selection.activeGameObject);
        }
        Debug.Log("MISSION3_NEXT_LEVEL_SETUP_PASS: world-space NEXT LEVEL inside lift, persistent OnClick, Mission4 destination.");
    }

    public static void Validate(mission3Hotel_Manager_ShengChiehLee manager)
    {
        var button = manager.nextLevelButton;
        if (button == null || button.GetComponentInParent<mission3Lift_Prop_ShengChiehLee>() == null) throw new Exception("NEXT LEVEL must be inside the lift.");
        if (button.onClick.GetPersistentEventCount() != 1 || button.onClick.GetPersistentTarget(0) != manager || button.onClick.GetPersistentMethodName(0) != nameof(manager.LoadNextLevel)) throw new Exception("Missing NEXT LEVEL OnClick binding.");
        if (button.GetComponent<BoxCollider>() == null || button.GetComponent<mission3Interact_Prop_ShengChiehLee>().kind != mission3Interact_Prop_ShengChiehLee.Kind.NextLevel) throw new Exception("Missing E/click interaction.");
        if (manager.nextLevelStatus == null) throw new Exception("Missing next-level status label.");
    }
}
