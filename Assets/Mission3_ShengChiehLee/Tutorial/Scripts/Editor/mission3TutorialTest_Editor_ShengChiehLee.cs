using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class mission3TutorialTest_Editor_ShengChiehLee
{
    const string Flag = "Mission3.Tutorial.Test";
    const string Request = "Library/Mission3TutorialTest.txt";
    const string CorePass = "PASS\nVerified mandatory entry and locked early exit; actual mouse look, physical WASD walking, Shift sprint, Space jump across barrier, aimed E pickup, map and journal open/close, ESC pause and UI resume, unlocked exit, return to reception, preserved inventory/notes/score and excluded training time; original hotel controls and guide restored; replay skips completed training only within the same session.\n";
    static Type TeammateLessonType => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("Tutorial_IceLesson_Tutorial_ZhuohengWu")).FirstOrDefault(type => type != null);
    static string Output => SessionState.GetString("Mission3.Tutorial.Output", Path.GetFullPath("Temp/Mission3TutorialValidation"));
    static int phase;
    static double began, entered;
    static mission3Tutorial_Manager_ShengChiehLee lesson;
    static mission3Hotel_Manager_ShengChiehLee manager;
    static Keyboard keyboard;
    static Mouse mouse;
    static bool projectError;
    static mission3TutorialTest_Editor_ShengChiehLee() { EditorApplication.update += Tick; }
    [MenuItem("Mission3/Test Mandatory Tutorial")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode before testing tutorial.");
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty) throw new Exception("Save the current scene before testing.");
        EditorSceneManager.OpenScene("Assets/Scenes/Mission3.unity");
        var gameType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameType != null) EditorWindow.GetWindow(gameType, false, "Game", true).Focus();
        Directory.CreateDirectory(Output); File.WriteAllText(Output + "/validation.txt", "RUNNING\n");
        SessionState.SetBool(Flag, true); phase = 0; began = entered = EditorApplication.timeSinceStartup;
        EditorApplication.isPaused = false; EditorApplication.EnterPlaymode();
    }
    static void Next(int value) { phase = value; entered = EditorApplication.timeSinceStartup; File.AppendAllText(Output + "/trace.txt", "Phase " + value + " at " + entered + "\n"); }
    static void Keys(params Key[] values) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(values)); }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (File.Exists("Library/Mission3TutorialStop.txt"))
        {
            File.Delete("Library/Mission3TutorialStop.txt");
            var currentRoom = UnityEngine.Object.FindFirstObjectByType<mission3Tutorial_Manager_ShengChiehLee>();
            var currentManager = UnityEngine.Object.FindFirstObjectByType<mission3Hotel_Manager_ShengChiehLee>();
            if (currentManager != null) File.WriteAllText(Output + "/interrupted-state.txt", "Room=" + (currentRoom != null) + "\nManagerEnabled=" + currentManager.enabled + "\nPage=" + currentManager.CurrentPage + "\nPosition=" + currentManager.playerBody.position + "\nLocalPosition=" + (currentRoom == null ? Vector3.zero : currentRoom.transform.InverseTransformPoint(currentManager.playerBody.position)) + "\nCapsule=" + currentManager.playerBody.GetComponent<Collider>().bounds.size + "\nKinematic=" + currentManager.playerBody.isKinematic + "\nVelocity=" + currentManager.playerBody.linearVelocity);
            if (currentRoom != null)
            {
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var bounds = currentManager.playerBody.GetComponent<Collider>().bounds;
                File.AppendAllText(Output + "/interrupted-state.txt", "\nStep=" + currentRoom.CurrentStep + "\nJumpObserved=" + currentRoom.GetType().GetField("jumped", flags).GetValue(currentRoom) + "\nJumpPressedAt=" + currentRoom.GetType().GetField("jumpPressedAt", flags).GetValue(currentRoom) + "\nNow=" + Time.unscaledTime + "\nGrounded=" + currentRoom.GetType().GetMethod("Grounded", flags).Invoke(currentRoom, null) + "\nBoundsCenter=" + bounds.center + "\nBoundsMin=" + bounds.min + "\nMovement=" + currentManager.movement.GetType().Name + "\nMovementEnabled=" + currentManager.movement.enabled + "\nPaused=" + currentRoom.Paused + "\nSpawn=" + currentRoom.spawn.position + "\nScale=" + Time.timeScale);
            }
            SessionState.SetBool(Flag, false); if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); return;
        }
        if (File.Exists(Request) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            var output = File.ReadAllText(Request).Trim(); File.Delete(Request);
            if (Path.IsPathRooted(output)) SessionState.SetString("Mission3.Tutorial.Output", output);
            Begin(); return;
        }
        if (File.Exists("Library/Mission3TutorialCapture.txt") && EditorApplication.isPlaying)
        {
            lesson = UnityEngine.Object.FindFirstObjectByType<mission3Tutorial_Manager_ShengChiehLee>();
            if (lesson == null || !lesson.Initialized) return;
            ScreenCapture.CaptureScreenshot(Path.Combine(Output, "03-start-room.png")); File.Delete("Library/Mission3TutorialCapture.txt"); return;
        }
        if (!SessionState.GetBool(Flag, false) || !EditorApplication.isPlaying) return;
        try
        {
            if (began == 0) began = entered = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - began > 100) throw new Exception("Tutorial test timed out at phase " + phase + ", step " + (lesson == null ? "missing" : lesson.CurrentStep.ToString()) + ", position " + (manager == null ? Vector3.zero : manager.playerBody.position));
            double age = EditorApplication.timeSinceStartup - entered;
            switch (phase)
            {
                case 0:
                    lesson = UnityEngine.Object.FindFirstObjectByType<mission3Tutorial_Manager_ShengChiehLee>();
                    if (lesson == null || !lesson.Initialized) return;
                    manager = lesson.Manager;
                    keyboard = InputSystem.AddDevice<Keyboard>("Mission3 tutorial test keyboard"); mouse = InputSystem.AddDevice<Mouse>("Mission3 tutorial test mouse");
                    Application.runInBackground = true; projectError = false; Application.logMessageReceived += Log;
                    Require(lesson.CurrentStep == mission3Tutorial_Manager_ShengChiehLee.Step.Welcome && !manager.enabled && !manager.canvasUI.gameObject.activeInHierarchy, "Hotel is not gated by the mandatory welcome.");
                    Require(lesson.doorCollider.enabled && !lesson.TryEnterHotel(), "Door or exit can bypass training.");
                    Require(manager.CollectedCount == 0 && manager.ElapsedSeconds < .1f, "Training changed starting inventory or timer.");
                    lesson.beginButton.onClick.Invoke(); Next(1); break;
                case 1:
                    if (age < .3) return;
                    Require(lesson.CurrentStep == mission3Tutorial_Manager_ShengChiehLee.Step.Look, "Start button skipped the look exercise.");
                    InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(150, 0) });
                    Next(2); break;
                case 2:
                    if (lesson.CurrentStep == mission3Tutorial_Manager_ShengChiehLee.Step.Look) { InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(150, 0) }); return; }
                    Require(lesson.CurrentStep == mission3Tutorial_Manager_ShengChiehLee.Step.Walk, "Mouse movement did not finish look exercise.");
                    FaceForward(); Capture("01-room-controls"); Keys(Key.Escape); Next(3); break;
                case 3:
                    if (age < .2) return;
                    Keys(); Require(lesson.Paused && manager.playerBody.isKinematic && !manager.movement.enabled, "ESC did not safely pause training.");
                    lesson.resumeButton.onClick.Invoke(); Require(!lesson.Paused && lesson.ControlsEnabled, "Resume button did not restore controls.");
                    Keys(Key.W); Next(4); break;
                case 4:
                    if (lesson.CurrentStep == mission3Tutorial_Manager_ShengChiehLee.Step.Walk) return;
                    Require(lesson.CurrentStep == mission3Tutorial_Manager_ShengChiehLee.Step.Sprint, "Real walking did not reach the marker.");
                    Keys(Key.W, Key.LeftShift); Next(5); break;
                case 5:
                    if (lesson.CurrentStep == mission3Tutorial_Manager_ShengChiehLee.Step.Sprint) return;
                    Require(lesson.CurrentStep == mission3Tutorial_Manager_ShengChiehLee.Step.Jump, "Running to the second marker did not finish sprint.");
                    Keys(Key.W, Key.Space); Next(6); break;
                case 6:
                    if (age < .12) return;
                    Keys(Key.W); Next(7); break;
                case 7:
                    if (lesson.CurrentStep == mission3Tutorial_Manager_ShengChiehLee.Step.Jump) return;
                    Require(lesson.CurrentStep == mission3Tutorial_Manager_ShengChiehLee.Step.Interact, "Jumping and landing beyond the barrier did not finish jump.");
                    Keys();
                    var interactionStart = lesson.transform.TransformPoint(new Vector3(0, lesson.spawn.localPosition.y, 4.9f));
                    manager.playerBody.position = interactionStart; manager.playerBody.transform.position = interactionStart; manager.playerBody.linearVelocity = Vector3.zero; Physics.SyncTransforms();
                    AimAtCard(); Next(8); break;
                case 8:
                    if (age < .25) return;
                    if (!lesson.promptLabel.text.StartsWith("E   ") && age < 2) { AimAtCard(); return; }
                    AimAtCard();
                    bool cardInView = Physics.Raycast(manager.playerCamera.transform.position, manager.playerCamera.transform.forward, out var viewHit, 3f, ~0, QueryTriggerInteraction.Collide) && viewHit.collider == lesson.practiceCollider;
                    if (!cardInView)
                    {
                        if (age < 2) return;
                        throw new Exception("Training card is not in interaction range: hit=" + (viewHit.collider == null ? "none" : viewHit.collider.name) + ", player=" + lesson.transform.InverseTransformPoint(manager.playerBody.position) + ", camera=" + lesson.transform.InverseTransformPoint(manager.playerCamera.transform.position) + ", distance=" + Vector3.Distance(manager.playerCamera.transform.position, lesson.practiceCollider.bounds.center));
                    }
                    Keys(Key.E); Next(9); break;
                case 9:
                    if (age < .2) return;
                    Keys(); Require(lesson.CurrentStep == mission3Tutorial_Manager_ShengChiehLee.Step.Tools && !lesson.practiceCard.gameObject.activeSelf, "Aimed E pickup did not collect the training card. Paused=" + lesson.Paused + ", prompt=" + lesson.promptLabel.text);
                    Require(manager.CollectedCount == 0 && !manager.notesRead.Any(x => x), "Practice pickup leaked into hotel inventory or notes.");
                    Keys(Key.M); Next(10); break;
                case 10:
                    if (age < .2) return;
                    Keys(); Require(lesson.toolsPanel.activeSelf && lesson.mapPanel.activeSelf && manager.playerBody.isKinematic, "M did not open and freeze the practice map.");
                    Require(!lesson.TryEnterHotel(), "Map exercise allowed an early exit.");
                    Next(101); break;
                case 101:
                    if (age < .08) return;
                    Keys(Key.M); Next(11); break;
                case 11:
                    if (age < .2) return;
                    Keys(); Require(lesson.MapPractised && !lesson.JournalPractised && lesson.doorCollider.enabled, "Map close or locked-door state incorrect: map=" + lesson.MapPractised + ", journal=" + lesson.JournalPractised + ", locked=" + lesson.doorCollider.enabled);
                    Keys(Key.Tab); Next(12); break;
                case 12:
                    if (age < .2) return;
                    Keys(); Require(lesson.journalPanel.activeSelf && manager.playerBody.isKinematic, "TAB did not open and freeze the practice journal.");
                    Next(102); break;
                case 102:
                    if (age < .08) return;
                    Keys(Key.Tab); Next(13); break;
                case 13:
                    if (age < .4) return;
                    Keys(); Require(lesson.CurrentStep == mission3Tutorial_Manager_ShengChiehLee.Step.Exit && !lesson.doorCollider.enabled, "Door did not unlock after all six exercises.");
                    Require(manager.ElapsedSeconds < .1f && !manager.enabled, "Hotel timer advanced during training.");
                    FaceForward(); Capture("02-exit-unlocked");
                    var exitStart = lesson.transform.TransformPoint(new Vector3(0, lesson.spawn.localPosition.y, 8));
                    manager.playerBody.position = exitStart; manager.playerBody.transform.position = exitStart; FaceForward(); Physics.SyncTransforms();
                    Keys(Key.W); Next(14); break;
                case 14:
                    if (!mission3Tutorial_Manager_ShengChiehLee.CompletedThisSession) return;
                    Keys(); Next(15); break;
                case 15:
                    if (age < .8) return;
                    Require(UnityEngine.Object.FindFirstObjectByType<mission3Tutorial_Manager_ShengChiehLee>() == null, "Room remained active after entering the hotel.");
                    Require(manager.CurrentPage == mission3Hotel_Manager_ShengChiehLee.Page.Welcome && manager.canvasUI.pages[1].activeInHierarchy && manager.playerBody.isKinematic && !manager.movement.enabled, "Hotel introduction did not appear after training.");
                    Require(manager.canvasUI.pages[1].transform.Find("Title").GetComponent<TMP_Text>().text == "Welcome to the HOTEL FLOOR 13!", "Hotel title was not preserved.");
                    Require(manager.canvasUI.pages[1].transform.Find("Brief").GetComponent<TMP_Text>().text.StartsWith("A note beside your keys reads:"), "Hotel story was not preserved.");
                    Require(manager.ElapsedSeconds < .1f, "Hotel timer advanced before accepting the introduction.");
                    Capture("03-hotel-introduction"); manager.canvasUI.Begin(); Next(151); break;
                case 151:
                    if (age < .25) return;
                    Require(manager.enabled && manager.movement.enabled && manager.mouseLook.enabled && manager.canvasUI.gameObject.activeSelf && !manager.playerBody.isKinematic, "Hotel controls or Canvas were not restored.");
                    Require(manager.CollectedCount == 0 && !manager.notesRead.Any(x => x) && manager.FinalScore == 0 && !manager.ReturnsSubmitted, "Hotel progress changed during tutorial.");
                    Require(manager.ElapsedSeconds < 2 && manager.guide != null && !manager.guide.MapViewed && !manager.guide.JournalViewed, "Training consumed hotel time or marked real clues as read.");
                    Require(!projectError, "A project runtime error occurred during tutorial.");
                    Next(16); SceneManager.LoadScene("Assets/Scenes/Mission3.unity"); break;
                case 16:
                    var fresh = UnityEngine.Object.FindFirstObjectByType<mission3Hotel_Manager_ShengChiehLee>();
                    if (fresh == null || fresh == manager || fresh.CurrentPage != mission3Hotel_Manager_ShengChiehLee.Page.Welcome) return;
                    Require(SceneManager.GetActiveScene().path == "Assets/Scenes/Mission3.unity", "Mission3 scene entry points to the wrong asset.");
                    Require(UnityEngine.Object.FindFirstObjectByType<mission3Tutorial_Manager_ShengChiehLee>() == null, "Retry repeated already completed training in the same session.");
                    // The teammate lesson exists on integration, but is optional on the Mission3 branch.
                    if (!File.Exists("Assets/Scenes/Tutorial.unity") || TeammateLessonType == null)
                    {
                        Finish(CorePass + "Optional teammate ice/fire tutorial check skipped: its scene or script is not present on this branch.\n"); break;
                    }
                    EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Tutorial.unity", new LoadSceneParameters(LoadSceneMode.Single)); Next(17); break;
                case 17:
                    if (SceneManager.GetActiveScene().path != "Assets/Scenes/Tutorial.unity" || age < .3) return;
                    Require(UnityEngine.Object.FindFirstObjectByType<mission3Tutorial_Manager_ShengChiehLee>() == null, "Mission3 gate appeared in the teammate tutorial.");
                    var teammateType = TeammateLessonType;
                    var other = Resources.FindObjectsOfTypeAll(teammateType).OfType<Behaviour>().FirstOrDefault(component => component.gameObject.scene == SceneManager.GetActiveScene());
                    Require(other != null && other.enabled && Equals(teammateType.GetProperty("Completed")?.GetValue(other), false) && Equals(teammateType.GetProperty("IsMelting")?.GetValue(other), false), "Teammate tutorial no longer starts normally.");
                    Finish(CorePass + "Verified teammate ice/fire tutorial still starts without the Mission3 room.\n"); break;
            }
        }
        catch (Exception error) { Finish("FAIL\nPhase " + phase + ", step " + (lesson == null ? "missing" : lesson.CurrentStep.ToString()) + "\n" + error); Debug.LogException(error); }
    }
    static void FaceForward() { manager.playerBody.rotation = Quaternion.identity; manager.playerBody.transform.rotation = Quaternion.identity; manager.playerCamera.transform.localRotation = Quaternion.identity; manager.playerCamera.GetComponent<mission3TutorialLook_Player_ShengChiehLee>().SynchronizeRotation(); }
    static void AimAtCard()
    {
        var direction = lesson.practiceCollider.bounds.center - manager.playerCamera.transform.position;
        var flat = direction; flat.y = 0; manager.playerBody.transform.rotation = Quaternion.LookRotation(flat);
        manager.playerBody.rotation = manager.playerBody.transform.rotation; manager.playerCamera.transform.rotation = Quaternion.LookRotation(direction);
        manager.playerCamera.GetComponent<mission3TutorialLook_Player_ShengChiehLee>().SynchronizeRotation(); Physics.SyncTransforms();
    }
    static void Log(string message, string stack, LogType type) { if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && stack.Contains("Assets/")) projectError = true; }
    static void Finish(string report)
    {
        File.WriteAllText(Output + "/validation.txt", report); SessionState.SetBool(Flag, false); Application.logMessageReceived -= Log;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard); if (mouse != null) InputSystem.RemoveDevice(mouse);
        EditorApplication.ExitPlaymode();
    }
    static void Capture(string name)
    {
        var camera = manager.playerCamera; var canvas = lesson != null ? lesson.GetComponentInChildren<Canvas>() : manager.canvasUI.GetComponent<Canvas>();
        var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float oldPlane = canvas.planeDistance;
        var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
        var data = camera.GetComponent<UniversalAdditionalCameraData>(); bool post = data != null && data.renderPostProcessing;
        var target = new RenderTexture(1600, 900, 24); target.Create(); Texture2D image = null;
        try
        {
            if (data != null) data.renderPostProcessing = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .5f; camera.targetTexture = target; Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target; image = new Texture2D(1600, 900, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply(); File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane; camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
            if (data != null) data.renderPostProcessing = post;
            if (image != null) UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
