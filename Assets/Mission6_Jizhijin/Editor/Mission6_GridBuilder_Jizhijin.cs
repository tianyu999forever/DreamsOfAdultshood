using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class Mission6_GridBuilder_Jizhijin
{
    const string Root = "Assets/Mission6_Jizhijin";
    const string ScenePath = Root + "/Scenes/Mission6_Jizhijin.unity";
    static Scene scene;
    static T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
    static Mission6_GridBuilder_Jizhijin() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists("Temp/Mission6_Jizhijin.grid") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Temp/Mission6_Jizhijin.grid");
        try { Rebuild(); File.WriteAllText("docs/Mission6_Jizhijin/grid-build.txt", "Built 20x20 maze with connected crossword, shared blank cells and walkable PLAN room. Map synchronized."); }
        catch (Exception error) { File.WriteAllText("docs/Mission6_Jizhijin/grid-build-error.txt", error.ToString()); Debug.LogException(error); }
    }

    [MenuItem("Mission6 Jizhijin/Rebuild 20x20 word grid")]
    public static void Rebuild()
    {
        scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var player = All<Moving>().First(x => x.gameObject.activeInHierarchy);
        var canvas = All<Canvas>().First(x => x.renderMode == RenderMode.ScreenSpaceOverlay);
        var inventory = All<Mission6_Letters_GameManager_Jizhijin>().Single();
        var watch = player.GetComponentInChildren<PocketWatch>(true);
        var template = canvas.transform.Find("DropdownTemplate_Jizhijin");
        if (template == null)
        {
            var source = All<TMP_Dropdown>().First();
            template = Object.Instantiate(source.gameObject, canvas.transform).transform;
            template.name = "DropdownTemplate_Jizhijin"; template.gameObject.SetActive(false);
        }
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            if (canvas.transform.GetChild(i) != template) Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject);

        // Archive this scene's old environment as an inactive group; shared assets remain untouched.
        var legacy = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "LegacyEnvironment_Jizhijin");
        if (legacy == null) legacy = new GameObject("LegacyEnvironment_Jizhijin");
        var oldGrid = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Grid20x20_Jizhijin");
        if (oldGrid != null) Object.DestroyImmediate(oldGrid);
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root == player.gameObject || root == canvas.gameObject || root == inventory.gameObject || root == legacy || root.GetComponent<UnityEngine.EventSystems.EventSystem>() != null) continue;
            root.transform.SetParent(legacy.transform, true);
        }
        legacy.SetActive(false);
        foreach (var flare in All<LensFlareComponentSRP>()) flare.enabled = false;
        foreach (var flare in All<LensFlare>()) flare.enabled = false;
        foreach (var volume in All<Volume>()) volume.enabled = false;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.69f, 0.75f, 0.79f);
        RenderSettings.ambientIntensity = 1;
        var camera = player.GetComponentInChildren<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.63f, 0.77f, 0.84f);
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        player.transform.position = Mission6_Grid_Level_Jizhijin.Center(1, 1, 1.4f);
        player.transform.rotation = Quaternion.identity;
        camera.transform.localRotation = Quaternion.Euler(-8, 0, 0);
        player.GetComponent<Rigidbody>().isKinematic = false;
        watch.totalTime = 90;

        var grid = new GameObject("Grid20x20_Jizhijin");
        var level = grid.AddComponent<Mission6_Grid_Level_Jizhijin>();
        level.moving = player; level.mouseLook = player.GetComponentInChildren<MouseLook>();
        level.headBob = player.GetComponentInChildren<HeadBob>(); level.watch = watch;
        var floorA = Material("GridFloorA", new Color(0.44f, 0.56f, 0.51f));
        var floorB = Material("GridFloorB", new Color(0.5f, 0.62f, 0.56f));
        var wall = Material("GridWall", new Color(0.23f, 0.35f, 0.4f));
        var raised = Material("GridRaised", new Color(0.24f, 0.5f, 0.59f));
        var slope = Material("GridRamp", new Color(0.65f, 0.51f, 0.33f));
        var wordFloor = Material("GridWord", new Color(0.86f, 0.82f, 0.64f));
        var exitMat = Material("GridExit", new Color(0.25f, 0.7f, 0.45f));
        for (int row = 0; row < 20; row++) for (int x = 0; x < 20; x++)
        {
            float height = Mission6_Grid_Level_Jizhijin.Height(x, row);
            int ramp = Mission6_Grid_Level_Jizhijin.RampDirection(x, row);
            var tile = Cube("Cell_" + x.ToString("00") + "_" + row.ToString("00"), grid.transform,
                Mission6_Grid_Level_Jizhijin.Center(x, row, -0.1f), new Vector3(2.37f, 0.2f, 2.37f),
                height > 0 ? raised : ((x + row) % 2 == 0 ? floorA : floorB));
            if (ramp != 0)
            {
                tile.transform.position = Mission6_Grid_Level_Jizhijin.Center(x, row, 0.08f);
                tile.transform.rotation = Quaternion.Euler(0, 0, ramp * Mathf.Atan(0.36f / 2.4f) * Mathf.Rad2Deg);
                tile.transform.localScale = new Vector3(2.43f, 0.2f, 2.37f);
                tile.GetComponent<Renderer>().sharedMaterial = slope;
            }
            if (Mission6_Grid_Level_Jizhijin.IsWall(x, row))
            {
                float wallHeight = row == 4 && x < 4 ? 6.5f : 3.6f;
                Cube("Wall_" + x + "_" + row, grid.transform, Mission6_Grid_Level_Jizhijin.Center(x, row, wallHeight / 2), new Vector3(2.4f, wallHeight, 2.4f), wall);
            }
        }
        float width = 20 * Mission6_Grid_Level_Jizhijin.CellSize;
        Cube("BoundaryWest", grid.transform, new Vector3(-0.2f, 1.8f, width / 2), new Vector3(0.4f, 3.6f, width + 0.8f), wall);
        Cube("BoundaryEast", grid.transform, new Vector3(width + 0.2f, 1.8f, width / 2), new Vector3(0.4f, 3.6f, width + 0.8f), wall);
        Cube("BoundarySouth", grid.transform, new Vector3(width / 2, 1.8f, -0.2f), new Vector3(width, 3.6f, 0.4f), wall);
        Cube("BoundaryNorth", grid.transform, new Vector3(width / 2, 1.8f, width + 0.2f), new Vector3(width, 3.6f, 0.4f), wall);
        var sunlight = new GameObject("Sunlight_NoFlare_Jizhijin", typeof(Light));
        sunlight.transform.SetParent(grid.transform); sunlight.transform.rotation = Quaternion.Euler(55, -25, 0);
        var light = sunlight.GetComponent<Light>(); light.type = LightType.Directional;
        light.color = new Color(1, 0.94f, 0.84f); light.intensity = 1.1f; light.shadows = LightShadows.Soft;

        foreach (var pair in Mission6_Grid_Level_Jizhijin.Crossword)
        {
            var p = pair.Key; var data = pair.Value;
            grid.transform.Find("Cell_" + p.x.ToString("00") + "_" + p.y.ToString("00")).GetComponent<Renderer>().sharedMaterial = wordFloor;
            var glyph = WorldText("CrosswordCell_" + p.x + "_" + p.y, grid.transform,
                Mission6_Grid_Level_Jizhijin.Center(p.x, p.y, 0.025f), data.missing ? "_" : data.answer.ToString(), 11);
            glyph.transform.rotation = Quaternion.Euler(90, 0, 0);
            glyph.color = data.missing ? new Color(0.8f, 0.3f, 0.05f) : new Color(0.06f, 0.17f, 0.19f);
            if (data.number > 0)
            {
                var number = WorldText("CrosswordNumber_" + data.number, grid.transform,
                    Mission6_Grid_Level_Jizhijin.Center(p.x, p.y, 0.03f) + new Vector3(-0.8f, 0, 0.8f), data.number.ToString(), 2.5f);
                number.transform.rotation = Quaternion.Euler(90, 0, 0);
                number.color = new Color(0.1f, 0.2f, 0.2f);
            }
        }
        for (int i = 0; i < Mission6_Grid_Level_Jizhijin.LetterCells.Length; i++)
        {
            var cell = Mission6_Grid_Level_Jizhijin.LetterCells[i];
            var go = new GameObject("Pickup_" + Mission6_Grid_Level_Jizhijin.PickupLetters[i] + "_" + i);
            go.transform.SetParent(grid.transform); go.transform.position = Mission6_Grid_Level_Jizhijin.Center(cell.x, cell.y, 1.2f);
            var collider = go.AddComponent<BoxCollider>(); collider.isTrigger = true; collider.size = new Vector3(1.15f, 2.3f, 1.15f);
            var pickup = go.AddComponent<Mission6_LetterPickup_Collectible_Jizhijin>();
            pickup.letter = Mission6_Grid_Level_Jizhijin.PickupLetters[i].ToString(); pickup.inventory = inventory;
            var label = WorldText("LetterVisual", go.transform, go.transform.position, pickup.letter, 12);
            label.color = new Color(1, 0.8f, 0.2f); label.gameObject.AddComponent<FaceCamera>();
            pickup.visual = label.transform; go.AddComponent<FloatingItem>().height = 0.12f;
        }

        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        var bag = Panel("LetterBag_Jizhijin", canvas.transform, new Color(0.035f, 0.07f, 0.09f, 0.94f), false);
        var bagRect = bag.GetComponent<RectTransform>(); bagRect.anchorMin = bagRect.anchorMax = new Vector2(0.5f, 0);
        bagRect.anchoredPosition = new Vector2(0, 55); bagRect.sizeDelta = new Vector2(700, 76);
        inventory.inventoryText = Label("InventoryText", bag.transform, "", 23, Vector2.zero, new Vector2(670, 70)); inventory.Refresh();
        level.explorationBag = bag;
        var status = Label("PhotoStatus_Jizhijin", canvas.transform, "", 19, Vector2.zero, new Vector2(600, 32));
        status.rectTransform.anchorMin = status.rectTransform.anchorMax = new Vector2(0.5f, 1); status.rectTransform.anchoredPosition = new Vector2(0, -24);
        level.photoHint = status.gameObject;
        level.objectiveText = Label("Objective_Jizhijin", canvas.transform, "", 17, Vector2.zero, new Vector2(850, 28));
        level.objectiveText.rectTransform.anchorMin = level.objectiveText.rectTransform.anchorMax = new Vector2(0.5f, 1);
        level.objectiveText.rectTransform.anchoredPosition = new Vector2(0, -53);
        watch.hintText = Label("WatchHint_Jizhijin", canvas.transform, "E: WATCH    Z: MAP PHOTO (ONCE, 5s)", 21, new Vector2(0, 270), new Vector2(850, 40));
        watch.hintText.gameObject.SetActive(false);
        var map = Mission6_GridMap_Jizhijin.Create();
        var photo = player.GetComponent<Mission6_Photo_Player_Jizhijin>();
        photo.statusText = status; photo.moving = player; photo.level = level;
        photo.photoPanel = MapPanel("PhotoPanel_Jizhijin", canvas.transform, map, out var photoTitle);
        photo.countdownText = photoTitle; photo.photoPanel.SetActive(false);
        var board = new GameObject("PlanBoard_Jizhijin", typeof(RectTransform), typeof(Canvas));
        board.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        board.transform.SetParent(grid.transform, false);
        board.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 800);
        board.GetComponent<RectTransform>().anchoredPosition = new Vector2(4.8f, 3.35f);
        board.transform.position = new Vector3(4.8f, 3.35f, 9.53f); board.transform.localScale = Vector3.one * 0.01f;
        var boardPanel = MapPanel("PlanPanel_Jizhijin", board.transform, map, out var planTitle);
        level.planPanel = board; level.planCountdown = level.objectiveText;
        planTitle.gameObject.SetActive(false);
        planTitle.text = "PLAN 30s / LEAVE THROUGH THE OPENING"; planTitle.fontSize = 23;
        Label("PlanHint", boardPanel.transform, "WASD: MOVE / LOOK AT THE MAP / LEAVE EARLY TO START", 17, new Vector2(0, -315), new Vector2(780, 32));
        Label("MapLegend", boardPanel.transform, "GOLD LETTERS: 9 PICKUPS / FILL ONLY 4 SHARED BLANKS", 15, new Vector2(0, -287), new Vector2(780, 28));
        var startExit = new GameObject("PlanExit_Jizhijin", typeof(BoxCollider), typeof(Mission6_PlanExit_Trigger_Jizhijin));
        startExit.transform.SetParent(grid.transform); startExit.transform.position = new Vector3(10.5f, 2, 3.6f);
        startExit.GetComponent<BoxCollider>().isTrigger = true; startExit.GetComponent<BoxCollider>().size = new Vector3(0.4f, 5, 2.4f);
        startExit.GetComponent<Mission6_PlanExit_Trigger_Jizhijin>().level = level;
        var leaveSign = WorldText("LeaveStartRoom", grid.transform, new Vector3(9.25f, 3.2f, 3.6f), "LEAVE TO START ->", 4);
        leaveSign.rectTransform.sizeDelta = new Vector2(6, 1);
        leaveSign.gameObject.AddComponent<FaceCamera>();

        var death = Panel("DeathPanel_Jizhijin", canvas.transform, new Color(0.02f, 0.04f, 0.06f, 1), true);
        Label("DeathTitle", death.transform, "TIME IS UP", 42, new Vector2(0, 70), new Vector2(800, 60));
        var retry = Button("Restart", death.transform, "TRY AGAIN", new Vector2(0, -30));
        UnityEventTools.AddPersistentListener(retry.onClick, watch.RestartGame); watch.deathPanel = death; death.SetActive(false);
        var exitPanel = CreatePuzzle(canvas.transform, template.GetComponent<TMP_Dropdown>(), inventory);
        var exit = new GameObject("GridExit_Jizhijin", typeof(BoxCollider), typeof(ExitTrigger)); exit.transform.SetParent(grid.transform);
        exit.transform.position = Mission6_Grid_Level_Jizhijin.Center(18, 18, 1.7f);
        var exitCollider = exit.GetComponent<BoxCollider>(); exitCollider.isTrigger = true; exitCollider.size = new Vector3(2.2f, 3.4f, 2.2f);
        var exitTrigger = exit.GetComponent<ExitTrigger>(); exitTrigger.exitPanel = exitPanel.gameObject;
        exitTrigger.moving = player; exitTrigger.mouseLook = level.mouseLook; exitTrigger.headBob = level.headBob; exitTrigger.pocketWatch = watch;
        var back = Button("BackToGrid", exitPanel.transform, "BACK TO GRID", new Vector2(-145, -307));
        UnityEventTools.AddPersistentListener(back.onClick, exitTrigger.ClosePanel);
        exitPanel.gameObject.SetActive(false);
        Cube("ExitPad", grid.transform, Mission6_Grid_Level_Jizhijin.Center(18, 18, 0.02f), new Vector3(2.3f, 0.04f, 2.3f), exitMat);
        var exitLabel = WorldText("ExitSign", grid.transform, Mission6_Grid_Level_Jizhijin.Center(18, 18, 3), "EXIT", 10);
        exitLabel.gameObject.AddComponent<FaceCamera>();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Selection.activeGameObject = grid;
    }

    static Material Material(string name, Color color)
    {
        string path = Root + "/Materials/Mission6_" + name + "_Jizhijin.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.color = color; mat.SetFloat("_Smoothness", 0.05f); EditorUtility.SetDirty(mat); return mat;
    }
    static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name;
        cube.transform.SetParent(parent); cube.transform.position = position; cube.transform.localScale = size;
        cube.GetComponent<Renderer>().sharedMaterial = material; return cube;
    }
    static TextMeshPro WorldText(string name, Transform parent, Vector3 position, string text, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro)); go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshPro>(); label.rectTransform.sizeDelta = new Vector2(2.2f, 2.2f);
        label.transform.position = position; label.rectTransform.anchoredPosition = new Vector2(position.x - parent.position.x, position.y - parent.position.y);
        label.text = text; label.fontSize = size; label.alignment = TextAlignmentOptions.Center; label.color = Color.white;
        label.margin = Vector4.zero; label.raycastTarget = false; return label;
    }
    static GameObject Panel(string name, Transform parent, Color color, bool full)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        if (full) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero; }
        go.GetComponent<Image>().color = color; return go;
    }
    static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Vector2 position, Vector2 dimensions)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>(); label.text = text; label.fontSize = size;
        label.color = Color.white; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.rectTransform.anchoredPosition = position; label.rectTransform.sizeDelta = dimensions; return label;
    }
    static Button Button(string name, Transform parent, string text, Vector2 position)
    {
        var go = Panel(name, parent, new Color(0.1f, 0.37f, 0.43f, 1), false);
        var rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(260, 44);
        var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
        Label("Label", go.transform, text, 21, Vector2.zero, new Vector2(250, 42)); return button;
    }
    static GameObject MapPanel(string name, Transform parent, Texture2D map, out TMP_Text title)
    {
        var panel = Panel(name, parent, new Color(0.025f, 0.05f, 0.065f, 1), true);
        var image = new GameObject("GridMapImage", typeof(RectTransform), typeof(RawImage)); image.transform.SetParent(panel.transform, false);
        image.GetComponent<RectTransform>().sizeDelta = new Vector2(555, 555); image.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 4);
        image.GetComponent<RawImage>().texture = map; image.GetComponent<RawImage>().raycastTarget = false;
        title = Label("MapTitle", panel.transform, "MAP PHOTO / 5s", 29, new Vector2(0, 318), new Vector2(1000, 45));
        return panel;
    }
    static Mission6_Spelling_ExitPanel_Jizhijin CreatePuzzle(Transform parent, TMP_Dropdown template, Mission6_Letters_GameManager_Jizhijin inventory)
    {
        var panel = Panel("CrosswordPanel_Jizhijin", parent, new Color(0.025f, 0.05f, 0.065f, 1), true);
        var puzzle = panel.AddComponent<Mission6_Spelling_ExitPanel_Jizhijin>(); puzzle.inventory = inventory;
        Label("PuzzleTitle", panel.transform, "COMPLETE THE CROSSWORD", 35, new Vector2(0, 292), new Vector2(1150, 55));
        Label("PuzzleHint", panel.transform, "4 blank squares / Choose the letters you need from 9 pickups", 20, new Vector2(0, 245), new Vector2(1100, 35));
        puzzle.bagText = Label("CollectedLetters", panel.transform, "", 17, new Vector2(0, 211), new Vector2(1100, 28));
        puzzle.bagText.color = new Color(1, 0.86f, 0.5f);
        var slots = new List<TMP_Dropdown>(); var cells = new List<Vector2Int>();
        const float size = 49;
        foreach (var pair in Mission6_Grid_Level_Jizhijin.Crossword.OrderByDescending(p => p.Key.y).ThenBy(p => p.Key.x))
        {
            var p = pair.Key; var data = pair.Value;
            var position = new Vector2(-350 + (p.x - 6) * size, 167 - (17 - p.y) * size);
            var square = Panel("Square_" + p.x + "_" + p.y, panel.transform, Color.white, false);
            square.GetComponent<RectTransform>().anchoredPosition = position;
            square.GetComponent<RectTransform>().sizeDelta = new Vector2(size - 1, size - 1);
            if (data.missing)
            {
                var slot = Object.Instantiate(template, square.transform); slot.name = "Blank_" + p.x + "_" + p.y;
                slot.gameObject.SetActive(true); slot.onValueChanged = new TMP_Dropdown.DropdownEvent();
                var rect = slot.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(0, -3); rect.sizeDelta = new Vector2(46, 36);
                slot.ClearOptions(); slot.AddOptions(new List<string> { "-" }); slot.captionText.fontSize = 24;
                slot.captionText.alignment = TextAlignmentOptions.Center;
                slot.captionText.rectTransform.anchorMin = Vector2.zero; slot.captionText.rectTransform.anchorMax = Vector2.one;
                slot.captionText.rectTransform.offsetMin = new Vector2(1, 0); slot.captionText.rectTransform.offsetMax = new Vector2(-12, 0);
                slot.GetComponent<Image>().color = new Color(1, 0.9f, 0.65f);
                ConfigureLetterMenu(slot);
                slots.Add(slot); cells.Add(p);
            }
            else
            {
                var fixedLetter = Label("GivenLetter", square.transform, data.answer.ToString(), 28, new Vector2(0, -3), new Vector2(44, 40));
                fixedLetter.color = new Color(0.06f, 0.16f, 0.2f);
            }
            if (data.number > 0)
            {
                var number = Label("ClueNumber", square.transform, data.number.ToString(), 12, new Vector2(-16, 16), new Vector2(15, 14));
                number.color = Color.black;
            }
        }
        var labels = new TMP_Text[Mission6_Grid_Level_Jizhijin.Words.Length];
        Label("AcrossTitle", panel.transform, "ACROSS", 25, new Vector2(245, 174), new Vector2(390, 35));
        Label("DownTitle", panel.transform, "DOWN", 25, new Vector2(245, -10), new Vector2(390, 35));
        int across = 0, down = 0;
        for (int w = 0; w < Mission6_Grid_Level_Jizhijin.Words.Length; w++)
        {
            var clue = Mission6_Grid_Level_Jizhijin.Words[w];
            float y = clue.down ? -48 - 34 * down++ : 136 - 34 * across++;
            labels[w] = Label("Clue_" + w, panel.transform, clue.number + ". " + clue.hint, 19, new Vector2(245, y), new Vector2(420, 32));
            labels[w].alignment = TextAlignmentOptions.Left;
        }
        puzzle.letterSlots = slots.ToArray(); puzzle.slotCells = cells.ToArray(); puzzle.wordLabels = labels;
        puzzle.resultText = Label("Result", panel.transform, "", 21, new Vector2(0, -205), new Vector2(1120, 46));
        var submit = Button("SubmitButton", panel.transform, "CHECK CROSSWORD", new Vector2(0, -257));
        UnityEventTools.AddPersistentListener(submit.onClick, puzzle.CheckAnswer);
        var replay = Button("Replay", panel.transform, "PLAY AGAIN", new Vector2(145, -307));
        UnityEventTools.AddPersistentListener(replay.onClick, puzzle.PlayAgain);
        puzzle.playAgainButton = replay.gameObject; replay.gameObject.SetActive(false);
        return puzzle;
    }
    static void ConfigureLetterMenu(TMP_Dropdown slot)
    {
        // The small crossword button and its expanded menu need independent widths.
        // A full bag has nine letters plus the empty option; show all ten rows.
        if (slot.template != null) Object.DestroyImmediate(slot.template.gameObject);
        var popup = Panel("LetterMenu", slot.transform, new Color(0.96f, 0.97f, 0.94f, 1), false);
        var rect = popup.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(132, 260);
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(popup.transform, false);
        var viewRect = viewport.GetComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero; viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = new Vector2(4, 0); viewRect.offsetMax = new Vector2(-4, 0);
        var content = new GameObject("Content", typeof(RectTransform)); content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 1); contentRect.sizeDelta = new Vector2(0, 26);
        var item = Panel("Item", content.transform, Color.white, false);
        var itemRect = item.GetComponent<RectTransform>();
        itemRect.anchorMin = Vector2.zero; itemRect.anchorMax = Vector2.right;
        itemRect.anchoredPosition = new Vector2(0, 13); itemRect.sizeDelta = new Vector2(0, 26);
        var toggle = item.AddComponent<Toggle>(); toggle.targetGraphic = item.GetComponent<Image>();
        var colors = toggle.colors; colors.highlightedColor = new Color(1, 0.89f, 0.6f); colors.selectedColor = colors.highlightedColor; toggle.colors = colors;
        var check = Label("Selected", item.transform, ">", 18, Vector2.zero, new Vector2(20, 26));
        check.color = new Color(0.1f, 0.25f, 0.28f);
        check.rectTransform.anchorMin = check.rectTransform.anchorMax = new Vector2(0, 0.5f); check.rectTransform.anchoredPosition = new Vector2(12, 0);
        toggle.graphic = check;
        var text = Label("Letter", item.transform, "-", 21, Vector2.zero, new Vector2(80, 26));
        text.color = new Color(0.06f, 0.16f, 0.2f); text.alignment = TextAlignmentOptions.Left;
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(28, 0); text.rectTransform.offsetMax = new Vector2(-8, 0);
        var scroll = popup.AddComponent<ScrollRect>(); scroll.content = contentRect; scroll.viewport = viewRect;
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        slot.template = rect; slot.itemText = text; slot.itemImage = null; slot.alphaFadeSpeed = 0;
        popup.SetActive(false);
    }
}
