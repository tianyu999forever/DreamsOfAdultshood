using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Kept outside the hotel scene so team scene edits do not overwrite the lesson.
[DefaultExecutionOrder(500)]
public class mission3Tutorial_Manager_ShengChiehLee : MonoBehaviour
{
    public enum Step { Welcome, Look, Walk, Sprint, Jump, Interact, Tools, Exit, Finished }
    public Transform spawn, walkTarget, sprintTarget, practiceCard, exitTarget, doorHinge;
    public Collider practiceCollider, doorCollider;
    public GameObject walkMarker, sprintMarker, practiceLabel, welcomePanel, toolsPanel, mapPanel, journalPanel, pausePanel;
    public TMP_Text stepLabel, titleLabel, instructionLabel, keyLabel, promptLabel, progressLabel, doorLabel;
    public Image progressFill;
    public RawImage mapImage;
    public CanvasGroup fade;
    public Button beginButton, resumeButton;
    public Step CurrentStep { get; private set; }
    public bool Initialized { get; private set; }
    public bool Paused { get; private set; }
    public bool MapPractised { get; private set; }
    public bool JournalPractised { get; private set; }
    public mission3Hotel_Manager_ShengChiehLee Manager { get; private set; }
    public static bool CompletedThisSession { get; private set; }
    public bool ControlsEnabled => lessonMove != null && lessonMove.enabled && lessonLook != null && lessonLook.enabled;

    const string ScenePath = "Assets/Scenes/Mission3.unity";
    const string PreviewPath = "Assets/Mission3_ShengChiehLee/Scenes/Mission3_Preview.unity";
    const string ResourcePath = "Mission3/TutorialRoom";
    Vector3 hotelPosition, cameraPosition, previousPosition;
    Quaternion hotelRotation, cameraRotation, previousView;
    bool hotelCanvasActive, guideEnabled, headBobEnabled, transitioning, mapOpen, journalOpen;
    mission3Watch_Player_ShengChiehLee[] watches;
    mission3TutorialLook_Player_ShengChiehLee lessonLook;
    mission3TutorialMove_Player_ShengChiehLee lessonMove;
    bool[] watchEnabled;
    float groundHeight, walkDistance, runTime, lookAngle, jumpPressedAt = -100;
    bool jumped;
    bool heldE, heldM, heldTab, heldEscape;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession()
    {
        CompletedThisSession = false;
        SceneManager.sceneLoaded -= SceneLoaded;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.path != ScenePath && scene.path != PreviewPath) return;
        var manager = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).Single();
        var originalMovement = manager.movement;
        if (!(originalMovement is mission3TutorialMove_Player_ShengChiehLee))
        {
            var movement = originalMovement.gameObject.AddComponent<mission3TutorialMove_Player_ShengChiehLee>();
            movement.walkSpeed = originalMovement.walkSpeed; movement.runSpeed = originalMovement.runSpeed; movement.jumpSpeed = originalMovement.jumpSpeed;
            movement.cameraTransform = originalMovement.cameraTransform; movement.footstepSource = originalMovement.footstepSource; movement.footstepClips = originalMovement.footstepClips;
            movement.walkStepInterval = originalMovement.walkStepInterval; movement.runStepInterval = originalMovement.runStepInterval; movement.walkVolume = originalMovement.walkVolume; movement.runVolume = originalMovement.runVolume;
            movement.enabled = originalMovement.enabled; originalMovement.enabled = false; manager.movement = movement;
        }
        if (scene.path != ScenePath || CompletedThisSession) return;
        var prefab = Resources.Load<GameObject>(ResourcePath);
        if (prefab == null) { Debug.LogError("Mission3 tutorial room is missing from Resources."); return; }
        var room = Instantiate(prefab); room.name = "Mission3_TutorialRoom";
        SceneManager.MoveGameObjectToScene(room, scene);
    }
    void Start()
    {
        Manager = gameObject.scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).Single();
        hotelPosition = Manager.playerBody.position; hotelRotation = Manager.playerBody.rotation;
        cameraPosition = Manager.playerCamera.transform.localPosition; cameraRotation = Manager.playerCamera.transform.localRotation;
        headBobEnabled = Manager.headBob != null && Manager.headBob.enabled;
        guideEnabled = Manager.guide != null && Manager.guide.enabled;
        hotelCanvasActive = Manager.canvasUI.gameObject.activeSelf;
        // Let the hotel's Start capture its own reception spawn before moving the player.
        Manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.None);
        if (Manager.headBob != null) headBobEnabled = Manager.headBob.enabled;
        Manager.enabled = false;
        if (Manager.guide != null) Manager.guide.enabled = false;
        Manager.canvasUI.gameObject.SetActive(false);
        lessonLook = Manager.playerCamera.gameObject.AddComponent<mission3TutorialLook_Player_ShengChiehLee>();
        lessonLook.enabled = false; lessonLook.player = Manager.playerBody.transform; lessonLook.sensitivity = Manager.mouseLook.sensitivity;
        lessonMove = (mission3TutorialMove_Player_ShengChiehLee)Manager.movement; lessonMove.enabled = false;
        watches = Manager.playerBody.GetComponentsInChildren<mission3Watch_Player_ShengChiehLee>(true);
        watchEnabled = watches.Select(x => x.enabled).ToArray();
        foreach (var watch in watches) watch.enabled = false;
        groundHeight = Physics.Raycast(hotelPosition, Vector3.down, out var floor, 20f, ~0, QueryTriggerInteraction.Ignore)
            ? floor.point.y : hotelPosition.y - Manager.playerBody.GetComponent<Collider>().bounds.extents.y;
        float leftEdge = hotelPosition.x;
        foreach (var collider in gameObject.scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Collider>(true)))
            if (collider.enabled && !collider.isTrigger && !collider.transform.IsChildOf(transform) && !collider.transform.IsChildOf(Manager.playerBody.transform))
                leftEdge = Mathf.Min(leftEdge, collider.bounds.min.x);
        transform.SetPositionAndRotation(new Vector3(leftEdge - 18f, groundHeight, hotelPosition.z), Quaternion.identity);
        spawn.localPosition = new Vector3(0, hotelPosition.y - groundHeight + .04f, -6.5f);
        PlacePlayer(spawn.position, transform.rotation, Quaternion.identity);
        mapImage.texture = Manager.floorMap;
        beginButton.onClick.AddListener(BeginTraining); resumeButton.onClick.AddListener(ResumeTraining);
        welcomePanel.SetActive(true); pausePanel.SetActive(false); toolsPanel.SetActive(false);
        walkMarker.SetActive(false); sprintMarker.SetActive(false); fade.alpha = 0;
        Initialized = true; SetStep(Step.Welcome); Freeze(true);
    }
    public void BeginTraining()
    {
        if (!Initialized || CurrentStep != Step.Welcome) return;
        welcomePanel.SetActive(false); SetStep(Step.Look); Freeze(false);
    }
    void Update()
    {
        if (!Initialized || transitioning || CurrentStep == Step.Finished || CurrentStep == Step.Welcome) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) { PauseTraining(); return; }
        bool ePressed = Pressed(ref heldE, keyboard.eKey.isPressed), mPressed = Pressed(ref heldM, keyboard.mKey.isPressed);
        bool tabPressed = Pressed(ref heldTab, keyboard.tabKey.isPressed), escapePressed = Pressed(ref heldEscape, keyboard.escapeKey.isPressed);
        if (Paused) return;
        if (mapOpen || journalOpen)
        {
            if (escapePressed || (mapOpen && mPressed) || (journalOpen && tabPressed)) CloseTool();
            return;
        }
        if (escapePressed) { PauseTraining(); return; }
        var position = Manager.playerBody.position;
        var view = Manager.playerCamera.transform.rotation;
        var local = transform.InverseTransformPoint(position);
        bool movingInput = keyboard.wKey.isPressed || keyboard.aKey.isPressed || keyboard.sKey.isPressed || keyboard.dKey.isPressed;
        var flatDelta = position - previousPosition; flatDelta.y = 0;
        switch (CurrentStep)
        {
            case Step.Look:
                lookAngle += Mathf.Min(Quaternion.Angle(previousView, view), 12f);
                if (lookAngle >= 40f) SetStep(Step.Walk);
                break;
            case Step.Walk:
                if (movingInput) walkDistance += Mathf.Min(flatDelta.magnitude, .5f);
                if (walkDistance >= 2f && FlatDistance(position, walkTarget.position) < .95f) SetStep(Step.Sprint);
                break;
            case Step.Sprint:
                var velocity = Manager.playerBody.linearVelocity; velocity.y = 0;
                if (movingInput && keyboard.leftShiftKey.isPressed && velocity.magnitude > Manager.movement.walkSpeed + .2f) runTime += Time.deltaTime;
                if (runTime >= .35f && FlatDistance(position, sprintTarget.position) < 1f) SetStep(Step.Jump);
                break;
            case Step.Jump:
                if (keyboard.spaceKey.isPressed) jumpPressedAt = Time.unscaledTime;
                if (Time.unscaledTime - jumpPressedAt < 1.1f && position.y > spawn.position.y + .16f && Manager.playerBody.linearVelocity.y > .7f) jumped = true;
                bool settled = Grounded() || (Mathf.Abs(Manager.playerBody.linearVelocity.y) < .15f && position.y < spawn.position.y + .8f);
                if (jumped && local.z > 4.3f && settled && Manager.playerBody.linearVelocity.y <= .25f) SetStep(Step.Interact);
                break;
            case Step.Interact:
                bool aimed = Physics.Raycast(Manager.playerCamera.transform.position, Manager.playerCamera.transform.forward, out var hit, 3f, ~0, QueryTriggerInteraction.Collide) && hit.collider == practiceCollider;
                Text(promptLabel, aimed ? "E   PICK UP TRAINING CARD" : "Aim at the gold card on the desk.");
                if (aimed && ePressed)
                { practiceCard.gameObject.SetActive(false); practiceLabel.SetActive(false); SetStep(Step.Tools); }
                break;
            case Step.Tools:
                if (mPressed) OpenTool(true);
                else if (tabPressed) OpenTool(false);
                break;
            case Step.Exit:
                doorHinge.localRotation = Quaternion.Slerp(doorHinge.localRotation, Quaternion.Euler(0, -105, 0), Time.deltaTime * 3f);
                if (FlatDistance(position, exitTarget.position) < 1.1f) TryEnterHotel();
                break;
        }
        previousPosition = Manager.playerBody.position; previousView = Manager.playerCamera.transform.rotation;
        if (local.y < -4f || Mathf.Abs(local.x) > 7f || local.z < -9.5f || local.z > 13f)
        { PlacePlayer(spawn.position, transform.rotation, Quaternion.identity); previousPosition = Manager.playerBody.position; }
    }
    bool Grounded()
    {
        var bounds = Manager.playerBody.GetComponent<Collider>().bounds;
        // A rounded capsule can settle slightly above the floor when pressed against a desk edge.
        return Physics.Raycast(bounds.center, Vector3.down, bounds.extents.y + .4f, ~0, QueryTriggerInteraction.Ignore);
    }
    void SetStep(Step step)
    {
        CurrentStep = step;
        walkMarker.SetActive(step == Step.Walk); sprintMarker.SetActive(step == Step.Sprint);
        Text(stepLabel, step == Step.Welcome ? "MANDATORY TUTORIAL" : step == Step.Exit ? "TRAINING COMPLETE" : "STEP " + (int)step + " / 6");
        Text(progressLabel, Mathf.Clamp((int)step - 1, 0, 6) + " / 6 COMPLETED");
        progressFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(((int)step - 1) / 6f), 1, 1);
        string title, instruction, keys;
        switch (step)
        {
            case Step.Look: title = "GET YOUR BEARINGS"; instruction = "Move the mouse to look around. Turn your view before moving on."; keys = "MOUSE   LOOK AROUND"; break;
            case Step.Walk: title = "FOLLOW THE LIGHT"; instruction = "Walk onto the cyan floor marker. The next instruction appears when you reach it."; keys = "W A S D   MOVE"; break;
            case Step.Sprint: title = "PICK UP THE PACE"; instruction = "Hold Left Shift while moving toward the next cyan marker."; keys = "LEFT SHIFT + W A S D   RUN"; break;
            case Step.Jump: title = "CLEAR THE LOW BARRIER"; instruction = "Move forward and jump over the low barrier to reach the other side."; keys = "SPACE   JUMP"; break;
            case Step.Interact: title = "AIM, THEN INTERACT"; instruction = "Look at the gold training card on the desk. Press E when the pickup prompt appears."; keys = "E   PICK UP / INTERACT"; break;
            case Step.Tools: title = "CHECK YOUR TOOLS"; instruction = "Open and close the floor map with M. Then open and close the guest journal with TAB."; keys = "M   FLOOR MAP     TAB   JOURNAL"; break;
            case Step.Exit: title = "READY FOR THE HOTEL"; instruction = "The exit is unlocked. Walk through the doorway to begin exploring the hotel."; keys = "W A S D   ENTER THE HOTEL"; doorCollider.enabled = false; Text(doorLabel, "READY\nENTER THE HOTEL"); break;
            default: title = "WELCOME TO FLOOR 03"; instruction = "Practise the controls in this safe room. Complete six short steps to unlock the hotel entrance."; keys = "NO TIMER  /  NO THREATS"; break;
        }
        Text(titleLabel, title); Text(instructionLabel, instruction); Text(keyLabel, keys);
        if (step != Step.Interact) Text(promptLabel, step == Step.Exit ? "The hotel starts beyond this door." : "ESC   PAUSE TRAINING");
        previousPosition = Manager != null ? Manager.playerBody.position : Vector3.zero;
        previousView = Manager != null ? Manager.playerCamera.transform.rotation : Quaternion.identity;
    }
    void OpenTool(bool map)
    {
        mapOpen = map; journalOpen = !map;
        toolsPanel.SetActive(true); mapPanel.SetActive(map); journalPanel.SetActive(!map); Freeze(true);
    }
    void CloseTool()
    {
        if (mapOpen) MapPractised = true;
        if (journalOpen) JournalPractised = true;
        mapOpen = journalOpen = false; toolsPanel.SetActive(false); Freeze(false);
        if (MapPractised && JournalPractised) SetStep(Step.Exit);
        else Text(instructionLabel, MapPractised ? "Map checked. Press TAB to open your guest journal, then close it with TAB." : "Journal checked. Press M to open your floor map, then close it with M.");
    }
    public void PauseTraining()
    {
        if (!Initialized || transitioning || CurrentStep == Step.Welcome || CurrentStep == Step.Finished || Paused) return;
        Paused = true; mapOpen = journalOpen = false; toolsPanel.SetActive(false); pausePanel.SetActive(true); Freeze(true);
    }
    public void ResumeTraining()
    {
        if (!Paused) return;
        Paused = false; pausePanel.SetActive(false); Freeze(false);
    }
    void Freeze(bool value)
    {
        if (Manager == null) return;
        Manager.movement.enabled = false; Manager.mouseLook.enabled = false; lessonLook.enabled = !value; lessonMove.enabled = !value;
        if (Manager.headBob != null) Manager.headBob.enabled = !value && headBobEnabled;
        if (!Manager.playerBody.isKinematic) Manager.playerBody.linearVelocity = Vector3.zero;
        Manager.playerBody.isKinematic = value;
        if (!value) lessonLook.SynchronizeRotation();
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = value;
    }
    void PlacePlayer(Vector3 position, Quaternion rotation, Quaternion cameraLocalRotation)
    {
        Manager.playerBody.transform.SetPositionAndRotation(position, rotation); Manager.playerBody.position = position; Manager.playerBody.rotation = rotation;
        Manager.playerCamera.transform.localPosition = cameraPosition; Manager.playerCamera.transform.localRotation = cameraLocalRotation;
        if (!Manager.playerBody.isKinematic) Manager.playerBody.linearVelocity = Vector3.zero;
        if (lessonLook != null) lessonLook.SynchronizeRotation(); Physics.SyncTransforms();
    }
    public bool TryEnterHotel()
    {
        if (CurrentStep != Step.Exit || transitioning || Paused || FlatDistance(Manager.playerBody.position, exitTarget.position) > 1.3f) return false;
        transitioning = true; StartCoroutine(EnterHotel()); return true;
    }
    IEnumerator EnterHotel()
    {
        Freeze(true);
        for (float time = 0; time < .3f; time += Time.unscaledDeltaTime) { fade.alpha = time / .3f; yield return null; }
        fade.alpha = 1;
        PlacePlayer(hotelPosition, hotelRotation, cameraRotation);
        Manager.canvasUI.gameObject.SetActive(hotelCanvasActive);
        if (Manager.guide != null) Manager.guide.enabled = guideEnabled;
        for (int i = 0; i < watches.Length; i++) if (watches[i] != null) watches[i].enabled = watchEnabled[i];
        Manager.enabled = true; Manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.None);
        lessonLook.enabled = false; Destroy(lessonLook);
        Manager.playerBody.isKinematic = false; Manager.playerBody.linearVelocity = Vector3.zero;
        Manager.movement.enabled = true; Manager.mouseLook.enabled = true;
        if (Manager.headBob != null) Manager.headBob.enabled = headBobEnabled;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        // Show the authored hotel story after training, before the shift timer starts.
        Manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Welcome);
        CompletedThisSession = true; CurrentStep = Step.Finished;
        Manager.ShowHint("Tutorial complete. Inspect the lobby map, read the guest notes, and find the lost belongings.");
        for (float time = .3f; time > 0; time -= Time.unscaledDeltaTime) { fade.alpha = time / .3f; yield return null; }
        Destroy(gameObject);
    }
    void OnApplicationFocus(bool focus) { if (!focus) PauseTraining(); }
    static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
    static void Text(TMP_Text label, string value) { if (label != null && label.text != value) label.text = value; }
    static bool Pressed(ref bool previous, bool current) { bool pressed = current && !previous; previous = current; return pressed; }
}
