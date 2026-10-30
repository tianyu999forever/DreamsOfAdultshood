using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum Mission6_Phase_Jizhijin { Planning, Exploring, Transition, Finished, Failed }
[DefaultExecutionOrder(200)]
public class Mission6_Campaign_Jizhijin : MonoBehaviour
{
    public Mission6_Grid_Level_Jizhijin[] levels;
    public Moving moving; public MouseLook mouseLook; public HeadBob headBob; public PocketWatch watch;
    public Mission6_Letters_GameManager_Jizhijin inventory;
    public Mission6_Photo_Player_Jizhijin photo;
    public Mission6_Spelling_ExitPanel_Jizhijin[] puzzles;
    public GameObject bagPanel, photoHint, deathPanel, finishPanel;
    public TMP_Text objective, timer, stageResult, deathReason, finishText;
    public Transform firstGate;
    public Collider gateCollider;
    public NavMeshData navigation;
    public Mission6_Enemy_Jizhijin[] enemies;
    public string nextSceneName;
    public Mission6_Phase_Jizhijin Phase { get; private set; }
    public Mission6_Grid_Level_Jizhijin CurrentLevel { get; private set; }
    public bool PuzzleOpen { get; private set; }
    public float Elapsed { get; private set; }
    public float PlanLeft { get; private set; }
    public bool GateOpen { get; private set; }
    public Mission6_Score_Jizhijin FirstScore { get; private set; }
    public Mission6_Score_Jizhijin SecondScore { get; private set; }
    public bool HazardsActive => CurrentLevel == levels[1] && Phase == Mission6_Phase_Jizhijin.Exploring && !PuzzleOpen && Time.timeScale > 0;
    NavMeshDataInstance navigationInstance;
    Vector3 gateClosed;
    bool watchVisible;
    static int restartAt;
    static Mission6_Score_Jizhijin checkpointScore;
    void Awake() { if (navigation != null) navigationInstance = NavMesh.AddNavMeshData(navigation); }
    void Start()
    {
        Time.timeScale = 1; RenderSettings.fog = false;
        watch.enabled = false; gateClosed = firstGate.localPosition;
        int initial = restartAt; restartAt = 0;
        if (initial == 2) { FirstScore = checkpointScore; GateOpen = true; gateCollider.enabled = false; firstGate.localPosition = gateClosed + Vector3.up * 4.5f; }
        StartLevel(initial == 2 ? 2 : 1, true);
    }
    public void StartLevel(int number, bool reposition)
    {
        CurrentLevel = levels[number - 1]; Phase = Mission6_Phase_Jizhijin.Planning;
        Elapsed = 0; PlanLeft = 30; PuzzleOpen = false;
        foreach (var p in puzzles) p.gameObject.SetActive(false);
        foreach (var l in levels) { l.planPanel.SetActive(l == CurrentLevel); l.photoPanel.SetActive(false); }
        inventory.Clear(); photo.SetLevel(CurrentLevel);
        bagPanel.SetActive(false); photoHint.SetActive(false); deathPanel.SetActive(false); finishPanel.SetActive(false);
        stageResult.text = "";
        SetPlayerControl(true);
        if (reposition)
        {
            var rb = moving.GetComponent<Rigidbody>(); rb.position = CurrentLevel.Data.Center(1, 1, 1.4f); rb.rotation = Quaternion.identity; rb.linearVelocity = Vector3.zero;
            Camera.main.transform.localRotation = Quaternion.Euler(-8, 0, 0); Physics.SyncTransforms();
        }
        watchVisible = false; if (watch.watchVisuals != null) watch.watchVisuals.SetActive(false);
        RefreshHud();
    }
    public void EnterSecondLevel()
    {
        if (!GateOpen || CurrentLevel != levels[0] || Phase != Mission6_Phase_Jizhijin.Transition) return;
        checkpointScore = FirstScore; StartLevel(2, false);
    }
    public void BeginExploration()
    {
        if (Phase != Mission6_Phase_Jizhijin.Planning) return;
        Phase = Mission6_Phase_Jizhijin.Exploring; CurrentLevel.planPanel.SetActive(false);
        bagPanel.SetActive(true); photoHint.SetActive(true); RefreshHud();
    }
    void Update()
    {
        if (watch != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) watchVisible = !watchVisible;
        if (watch != null)
        {
            bool visible = watchVisible && Phase != Mission6_Phase_Jizhijin.Failed && Phase != Mission6_Phase_Jizhijin.Finished;
            if (watch.watchVisuals != null) watch.watchVisuals.SetActive(visible);
            watch.transform.localPosition = visible ? watch.shownPosition : watch.hiddenPosition;
            watch.transform.localRotation = Quaternion.Euler(visible ? watch.shownRotation : watch.hiddenRotation);
        }
        if (GateOpen) firstGate.localPosition = Vector3.MoveTowards(firstGate.localPosition, gateClosed + Vector3.up * 4.5f, Time.unscaledDeltaTime * 4);
        if (Phase == Mission6_Phase_Jizhijin.Planning)
        {
            PlanLeft = Mathf.Max(0, PlanLeft - Time.deltaTime);
            if (PlanLeft <= 0) Fail("PLAN time expired. Leave the starting room before 30 seconds.");
        }
        // Answer selection counts toward the clear time, even while hazards are paused.
        if (Phase == Mission6_Phase_Jizhijin.Exploring)
        {
            Elapsed += Time.unscaledDeltaTime;
            if (Elapsed >= CurrentLevel.Data.timeLimit) Fail("Time expired. Try a shorter route and complete enough words.");
        }
        RefreshHud();
    }
    void RefreshHud()
    {
        if (CurrentLevel == null) return;
        var d = CurrentLevel.Data;
        timer.text = "LEVEL " + d.number + "   " + Mathf.Max(0, d.timeLimit - Elapsed).ToString("0") + "s";
        foreach (var p in puzzles) if (p.gameObject.activeSelf && p.timeText != null) p.timeText.text = "TIME LEFT " + Mathf.Max(0, d.timeLimit - Elapsed).ToString("0") + "s   |   ELAPSED " + Elapsed.ToString("0.0") + "s   |   SCROLL LETTER LIST FOR MORE TILES";
        if (watch.timerText != null) watch.timerText.text = Mathf.FloorToInt(Mathf.Max(0, d.timeLimit - Elapsed) / 60).ToString("00") + ":" + Mathf.CeilToInt(Mathf.Max(0, d.timeLimit - Elapsed) % 60).ToString("00");
        objective.text = Phase == Mission6_Phase_Jizhijin.Planning ? "LEVEL " + d.number + "   PLAN " + Mathf.CeilToInt(PlanLeft) + "s   /   MOVE AND LEAVE EARLY" :
            Phase == Mission6_Phase_Jizhijin.Transition ? "GATE OPEN   /   WALK THROUGH TO LEVEL 2" :
            "LEVEL " + d.number + "   COMPLETE AT LEAST " + d.minimumWords + " / " + d.words.Length + " WORDS" + (d.number == 2 ? "   /   AVOID THE SENTINELS" : "");
    }
    public void OpenPuzzle(Mission6_Grid_Level_Jizhijin level)
    {
        if (CurrentLevel != level || Phase != Mission6_Phase_Jizhijin.Exploring || PuzzleOpen) return;
        PuzzleOpen = true; photo.Close(); bagPanel.SetActive(false); photoHint.SetActive(false);
        puzzles[level.stageNumber - 1].gameObject.SetActive(true); SetPlayerControl(false);
        Time.timeScale = 0;
    }
    public void ClosePuzzle()
    {
        if (Phase != Mission6_Phase_Jizhijin.Exploring) return;
        foreach (var p in puzzles) p.gameObject.SetActive(false);
        PuzzleOpen = false; Time.timeScale = 1; SetPlayerControl(true); bagPanel.SetActive(true); photoHint.SetActive(true);
    }
    public bool CompleteCurrentLevel()
    {
        if (Phase != Mission6_Phase_Jizhijin.Exploring || !PuzzleOpen) return false;
        var d = CurrentLevel.Data; var p = puzzles[d.number - 1]; p.CheckAnswer();
        var score = Mission6_Score_Jizhijin.Calculate(d.number, p.CorrectWords, d.words.Length, d.minimumWords, Elapsed, d.timeLimit);
        if (!score.passed) { p.resultText.text = p.CorrectWords + " / " + d.words.Length + " correct. Need at least " + d.minimumWords + " to open the gate."; return false; }
        p.gameObject.SetActive(false); PuzzleOpen = false; photo.Close(); Time.timeScale = 1;
        if (d.number == 1)
        {
            FirstScore = score; checkpointScore = score; GateOpen = true; gateCollider.enabled = false;
            Phase = Mission6_Phase_Jizhijin.Transition; stageResult.text = score.Summary;
            bagPanel.SetActive(false); photoHint.SetActive(false); SetPlayerControl(true);
        }
        else
        {
            SecondScore = score; Phase = Mission6_Phase_Jizhijin.Finished;
            SetPlayerControl(false); finishPanel.SetActive(true);
            float average = (FirstScore.points + SecondScore.points) / 2;
            string rank = average >= 90 ? "S" : average >= 80 ? "A" : average >= 70 ? "B" : "C";
            finishText.text = "MISSION COMPLETE   " + rank + "\n\n" + FirstScore.Summary + "\n\n" + SecondScore.Summary + "\n\nTOTAL  " + (FirstScore.seconds + SecondScore.seconds).ToString("0.0") + "s   |   " + average.ToString("0.0") + "/100";
            Time.timeScale = 0;
        }
        return true;
    }
    public void Fail(string reason)
    {
        if (Phase == Mission6_Phase_Jizhijin.Failed || Phase == Mission6_Phase_Jizhijin.Finished) return;
        Phase = Mission6_Phase_Jizhijin.Failed; PuzzleOpen = false; photo.Close();
        foreach (var p in puzzles) p.gameObject.SetActive(false);
        deathPanel.SetActive(true); deathReason.text = reason; SetPlayerControl(false); Time.timeScale = 0;
    }
    public void Replay()
    {
        restartAt = CurrentLevel != null && CurrentLevel.stageNumber == 2 && Phase != Mission6_Phase_Jizhijin.Finished ? 2 : 1;
        checkpointScore = FirstScore; Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }
    public void NextMission()
    {
        int index = SceneManager.GetActiveScene().buildIndex + 1;
        string path = string.IsNullOrWhiteSpace(nextSceneName) ? (index < SceneManager.sceneCountInBuildSettings ? SceneUtility.GetScenePathByBuildIndex(index) : "") : nextSceneName.Trim();
        if (string.IsNullOrEmpty(path) || !Application.CanStreamedLevelBeLoaded(path)) return;
        restartAt = 0; Time.timeScale = 1; SceneManager.LoadScene(path);
    }
    public void SetPlayerControl(bool enabled)
    {
        moving.enabled = mouseLook.enabled = headBob.enabled = enabled;
        if (!enabled) moving.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !enabled;
    }
    void OnDestroy() { if (navigationInstance.valid) navigationInstance.Remove(); }
}
