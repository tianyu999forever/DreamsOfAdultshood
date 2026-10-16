using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class Mission6_Verify_Jizhijin
{
    const string Flag = "Mission6_Jizhijin.Verify";
    const string Out = "docs/Mission6_Jizhijin/";
    static readonly List<string> results = new List<string>();
    static readonly List<string> errors = new List<string>();
    static int stage, pickup, menuSlot;
    static float next, timeout, photoStarted, planStarted, walkingX;
    static string timerBefore;
    static bool previousBackground;
    static Keyboard keyboard;
    static Mission6_Grid_Level_Jizhijin level;
    static Mission6_Photo_Player_Jizhijin photo;
    static Mission6_Letters_GameManager_Jizhijin inventory;
    static Mission6_Spelling_ExitPanel_Jizhijin puzzle;
    static Moving player;
    static Mission6_Verify_Jizhijin()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += Changed;
        Application.logMessageReceived += (message, trace, type) =>
        {
            if (SessionState.GetBool(Flag, false) && (type == LogType.Exception || type == LogType.Assert || type == LogType.Error)) errors.Add(message + "\n" + trace);
        };
    }
    [MenuItem("Mission6 Jizhijin/Verify gameplay")]
    public static void Start()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(Flag, true);
        SessionState.SetString(Flag + ".previous", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Mission6_Setup_Jizhijin.ScenePath);
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange change)
    {
        if (!SessionState.GetBool(Flag, false)) return;
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            stage = 0; next = Time.realtimeSinceStartup + 1; timeout = Time.realtimeSinceStartup + 80;
            previousBackground = Application.runInBackground; Application.runInBackground = true;
        }
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Flag + ".previous", ""));
            SessionState.SetBool(Flag, false); Write();
        }
    }
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Write() => File.WriteAllLines(Out + "verification.txt", results.Concat(errors.Select(x => "RUNTIME ERROR: " + x)));
    static void Check(bool ok, string name)
    { results.Add((ok ? "PASS: " : "FAIL: ") + name); Write(); if (!ok) throw new Exception(name); }
    static void Capture(string name) => ScreenCapture.CaptureScreenshot(Path.GetFullPath(Out + name + ".png"));
    static void Key(params UnityEngine.InputSystem.Key[] keys)
    { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); }
    static void Finish()
    {
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        Time.timeScale = 1; Application.runInBackground = previousBackground;
        results.Add(errors.Count == 0 ? "PASS: no runtime errors" : "FAIL: runtime errors recorded");
        Write(); stage = 100; EditorApplication.ExitPlaymode();
    }
    static void ValidateLayout()
    {
        var grid = level.transform;
        var map = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mission6_Jizhijin/Materials/Mission6_GridMap_Jizhijin.png");
        Check(map.width == 1000 && map.height == 1000, "map import preserves pixel dimensions without rescaling");
        bool cells = true, walls = true, pixels = true;
        for (int row = 0; row < 20; row++) for (int x = 0; x < 20; x++)
        {
            var tile = grid.Find("Cell_" + x.ToString("00") + "_" + row.ToString("00"));
            cells &= tile != null && Mathf.Abs(tile.position.x - (x + 0.5f) * 2.4f) < 0.01f && Mathf.Abs(tile.position.z - (row + 0.5f) * 2.4f) < 0.01f;
            bool wall = grid.Find("Wall_" + x + "_" + row) != null;
            walls &= wall == Mission6_Grid_Level_Jizhijin.IsWall(x, row);
            var pixel = map.GetPixel(Mission6_GridMap_Jizhijin.Margin + x * 44 + 1, Mission6_GridMap_Jizhijin.Margin + row * 44 + 1);
            var expected = Mission6_Grid_Level_Jizhijin.Crossword.ContainsKey(new Vector2Int(x, row)) ? Mission6_GridMap_Jizhijin.WallColor : Mission6_GridMap_Jizhijin.TerrainColor(x, row);
            pixels &= Mathf.Abs(pixel.r - expected.r) < 0.015f && Mathf.Abs(pixel.g - expected.g) < 0.015f && Mathf.Abs(pixel.b - expected.b) < 0.015f;
        }
        Check(cells, "exactly 20 x 20 floor cells at matching world coordinates");
        Check(walls, "scene walls match the 20 x 20 layout");
        Check(pixels, "map terrain colors match every one of the 400 cells");
        var seen = new HashSet<Vector2Int> { Mission6_Grid_Level_Jizhijin.StartCell };
        var queue = new Queue<Vector2Int>(); queue.Enqueue(Mission6_Grid_Level_Jizhijin.StartCell);
        var dirs = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var dir in dirs)
            {
                var n = cell + dir;
                if (!Mission6_Grid_Level_Jizhijin.IsWall(n.x, n.y) && seen.Add(n)) queue.Enqueue(n);
            }
        }
        Check(Mission6_Grid_Level_Jizhijin.LetterCells.All(seen.Contains) && seen.Contains(Mission6_Grid_Level_Jizhijin.ExitCell), "all letter cells and exit reachable from start");
        Check(Mission6_Grid_Level_Jizhijin.Words.All(w => Enumerable.Range(0, w.mask.Length).All(i => seen.Contains(w.CellAt(i)))), "all Across and Down word cells reachable");
        var missing = Mission6_Grid_Level_Jizhijin.BlankCells.Select(p => Mission6_Grid_Level_Jizhijin.Crossword[p].answer).ToArray();
        Check(missing.Length == 4 && missing.Distinct().Count() == 4 && missing.All(c => Mission6_Grid_Level_Jizhijin.PickupLetters.Contains(c)), "only four shared blanks require four available letters");
        Check(FindObjects().Length == 9 && Mission6_Grid_Level_Jizhijin.PickupLetters.Distinct().Count() == 9, "map contains nine distinct physical pickups including five distractors");
        bool badges = true;
        for (int i = 0; i < Mission6_Grid_Level_Jizhijin.LetterCells.Length; i++)
        {
            var cell = Mission6_Grid_Level_Jizhijin.LetterCells[i];
            var tile = FindObjects().Single(t => t.name.EndsWith("_" + i));
            badges &= tile.GetComponentInChildren<TMP_Text>().text == Mission6_Grid_Level_Jizhijin.PickupLetters[i].ToString();
            badges &= Vector2.Distance(new Vector2(tile.transform.position.x, tile.transform.position.z), new Vector2((cell.x + 0.5f) * 2.4f, (cell.y + 0.5f) * 2.4f)) < 0.01f;
            int ink = 0;
            for (int y = 8; y < 36; y++) for (int x = 12; x < 32; x++)
                if (map.GetPixel(Mission6_GridMap_Jizhijin.Margin + cell.x * 44 + x, Mission6_GridMap_Jizhijin.Margin + cell.y * 44 + y).r < 0.4f) ink++;
            badges &= ink > 20;
        }
        Check(badges, "all nine map badges contain letter glyphs at matching physical pickup coordinates");
        Check(Mission6_Grid_Level_Jizhijin.Crossword.Any(p => p.Value.missing && p.Value.words.Count > 1), "crossword contains missing letters shared by Across and Down");
        Check(Mission6_Grid_Level_Jizhijin.Crossword.Count < Mission6_Grid_Level_Jizhijin.Words.Sum(w => w.word.Length), "crossing words share physical cells");
        bool physics = true;
        foreach (var cell in new[] { new Vector2Int(3, 14), new Vector2Int(16, 10), new Vector2Int(1, 14), new Vector2Int(7, 4), new Vector2Int(5, 4) })
        {
            bool hit = Physics.Raycast(Mission6_Grid_Level_Jizhijin.Center(cell.x, cell.y, 9), Vector3.down, out var info, 15, ~0, QueryTriggerInteraction.Ignore);
            float expected = Mission6_Grid_Level_Jizhijin.IsWall(cell.x, cell.y) ? 3.6f : Mission6_Grid_Level_Jizhijin.Height(cell.x, cell.y) + (Mission6_Grid_Level_Jizhijin.RampDirection(cell.x, cell.y) != 0 ? 0.18f : 0);
            physics &= hit && Mathf.Abs(info.point.y - expected) < 0.03f;
        }
        Check(physics, "physical colliders have correct wall, passage, raised floor and ramp heights");
    }
    static void Tick()
    {
        if (File.Exists("Temp/Mission6_Jizhijin.verify") && !File.Exists("Temp/Mission6_Jizhijin.grid") && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
        { File.Delete("Temp/Mission6_Jizhijin.verify"); Start(); return; }
        if (!SessionState.GetBool(Flag, false) || !EditorApplication.isPlaying || stage == 100 || Time.realtimeSinceStartup < next) return;
        try
        {
            if (Time.realtimeSinceStartup > timeout) throw new TimeoutException();
            switch (stage)
            {
                case 0:
                    level = Find<Mission6_Grid_Level_Jizhijin>(); photo = Find<Mission6_Photo_Player_Jizhijin>();
                    inventory = Find<Mission6_Letters_GameManager_Jizhijin>(); player = Find<Moving>();
                    puzzle = UnityEngine.Object.FindObjectsByType<Mission6_Spelling_ExitPanel_Jizhijin>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
                    Check(level.IsPlanning && level.planPanel.activeSelf && level.planPanel.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace, "PLAN map is a board in the starting room");
                    Check(player.enabled && level.mouseLook.enabled && !player.GetComponent<Rigidbody>().isKinematic, "movement, mouse look, and physics remain enabled during PLAN");
                    Check(!level.explorationBag.activeSelf && !level.photoHint.activeSelf, "PLAN map is clear of exploration HUD overlays");
                    Check(!photo.TryUsePhoto() && !photo.Used, "moving during PLAN does not consume the photo");
                    Check(!RenderSettings.fog && !UnityEngine.Object.FindObjectsByType<LensFlareComponentSRP>(FindObjectsSortMode.None).Any(f => f.enabled), "view remains free of fog and lens flares");
                    ValidateLayout(); keyboard = InputSystem.AddDevice<Keyboard>();
                    level.mouseLook.enabled = false; level.headBob.enabled = false;
                    Capture("letters-01-plan-room");
                    player.GetComponent<Rigidbody>().rotation = Quaternion.Euler(0, 90, 0); Camera.main.transform.localRotation = Quaternion.Euler(-7, 0, 0);
                    stage = 17; next = Time.realtimeSinceStartup + 0.2f; break;
                case 17:
                    CheckSignFacing("starting-room sign faces the player from inside"); Capture("letters-02-sign-inside");
                    stage = 20; next = Time.realtimeSinceStartup + 0.2f; break;
                case 20:
                    player.GetComponent<Rigidbody>().rotation = Quaternion.identity; Camera.main.transform.localRotation = Quaternion.identity;
                    planStarted = level.PlanTimeLeft;
                    walkingX = player.transform.position.z; timerBefore = level.watch.timerText.text;
                    Key(UnityEngine.InputSystem.Key.W); stage = 1; next = Time.realtimeSinceStartup + 0.6f; break;
                case 1:
                    Key(); Check(level.IsPlanning && player.transform.position.z > walkingX + 0.5f, "WASD physically moves the player within the PLAN room");
                    Check(level.PlanTimeLeft < planStarted && level.watch.timerText.text == timerBefore, "PLAN ticks while the exploration timer waits");
                    player.GetComponent<Rigidbody>().position = new Vector3(8.3f, 1.4f, 3.6f); Physics.SyncTransforms();
                    Camera.main.transform.localRotation = Quaternion.identity;
                    Key(UnityEngine.InputSystem.Key.D); stage = 2; next = Time.realtimeSinceStartup + 0.8f; break;
                case 2:
                    Key(); Check(!level.IsPlanning && level.PlanTimeLeft > 0 && !level.planPanel.activeSelf, "walking through the opening ends PLAN early and hides its map");
                    Check(player.enabled && Time.timeScale == 1, "early departure keeps movement active");
                    Check(level.explorationBag.activeSelf && level.photoHint.activeSelf, "exploration HUD returns when leaving PLAN");
                    player.GetComponent<Rigidbody>().rotation = Quaternion.Euler(0, 270, 0); Camera.main.transform.localRotation = Quaternion.Euler(-7, 0, 0);
                    stage = 18; next = Time.realtimeSinceStartup + 0.2f; break;
                case 18:
                    CheckSignFacing("starting-room sign faces the player from outside"); Capture("letters-03-sign-outside");
                    stage = 21; next = Time.realtimeSinceStartup + 0.2f; break;
                case 21:
                    player.GetComponent<Rigidbody>().rotation = Quaternion.identity; Camera.main.transform.localRotation = Quaternion.Euler(20, 0, 0);
                    stage = 3; next = Time.realtimeSinceStartup + 0.2f; break;
                case 3:
                    Capture("letters-04-clear-view"); timerBefore = level.watch.timerText.text;
                    stage = 4; next = Time.realtimeSinceStartup + 0.3f; break;
                case 4:
                    Key(UnityEngine.InputSystem.Key.Z); photo.SendMessage("Update"); Key(); photoStarted = Time.unscaledTime;
                    Check(photo.Used && photo.IsVisible, "Z opens the updated crossword map once");
                    stage = 5; next = Time.realtimeSinceStartup + 1; break;
                case 5:
                    Capture("letters-05-photo"); Key(UnityEngine.InputSystem.Key.Z); photo.SendMessage("Update"); Key();
                    Check(photo.IsVisible && !photo.TryUsePhoto(), "repeated Z cannot extend the photo");
                    stage = 6; next = Time.realtimeSinceStartup + Mathf.Max(0, 5.25f - (Time.unscaledTime - photoStarted)); break;
                case 6:
                    Check(!photo.IsVisible && !photo.TryUsePhoto(), "photo closes after five seconds and cannot reopen");
                    Check(level.watch.timerText.text != timerBefore, "exploration countdown continues during photo");
                    var rTile = FindObjects().Single(t => t.GetComponentInChildren<TMP_Text>().text == "R");
                    player.GetComponent<Rigidbody>().position = new Vector3(rTile.transform.position.x, 1.4f, rTile.transform.position.z - 3);
                    player.GetComponent<Rigidbody>().rotation = Quaternion.identity; Camera.main.transform.localRotation = Quaternion.Euler(25, 0, 0); Physics.SyncTransforms();
                    stage = 19; next = Time.realtimeSinceStartup + 0.2f; break;
                case 19:
                    var glyph = FindObjects().Single(t => t.GetComponentInChildren<TMP_Text>().text == "R").GetComponentInChildren<TMP_Text>();
                    Check(Vector3.Dot(-glyph.transform.forward, Camera.main.transform.position - glyph.transform.position) > 0, "asymmetric pickup glyph presents its front to the camera");
                    Capture("letters-06-pickup-facing");
                    stage = 22; next = Time.realtimeSinceStartup + 0.2f; break;
                case 22:
                    player.GetComponent<Rigidbody>().position = Mission6_Grid_Level_Jizhijin.Center(14, 10, 1.5f);
                    player.GetComponent<Rigidbody>().rotation = Quaternion.identity; Camera.main.transform.localRotation = Quaternion.identity;
                    Physics.SyncTransforms(); walkingX = player.GetComponent<Rigidbody>().position.x; Key(UnityEngine.InputSystem.Key.D);
                    stage = 7; next = Time.realtimeSinceStartup + 2.4f; break;
                case 7:
                    Key(); Check(player.transform.position.x > walkingX + 7, "raised terrain remains walkable without jumping");
                    pickup = 0; stage = 8; next = 0; break;
                case 8:
                    if (pickup == 4)
                    {
                        player.GetComponent<Rigidbody>().position = Mission6_Grid_Level_Jizhijin.Center(18, 18, 1.4f);
                        Physics.SyncTransforms(); stage = 10; next = Time.realtimeSinceStartup + 0.25f; break;
                    }
                    var needed = Mission6_Grid_Level_Jizhijin.Crossword[Mission6_Grid_Level_Jizhijin.BlankCells[pickup]].answer.ToString();
                    var tile = FindObjects().Single(t => t.GetComponentInChildren<TMP_Text>().text == needed && t.GetComponent<Collider>().enabled);
                    player.GetComponent<Rigidbody>().position = tile.transform.position;
                    Physics.SyncTransforms(); stage = 9; next = Time.realtimeSinceStartup + 0.2f; break;
                case 9:
                    Check(inventory.Letters.Count == pickup + 1, "letter pickup " + (pickup + 1) + " collected exactly once");
                    pickup++; stage = 8; next = 0; break;
                case 10:
                    Check(puzzle.gameObject.activeInHierarchy && Time.timeScale == 0, "physical exit opens the crossword");
                    Check(puzzle.nextLevelButton != null && puzzle.nextLevelButton.transform.parent == puzzle.transform && puzzle.nextLevelButton.GetComponentInParent<Canvas>() != null && puzzle.nextLevelButton.GetComponent<Image>().color.a == 1, "NEXT LEVEL belongs to the exit Canvas and has an opaque button image");
                    puzzle.NextLevel(); Check(!puzzle.nextLevelButton.activeSelf && SceneManager.GetActiveScene().path == Mission6_Setup_Jizhijin.ScenePath, "unsolved crossword cannot advance to another mission");
                    Check(puzzle.letterSlots.Length == 4 && puzzle.slotCells.Distinct().Count() == 4, "crossword has exactly four inputs with shared intersections counted once");
                    Check(FindObjects().Count(t => t.GetComponent<Collider>().enabled) == 5, "five distractor letters remain uncollected at the exit");
                    Check(puzzle.transform.Cast<Transform>().Count(t => t.name.StartsWith("Square_")) == Mission6_Grid_Level_Jizhijin.Crossword.Count, "UI grid preserves the reference crossword shape");
                    puzzle.CheckAnswer(); Check(!puzzle.Solved, "incomplete crossword cannot win");
                    puzzle.letterSlots[0].value = 1; puzzle.letterSlots[1].value = 1;
                    Check(puzzle.letterSlots[1].value == 0, "a physical tile cannot fill two different squares");
                    for (int i = 0; i < 4; i++) puzzle.letterSlots[i].SetValueWithoutNotify((i + 1) % 4 + 1);
                    puzzle.CheckAnswer(); Check(!puzzle.Solved, "incorrect crossword can be retried");
                    Capture("letters-07-retry"); stage = 11; next = Time.realtimeSinceStartup + 0.3f; break;
                case 11:
                    foreach (var existing in puzzle.letterSlots) existing.SetValueWithoutNotify(0);
                    var used = new HashSet<int>();
                    for (int i = 0; i < puzzle.slotCells.Length; i++)
                    {
                        string answer = Mission6_Grid_Level_Jizhijin.Crossword[puzzle.slotCells[i]].answer.ToString();
                        int index = Enumerable.Range(0, inventory.Letters.Count).First(j => !used.Contains(j) && inventory.Letters[j] == answer);
                        used.Add(index); puzzle.letterSlots[i].value = index + 1;
                    }
                    puzzle.transform.Find("SubmitButton").GetComponent<Button>().onClick.Invoke();
                    Check(puzzle.Solved && puzzle.resultText.text.Contains("8 / 8") && inventory.Letters.Count == 4, "only four collected letters complete all eight crossing words");
                    Check(puzzle.wordLabels.All(l => l.color.g > 0.9f), "shared answers validate both Across and Down");
                    Check(puzzle.nextLevelButton.activeInHierarchy && puzzle.nextLevelButton.GetComponent<Button>().onClick.GetPersistentMethodName(0) == "NextLevel", "correct crossword reveals the wired NEXT LEVEL button");
                    Capture("letters-08-success"); stage = 12; next = Time.realtimeSinceStartup + 0.4f; break;
                case 12:
                    int intersection = Array.IndexOf(puzzle.slotCells, new Vector2Int(9, 16));
                    int single = Array.IndexOf(puzzle.slotCells, new Vector2Int(11, 16));
                    int oldValue = puzzle.letterSlots[intersection].value;
                    puzzle.letterSlots[intersection].SetValueWithoutNotify(puzzle.letterSlots[single].value);
                    puzzle.letterSlots[single].SetValueWithoutNotify(oldValue);
                    puzzle.CheckAnswer();
                    Check(!puzzle.Solved && puzzle.resultText.text.StartsWith("5 / 8"), "changing a shared square affects both crossing words");
                    Check(!puzzle.nextLevelButton.activeSelf, "changing an answer hides NEXT LEVEL until the crossword is correct again");
                    foreach (var remaining in FindObjects().Where(t => t.GetComponent<Collider>().enabled)) inventory.AddLetter(remaining.letter);
                    puzzle.gameObject.SetActive(false); puzzle.gameObject.SetActive(true);
                    Check(inventory.Letters.Count == 9 && inventory.Letters.All(c => puzzle.bagText.text.Contains(c)), "crossword displays all nine collected letters in its bag summary");
                    menuSlot = 0; puzzle.letterSlots[menuSlot].Show(); stage = 23; next = Time.realtimeSinceStartup + 0.2f; break;
                case 23:
                    Canvas.ForceUpdateCanvases();
                    var menu = puzzle.letterSlots[menuSlot].transform.Find("Dropdown List");
                    var view = menu.GetComponent<ScrollRect>().viewport;
                    var items = menu.GetComponentsInChildren<Toggle>().OrderByDescending(t => t.transform.position.y).ToArray();
                    bool readable = items.Length == 10 && menu.GetComponentsInChildren<Scrollbar>().Length == 0;
                    for (int i = 0; i < items.Length; i++)
                    {
                        var label = items[i].transform.Find("Letter").GetComponent<TMP_Text>();
                        var corners = new Vector3[4]; label.rectTransform.GetWorldCorners(corners);
                        readable &= label.text == (i == 0 ? "-" : inventory.Letters[i - 1]);
                        readable &= label.preferredWidth < label.rectTransform.rect.width;
                        readable &= corners.All(c => { var local = view.InverseTransformPoint(c); return local.x >= view.rect.xMin - 0.1f && local.x <= view.rect.xMax + 0.1f && local.y >= view.rect.yMin - 0.1f && local.y <= view.rect.yMax + 0.1f; });
                    }
                    Check(readable, "blank " + (menuSlot + 1) + " shows all nine letter options without clipping or scrollbar overlap");
                    if (menuSlot < 3)
                    {
                        puzzle.letterSlots[menuSlot++].Hide(); puzzle.letterSlots[menuSlot].Show();
                        next = Time.realtimeSinceStartup + 0.2f; break;
                    }
                    Capture("letters-09-full-bag-menu"); stage = 24; next = Time.realtimeSinceStartup + 0.3f; break;
                case 24:
                    var last = puzzle.letterSlots[menuSlot].transform.Find("Dropdown List").GetComponentsInChildren<Toggle>().OrderBy(t => t.transform.position.y).First();
                    last.isOn = true;
                    Check(puzzle.letterSlots[menuSlot].captionText.text == inventory.Letters[8], "last letter in a full bag remains selectable");
                    foreach (var slot in puzzle.letterSlots) slot.SetValueWithoutNotify(0);
                    for (int i = 0; i < puzzle.slotCells.Length; i++)
                    {
                        string required = Mission6_Grid_Level_Jizhijin.Crossword[puzzle.slotCells[i]].answer.ToString();
                        puzzle.letterSlots[i].value = Enumerable.Range(0, inventory.Letters.Count).First(j => inventory.Letters[j] == required) + 1;
                    }
                    puzzle.CheckAnswer();
                    puzzle.nextSceneName = "MissingMission_Verification"; puzzle.NextLevel();
                    Check(Time.timeScale == 0 && SceneManager.GetActiveScene().path == Mission6_Setup_Jizhijin.ScenePath, "unavailable next mission preserves the paused crossword instead of throwing a load error");
                    puzzle.nextSceneName = "";
                    puzzle.nextLevelButton.GetComponent<Button>().onClick.Invoke();
                    stage = 25; next = Time.realtimeSinceStartup + 1; break;
                case 25:
                    Check(SceneManager.GetActiveScene().buildIndex == 1 && Time.timeScale == 1, "NEXT LEVEL loads the next scene in Build Settings and restores time");
                    SceneManager.LoadScene(Mission6_Setup_Jizhijin.ScenePath); stage = 13; next = Time.realtimeSinceStartup + 1; break;
                case 13:
                    level = Find<Mission6_Grid_Level_Jizhijin>(); photo = Find<Mission6_Photo_Player_Jizhijin>(); inventory = Find<Mission6_Letters_GameManager_Jizhijin>();
                    Check(level.IsPlanning && level.moving.enabled && !photo.Used && inventory.Letters.Count == 0, "restart restores movable PLAN and an empty bag");
                    level.BeginExploration(); Check(photo.TryUsePhoto(), "new round restores the one-use photo");
                    level.watch.PlayerDead(); stage = 14; next = Time.realtimeSinceStartup + 0.2f; break;
                case 14:
                    Check(!photo.IsVisible, "death closes photo while paused");
                    level.watch.RestartGame(); stage = 15; next = Time.realtimeSinceStartup + 1; break;
                case 15:
                    level = Find<Mission6_Grid_Level_Jizhijin>();
                    Check(level.IsPlanning, "new PLAN begins after death restart");
                    Time.timeScale = 30; stage = 16; next = Time.realtimeSinceStartup + 5; break;
                case 16:
                    Check(Time.timeScale == 0 && level.watch.deathPanel.activeSelf && !level.moving.enabled, "remaining in the room until PLAN expires causes the original failure");
                    Finish(); break;
            }
        }
        catch (Exception error) { results.Add("FAIL: " + error); Finish(); }
    }
    static Mission6_LetterPickup_Collectible_Jizhijin[] FindObjects() => UnityEngine.Object.FindObjectsByType<Mission6_LetterPickup_Collectible_Jizhijin>(FindObjectsSortMode.None);
    static void CheckSignFacing(string message)
    {
        var sign = level.transform.Find("LeaveStartRoom");
        Check(sign != null && Vector3.Dot(-sign.forward, Camera.main.transform.position - sign.position) > 0, message);
    }
}
