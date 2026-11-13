using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class mission3Tutorial_Editor_ShengChiehLee
{
    public const string Root = "Assets/Mission3_ShengChiehLee/Tutorial";
    public const string Prefab = Root + "/Resources/Mission3/TutorialRoom.prefab";
    const string Request = "Library/Mission3TutorialBuild.txt";
    const string Report = "Library/Mission3TutorialBuildReport.txt";
    const string Version = "tutorial-v10";
    static readonly Color Ink = new Color32(20, 29, 35, 248), Cyan = new Color32(82, 221, 221, 255), Gold = new Color32(234, 197, 122, 255), White = new Color32(239, 239, 225, 255);
    static TMP_FontAsset font;
    static mission3Tutorial_Editor_ShengChiehLee() { File.WriteAllText("Library/Mission3TutorialEditorReady.txt", Version); EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var command = File.ReadAllText(Request).Trim(); File.Delete(Request);
        try
        {
            if (command.StartsWith("export|")) { Export(command.Substring(7)); File.WriteAllText(Report, "EXPORT_PASS\n" + command.Substring(7)); }
            else if (command == "preview") { Preview(); File.WriteAllText(Report, "PREVIEW_READY\nMission3 mandatory tutorial."); }
            else { Build(); File.WriteAllText(Report, "BUILD_PASS " + Version + "\n" + Prefab); }
        }
        catch (Exception error) { File.WriteAllText(Report, "BUILD_FAIL\n" + error); Debug.LogException(error); }
    }
    [MenuItem("Mission3/Play Mandatory Tutorial")]
    public static void Preview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode first.");
        if (SceneManager.GetActiveScene().isDirty) throw new Exception("Save your current scene before opening the preview.");
        EditorSceneManager.OpenScene("Assets/Scenes/Mission3.unity");
        var game = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (game != null) EditorWindow.GetWindow(game, false, "Game", true).Focus();
        EditorApplication.isPaused = false; EditorApplication.EnterPlaymode();
    }
    [MenuItem("Mission3/Export My Tutorial Assets")]
    public static void ExportOwnedAssets() { Export(Path.GetFullPath("Temp/Mission3-Tutorial.unitypackage")); }
    static void Export(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        AssetDatabase.ExportPackage(Root, path, ExportPackageOptions.Recurse);
    }
    [MenuItem("Mission3/Build Mandatory Tutorial Room")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode before editing the room.");
        Folder(Root + "/Resources/Mission3"); Folder(Root + "/Materials");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        var wall = Material("Tutorial_Wall", new Color32(148, 133, 103, 255));
        var floor = Material("Tutorial_Floor", new Color32(73, 74, 69, 255));
        var ceiling = Material("Tutorial_Ceiling", new Color32(184, 175, 151, 255));
        var dark = Material("Tutorial_Trim", new Color32(27, 42, 47, 255));
        var cyan = Material("Tutorial_Marker", Cyan, true);
        var gold = Material("Tutorial_Card", Gold, true);
        var originalScene = SceneManager.GetActiveScene();
        var temporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(temporaryScene);
        var root = new GameObject("Mission3_TutorialRoom");
        try
        {
            var lesson = root.AddComponent<mission3Tutorial_Manager_ShengChiehLee>();
            var room = Child(root.transform, "Room");
            Box(room, "Floor", new Vector3(0, -.15f, 1.5f), new Vector3(12, .3f, 21), floor);
            Box(room, "Ceiling", new Vector3(0, 4.55f, 0), new Vector3(12, .2f, 18), ceiling);
            Box(room, "WestWall", new Vector3(-6, 2.25f, 0), new Vector3(.25f, 4.5f, 18), wall);
            Box(room, "EastWall", new Vector3(6, 2.25f, 0), new Vector3(.25f, 4.5f, 18), wall);
            Box(room, "BackWall", new Vector3(0, 2.25f, -9), new Vector3(12, 4.5f, .25f), wall);
            Box(room, "FrontWest", new Vector3(-3.7f, 2.25f, 9), new Vector3(4.6f, 4.5f, .25f), wall);
            Box(room, "FrontEast", new Vector3(3.7f, 2.25f, 9), new Vector3(4.6f, 4.5f, .25f), wall);
            Box(room, "DoorLintel", new Vector3(0, 4.1f, 9), new Vector3(2.8f, .8f, .25f), dark);
            for (int z = -8; z <= 8; z += 4)
                Box(room, "Floor guide " + z, new Vector3(0, .012f, z), new Vector3(.09f, .02f, .8f), gold, false);
            lesson.spawn = Child(root.transform, "PlayerSpawn"); lesson.spawn.localPosition = new Vector3(0, 1, -6.5f);
            lesson.walkTarget = Child(root.transform, "WalkTarget"); lesson.walkTarget.localPosition = new Vector3(0, 0, -2.5f);
            lesson.sprintTarget = Child(root.transform, "SprintTarget"); lesson.sprintTarget.localPosition = new Vector3(0, 0, 1.5f);
            lesson.walkMarker = Marker(root.transform, "WalkMarker", lesson.walkTarget.localPosition, cyan);
            lesson.sprintMarker = Marker(root.transform, "SprintMarker", lesson.sprintTarget.localPosition, cyan);
            Box(room, "LowJumpBarrier", new Vector3(0, .18f, 3.6f), new Vector3(9, .36f, .35f), dark);
            Box(room, "BarrierGoldEdge", new Vector3(0, .37f, 3.6f), new Vector3(9, .025f, .35f), gold, false);
            Box(room, "PracticeDesk", new Vector3(0, .5f, 6.4f), new Vector3(2.4f, 1, .9f), dark);
            var card = Box(room, "TrainingCard", new Vector3(0, 1.12f, 6.1f), new Vector3(.6f, .16f, .5f), gold);
            card.GetComponent<BoxCollider>().size = new Vector3(1.5f, 4f, 1.5f);
            card.GetComponent<BoxCollider>().isTrigger = true;
            lesson.practiceCard = card.transform; lesson.practiceCollider = card.GetComponent<Collider>();
            lesson.practiceLabel = WorldText(room, "PracticeCardLabel", "TRAINING CARD", new Vector3(0, 1.6f, 6.35f), new Vector2(3, .5f), 2.5f, Gold).gameObject;
            lesson.doorHinge = Child(root.transform, "ExitDoorHinge"); lesson.doorHinge.localPosition = new Vector3(-1.4f, 0, 8.95f);
            var door = Box(lesson.doorHinge, "LockedDoor", new Vector3(1.4f, 1.8f, 0), new Vector3(2.75f, 3.6f, .18f), dark);
            lesson.doorCollider = door.GetComponent<Collider>();
            lesson.doorLabel = WorldText(room, "ExitSign", "LOCKED\nCOMPLETE TUTORIAL", new Vector3(0, 4.08f, 8.7f), new Vector2(5, .7f), 2.8f, Gold);
            lesson.exitTarget = Child(root.transform, "EnterHotelZone"); lesson.exitTarget.localPosition = new Vector3(0, 1, 10.3f);
            WorldText(room, "WallTitle", "FLOOR 03\nTUTORIAL ROOM", new Vector3(4.9f, 2.2f, 4), new Vector2(5, 2), 5f, White).transform.localRotation = Quaternion.Euler(0, 90, 0);
            Light(room, new Vector3(-2.7f, 3.25f, -4), 3.5f, new Color(1, .86f, .65f));
            Light(room, new Vector3(2.7f, 3.25f, 5), 3.5f, new Color(.65f, .84f, 1));
            var canvasObject = new GameObject("TutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false); var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 3000;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            var hud = Panel(canvasObject.transform, "CurrentObjective", new Vector2(350, -185), new Vector2(610, 280), Ink);
            hud.anchorMin = hud.anchorMax = new Vector2(0, 1);
            lesson.stepLabel = Label(hud, "Step", "", new Vector2(0, 101), new Vector2(550, 28), 17, Gold);
            lesson.titleLabel = Label(hud, "Objective", "", new Vector2(0, 57), new Vector2(550, 53), 29, White);
            lesson.instructionLabel = Label(hud, "Instruction", "", new Vector2(0, -10), new Vector2(550, 80), 22, White);
            lesson.keyLabel = Label(hud, "Key", "", new Vector2(0, -91), new Vector2(550, 35), 18, Cyan);
            var progress = Panel(canvasObject.transform, "Progress", new Vector2(0, -390), new Vector2(450, 60), Ink);
            lesson.progressLabel = Label(progress, "ProgressText", "", new Vector2(0, 10), new Vector2(420, 28), 17, Gold, TextAlignmentOptions.Center);
            var bar = Panel(progress, "ProgressBar", new Vector2(0, -16), new Vector2(400, 5), new Color(.15f,.22f,.25f));
            var fill = Panel(bar, "Fill", new Vector2(-200, 0), new Vector2(400, 5), Cyan).GetComponent<Image>(); fill.rectTransform.pivot = new Vector2(0, .5f); lesson.progressFill = fill;
            lesson.promptLabel = Label(canvasObject.transform, "Prompt", "", new Vector2(0, -300), new Vector2(1000, 50), 22, White, TextAlignmentOptions.Center);
            Label(canvasObject.transform, "Reticle", "+", Vector2.zero, new Vector2(25, 25), 25, White, TextAlignmentOptions.Center);
            lesson.welcomePanel = Modal(canvasObject.transform, "Welcome", "MISSION 03 / TUTORIAL ROOM", "A safe room to learn the controls.\n\nComplete six short actions to unlock the exit.\nNo timer. No threats. Your hotel belongings start afterwards.", out var begin);
            lesson.beginButton = begin; begin.GetComponentInChildren<TMP_Text>().text = "BEGIN TRAINING";
            lesson.pausePanel = Modal(canvasObject.transform, "Paused", "TRAINING PAUSED", "Your progress is kept.\n\nClick Resume to return to the room.", out var resume);
            lesson.resumeButton = resume; resume.GetComponentInChildren<TMP_Text>().text = "RESUME TRAINING";
            var tools = Panel(canvasObject.transform, "ToolOverlay", Vector2.zero, new Vector2(1500, 810), Ink); lesson.toolsPanel = tools.gameObject;
            var map = Panel(tools, "Map", Vector2.zero, new Vector2(1440, 750), Ink); lesson.mapPanel = map.gameObject;
            Label(map, "Title", "FLOOR MAP", new Vector2(0, 325), new Vector2(1300, 60), 34, Gold, TextAlignmentOptions.Center);
            var mapObject = new GameObject("FloorPlan", typeof(RectTransform), typeof(RawImage)); mapObject.transform.SetParent(map, false); var mapRect = (RectTransform)mapObject.transform; mapRect.sizeDelta = new Vector2(560, 560); lesson.mapImage = mapObject.GetComponent<RawImage>(); lesson.mapImage.raycastTarget = false;
            Label(map, "Close", "M OR ESC   CLOSE MAP", new Vector2(0, -335), new Vector2(1300, 50), 23, Cyan, TextAlignmentOptions.Center);
            var journal = Panel(tools, "Journal", Vector2.zero, new Vector2(1440, 750), Ink); lesson.journalPanel = journal.gameObject;
            Label(journal, "Title", "GUEST JOURNAL", new Vector2(0, 275), new Vector2(1300, 60), 34, Gold, TextAlignmentOptions.Center);
            Label(journal, "TrainingNote", "RECEPTION MEMO\n\nRead guest notes to learn who owns each belonging.\nWhere you find an item does not tell you its owner.\n\nReturn belongings at Lost & Found.\nThen enter the lift to settle your score.", new Vector2(0, 10), new Vector2(1050, 400), 29, White);
            Label(journal, "Close", "TAB OR ESC   CLOSE JOURNAL", new Vector2(0, -290), new Vector2(1300, 50), 23, Cyan, TextAlignmentOptions.Center);
            var fade = Panel(canvasObject.transform, "Transition", Vector2.zero, new Vector2(1600, 900), new Color(.02f,.03f,.035f)); fade.anchorMin = Vector2.zero; fade.anchorMax = Vector2.one; fade.sizeDelta = Vector2.zero;
            lesson.fade = fade.gameObject.AddComponent<CanvasGroup>(); lesson.fade.alpha = 0; lesson.fade.blocksRaycasts = false;
            PrefabUtility.SaveAsPrefabAsset(root, Prefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.CloseScene(temporaryScene, true);
            if (originalScene.IsValid() && originalScene.isLoaded) SceneManager.SetActiveScene(originalScene);
        }
    }
    static void Folder(string path) { if (AssetDatabase.IsValidFolder(path)) return; var parent = Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path)); }
    static Material Material(string name, Color color, bool unlit = false)
    {
        string path = Root + "/Materials/" + name + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color); if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .15f);
        EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material); return material;
    }
    static Transform Child(Transform parent, string name) { var child = new GameObject(name).transform; child.SetParent(parent, false); return child; }
    static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collision = true)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name; box.transform.SetParent(parent, false); box.transform.localPosition = position; box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material; box.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        if (!collision) UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>()); return box;
    }
    static GameObject Marker(Transform parent, string name, Vector3 position, Material material)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder); marker.name = name; marker.transform.SetParent(parent, false); marker.transform.localPosition = position + Vector3.up * .015f; marker.transform.localScale = new Vector3(1.6f, .012f, 1.6f); marker.GetComponent<Renderer>().sharedMaterial = material; UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>()); return marker;
    }
    static TMP_Text WorldText(Transform parent, string name, string value, Vector3 position, Vector2 size, float fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro)); go.transform.SetParent(parent, false); go.transform.localPosition = position;
        var text = go.GetComponent<TextMeshPro>(); text.font = font; text.text = value; text.fontSize = fontSize; text.color = color; text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = size; return text;
    }
    static void Light(Transform parent, Vector3 position, float intensity, Color color) { var go = Child(parent, "TutorialLight"); go.localPosition = position; var light = go.gameObject.AddComponent<Light>(); light.type = LightType.Point; light.range = 13; light.intensity = intensity; light.color = color; light.shadows = LightShadows.None; }
    static RectTransform Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position;
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return rect;
    }
    static TMP_Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false); var text = go.GetComponent<TextMeshProUGUI>(); text.font = font; text.text = value; text.fontSize = fontSize; text.color = color; text.alignment = alignment; text.raycastTarget = false; text.rectTransform.sizeDelta = size; text.rectTransform.anchoredPosition = position; return text;
    }
    static GameObject Modal(Transform parent, string name, string title, string body, out Button button)
    {
        var overlay = Panel(parent, name, Vector2.zero, new Vector2(1600, 900), new Color(.015f,.025f,.03f,1f)); overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.sizeDelta = Vector2.zero; overlay.GetComponent<Image>().raycastTarget = true;
        Label(overlay, "Heading", title, new Vector2(0, 160), new Vector2(1100, 75), 42, Gold, TextAlignmentOptions.Center);
        Label(overlay, "Message", body, new Vector2(0, -5), new Vector2(1000, 210), 28, White, TextAlignmentOptions.Center);
        var box = Panel(overlay, "Action", new Vector2(0, -210), new Vector2(430, 75), Cyan); box.GetComponent<Image>().raycastTarget = true; button = box.gameObject.AddComponent<Button>(); button.targetGraphic = box.GetComponent<Image>();
        Label(box, "Label", "CONTINUE", Vector2.zero, new Vector2(410, 65), 26, Ink, TextAlignmentOptions.Center); return overlay.gameObject;
    }
}
