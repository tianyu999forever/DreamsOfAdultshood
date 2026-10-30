using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class mission3Hotel_Manager_ShengChiehLee : MonoBehaviour
{
    public Camera playerCamera;
    public Moving movement;
    public MouseLook mouseLook;
    public Rigidbody playerBody;
    public HeadBob headBob;
    public mission3Canvas_UI_ShengChiehLee canvasUI;
    public mission3Guide_UI_ShengChiehLee guide;
    public bool demoMode;
    public Texture2D floorMap;
    public GameObject serviceDoor;
    public Transform serviceDoorOpenPosition;
    [Tooltip("Exact destination scene name or Assets/... scene path. Add the destination to Build Settings before playing.")]
    public string nextSceneName = "Mission4";
    public Button nextLevelButton;
    public TMP_Text nextLevelStatus;
    public UnityEvent<int> onMissionCompleted = new UnityEvent<int>();
    public int FinalScore { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool ShortcutOpened { get; private set; }
    public bool[] collected = new bool[6];
    public bool[] notesRead = new bool[3];
    public int[] assignment = { -1, -1, -1, -1, -1, -1 };
    public enum Page { None, Welcome, Map, Journal, Note, Checkout, Confirm, Result, Pause }
    Page page = Page.Welcome;
    Page journalReturnPage=Page.None;
    mission3Interact_Prop_ShengChiehLee focus;
    string noteText = "", notice = "";
    float noticeUntil, elapsed;
    bool wasMoving, wasLooking, wasBobbing, frozen;
    int lookResumeFrames;
    Vector3 startPosition;
    Quaternion startRotation;
    mission3Rules_Manager_ShengChiehLee.Result result;
    GUIStyle title, body, small, button;
    int Count => collected.Count(x => x);
    public Page CurrentPage => page;
    public int CollectedCount => Count;
    public string FocusPrompt => focus == null ? "" : focus.Prompt;
    public string Notice => Time.unscaledTime < noticeUntil ? notice : "";
    public string CurrentNote => noteText;
    public float ElapsedSeconds => elapsed;
    public string ResultDescription => "CHECKED OUT  /  " + FinalScore + " / 100\n\nCorrect returns: " + result.correct + "/6  (+" + result.itemPoints + ")\nComplete guest pairs: " + result.sets + "/3  (+" + result.setPoints + ")\nExploration bonus: +" + result.explorationPoints + "\n\n" + string.Join("\n", Enumerable.Range(0,6).Select(i => mission3Rules_Manager_ShengChiehLee.Items[i] + "  >  " + (301+mission3Rules_Manager_ShengChiehLee.Owner(i)) + (!collected[i] ? " / NOT FOUND" : assignment[i] == mission3Rules_Manager_ShengChiehLee.Owner(i) ? " / RETURNED" : " / WRONG OWNER")));

    void Start()
    {
        startPosition = playerBody.transform.position; startRotation = playerBody.transform.rotation;
        SetPage(Page.Welcome);
        RefreshNextLevelStatus();
    }
    public void SetPage(Page value)
    {
        page = value; bool stop = value != Page.None;
        if (stop && !frozen) { wasMoving = movement.enabled; wasLooking = mouseLook.enabled || (lookResumeFrames>0&&wasLooking); lookResumeFrames=0; wasBobbing = headBob != null && headBob.enabled; movement.enabled = false; mouseLook.enabled = false; if(headBob != null) headBob.enabled = false; playerBody.linearVelocity = Vector3.zero; playerBody.isKinematic = true; frozen = true; }
        else if (!stop && frozen) { playerBody.isKinematic = false; movement.enabled = wasMoving; mouseLook.enabled = false; lookResumeFrames=3; if(headBob != null) headBob.enabled = wasBobbing; frozen = false; }
        Cursor.lockState = stop ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = stop;
        focus = null;
        if(guide!=null)guide.PageOpened(value);
        if(canvasUI!=null)canvasUI.Refresh();
    }
    void LateUpdate() { if (page != Page.None) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } else if(lookResumeFrames>0&&--lookResumeFrames==0)mouseLook.enabled=wasLooking; }
    void Update()
    {
        var k = Keyboard.current; if (k == null) return;
        if (page == Page.Map && k.mKey.wasPressedThisFrame) { SetPage(Page.None); return; }
        if(k.tabKey.wasPressedThisFrame&&(page==Page.None||page==Page.Checkout||page==Page.Journal)) { if(page==Page.Journal)BackToHotel();else OpenJournal();return; }
        if (k.escapeKey.wasPressedThisFrame) { SetPage(page == Page.None ? Page.Pause : Page.None); return; }
        if (page != Page.None) return;
        elapsed += Time.deltaTime;
        if (k.mKey.wasPressedThisFrame) { SetPage(Page.Map); return; }
        focus = null;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, 3.4f, ~0, QueryTriggerInteraction.Collide))
            focus = hit.collider.GetComponentInParent<mission3Interact_Prop_ShengChiehLee>();
        if (focus != null && (k.eKey.wasPressedThisFrame ||
            (focus.kind == mission3Interact_Prop_ShengChiehLee.Kind.NextLevel && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))) Interact(focus);
        if (playerBody.position.y < -8) { playerBody.position = startPosition; playerBody.rotation = startRotation; playerBody.linearVelocity = Vector3.zero; Notify("Returned to reception."); }
    }
    public void Interact(mission3Interact_Prop_ShengChiehLee prop)
    {
        if (prop == null) return;
        if (prop.kind == mission3Interact_Prop_ShengChiehLee.Kind.NextLevel)
        {
            if (nextLevelButton != null) nextLevelButton.onClick.Invoke();
            else LoadNextLevel();
            return;
        }
        if (IsCompleted && prop.kind != mission3Interact_Prop_ShengChiehLee.Kind.LiftDoors) return;
        switch (prop.kind)
        {
            case mission3Interact_Prop_ShengChiehLee.Kind.LostProperty:
                if (prop.index < 0 || prop.index >= 6 || collected[prop.index]) return;
                collected[prop.index] = true; prop.gameObject.SetActive(false); Notify("Collected: " + mission3Rules_Manager_ShengChiehLee.Items[prop.index]); if(guide!=null)guide.ItemCollected(); break;
            case mission3Interact_Prop_ShengChiehLee.Kind.GuestNote:
                notesRead[prop.index] = true; noteText = mission3Rules_Manager_ShengChiehLee.Notes[prop.index]; SetPage(Page.Note); break;
            case mission3Interact_Prop_ShengChiehLee.Kind.FloorMap: SetPage(Page.Map); break;
            case mission3Interact_Prop_ShengChiehLee.Kind.ServiceDoor:
                if (ShortcutOpened) return; ShortcutOpened = true;
                if (serviceDoor != null) serviceDoor.SetActive(false);
                prop.gameObject.SetActive(false); Notify("Service passage opened. Exploration bonus: +10."); break;
            case mission3Interact_Prop_ShengChiehLee.Kind.LiftDoors:
                var lift = prop.GetComponentInParent<mission3Lift_Prop_ShengChiehLee>();
                if (lift != null) { lift.Open(); Notify("Lift doors opening. Use the checkout panel inside to return belongings."); }
                break;
            case mission3Interact_Prop_ShengChiehLee.Kind.Checkout:
                SetPage(Page.Checkout); break;
        }
    }
    void Notify(string text) { notice = text; noticeUntil = Time.unscaledTime + 4; }
    public void OpenJournal(){journalReturnPage=page==Page.Checkout?Page.Checkout:Page.None;SetPage(Page.Journal);}
    public void BackToHotel(){SetPage(page==Page.Journal?journalReturnPage:Page.None);}
    public void ShowHint(string text) { notice=text; noticeUntil=Time.unscaledTime+8; }
    public string CheckoutRequirement => Count<3 ? "Find "+(3-Count)+" more belongings to unlock checkout. You can preview the owner choices now." :
        Enumerable.Range(0,6).Any(i=>collected[i]&&assignment[i]<0) ? "Choose an owner for "+Enumerable.Range(0,6).Count(i=>collected[i]&&assignment[i]<0)+" remaining belongings. TAB: guest journal." : "All collected belongings have an owner. Review your choices before finalizing.";
    public void QuitDemo()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
    public void CompleteCheckout()
    {
        if (IsCompleted || Count < 3) return;
        for (int i = 0; i < 6; i++) if (collected[i] && assignment[i] < 0) { Notify("Assign every collected item before checkout."); return; }
        result = mission3Rules_Manager_ShengChiehLee.Settle(collected, assignment, ShortcutOpened);
        FinalScore = result.score; IsCompleted = true; SetPage(Page.Result);
        RefreshNextLevelStatus();
        onMissionCompleted.Invoke(FinalScore);
    }
    public void RefreshNextLevelStatus()
    {
        if (nextLevelStatus == null) return;
        nextLevelStatus.text = !IsCompleted ? "RETURN BELONGINGS FIRST" : demoMode ? "DEMO COMPLETE / E OR CLICK TO EXIT" :
            (Application.CanStreamedLevelBeLoaded(nextSceneName) ? "READY  /  E OR CLICK TO CONTINUE" : "DESTINATION NOT ADDED YET");
    }
    public void LoadNextLevel()
    {
        if (!IsCompleted) { Notify("Complete checkout before travelling to the next level."); return; }
        if(demoMode){QuitDemo();return;}
        string destination = (nextSceneName ?? "").Trim();
        if (string.IsNullOrEmpty(destination) || !Application.CanStreamedLevelBeLoaded(destination))
        {
            Notify("Next level is not available yet: " + (string.IsNullOrEmpty(destination) ? "no destination configured" : destination) + ".");
            return;
        }
        if (nextLevelButton != null) nextLevelButton.interactable = false;
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        SceneManager.LoadScene(destination);
    }
    void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    void InitStyles()
    {
        if (title != null) return;
        title = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, wordWrap = true };
        body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
        small = new GUIStyle(body) { fontSize = 14 };
        button = new GUIStyle(GUI.skin.button) { fontSize = 16, fixedHeight = 40, wordWrap = true };
    }
    void OnGUI()
    {
        if (canvasUI != null) return;
        InitStyles(); float scale = Mathf.Min(Screen.width/1200f, Screen.height/800f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
        float w = Screen.width/scale, h = Screen.height/scale;
        GUI.color = new Color(1,.94f,.79f);
        GUI.Label(new Rect(25,20,600,38), "MISSION 3  /  THE LAST CHECKOUT ", title);
        GUI.Label(new Rect(25,58,650,35), "LOST & FOUND   |   " + Count + "/6 belongings   |   " + notesRead.Count(x=>x) + "/3 guest notes", small);
        if (page == Page.None)
        {
            GUI.Label(new Rect(w/2-5,h/2-12,24,24), "+", body);
            if (focus != null) GUI.Label(new Rect(w/2-230,h/2+35,460,40), focus.Prompt, body);
            GUI.Label(new Rect(25,h-42,w-50,35), "WASD Move   SHIFT Run   E Interact   TAB Journal   M Floor map   ESC Pause", small);
        }
        if (Time.unscaledTime < noticeUntil) GUI.Label(new Rect(25,102,w-50,44), notice, body);
        if (page == Page.None) return;
        GUI.color = new Color(.1f,.07f,.06f,.97f); GUI.DrawTexture(new Rect(0,150,w,h-150), Texture2D.whiteTexture); GUI.color = new Color(1,.94f,.79f);
        GUILayout.BeginArea(new Rect(90,170,w-180,h-190));
        switch (page)
        {
            case Page.Welcome:
                GUILayout.Label("Some guests left more than their luggage.", title); GUILayout.Space(18);
                GUILayout.Label("Explore the hotel. Read the three guest notes, collect lost belongings, then return to the reception checkout beside the lift. Assign each belonging to its owner and settle your score.", body);
                GUILayout.Space(18); GUILayout.Label("Correct return: 10 points each (60 max)\nComplete guest pair: 10 bonus points (30 max)\nOpen the service shortcut: 10 points\nTotal: 100. You may leave with 3 or more items. No countdown.", body);
                GUILayout.Space(18); GUILayout.Label("The entrance wall carries the real floor plan. M opens a copy; TAB keeps the notes you have read. Items found in a room do not necessarily belong to that guest.", body);
                if (GUILayout.Button("Begin the night shift",button)) SetPage(Page.None); break;
            case Page.Map:
                GUILayout.Label("HOTEL 24 / FLOOR PLAN",title);
                Rect r = GUILayoutUtility.GetRect(480,370); GUI.DrawTexture(r,floorMap,ScaleMode.ScaleToFit);
                GUILayout.Label("Gold: walkable corridors / rooms   Red: entrance map   Blue: checkout + lift\n301 west wing / Traveller   302 north wing / Musician   303 east wing / Botanist\nGreen: service shortcut. North is at the top. TAB opens your guest journal.",small);
                Back(); break;
            case Page.Note: GUILayout.Label(noteText,title); GUILayout.Space(24); GUILayout.Label("This note is now saved in your journal (TAB).",body); Back(); break;
            case Page.Journal:
                GUILayout.Label("GUEST JOURNAL",title);
                for(int g=0;g<3;g++) { GUILayout.Space(12); GUILayout.Label(notesRead[g] ? mission3Rules_Manager_ShengChiehLee.Notes[g] : mission3Rules_Manager_ShengChiehLee.Guests[g]+" / Note not found yet", body); }
                GUILayout.Space(15); GUILayout.Label("In your bag: " + string.Join(", ",Enumerable.Range(0,6).Where(i=>collected[i]).Select(i=>mission3Rules_Manager_ShengChiehLee.Items[i])), small); Back(); break;
            case Page.Checkout:
                GUILayout.Label("CHECKOUT / RETURN LOST PROPERTY",title);
                GUILayout.Label("Select the guest for each collected belonging. Assignments can be changed until you finalize.",small);
                for(int i=0;i<6;i++) if(collected[i]) { GUILayout.BeginHorizontal(); GUILayout.Label(mission3Rules_Manager_ShengChiehLee.Items[i],body,GUILayout.Width(240));
                    for(int g=0;g<3;g++) { GUI.backgroundColor = assignment[i]==g ? new Color(.45f,.8f,.5f) : Color.white; if(GUILayout.Button((301+g).ToString(),button,GUILayout.Width(95))) assignment[i]=g; } GUI.backgroundColor=Color.white; GUILayout.EndHorizontal(); }
                GUILayout.Label("301 Traveller / 302 Musician / 303 Botanist",small);
                if(GUILayout.Button("Review final checkout",button)) { if(Enumerable.Range(0,6).Any(i=>collected[i]&&assignment[i]<0)) Notify("Choose a guest for each collected item."); else SetPage(Page.Confirm); }
                if(GUILayout.Button("Return to exploration / read journal",button)) SetPage(Page.None); break;
            case Page.Confirm:
                GUILayout.Label("Ready to leave the hotel?",title);
                GUILayout.Label("You collected "+Count+" of 6 belongings. Finalizing ends this run and locks the score. Any missing or incorrectly assigned belongings earn no return points.",body);
                if(GUILayout.Button("Finalize checkout and reveal score",button)) CompleteCheckout();
                if(GUILayout.Button("Keep editing assignments",button)) SetPage(Page.Checkout); break;
            case Page.Result:
                GUILayout.Label("CHECKED OUT   /   "+FinalScore+" / 100",title);
                GUILayout.Label("Correct returns: "+result.correct+" / 6  = "+result.itemPoints+"\nComplete guest pairs: "+result.sets+" / 3  = "+result.setPoints+"\nService shortcut: "+result.explorationPoints,body);
                for(int i=0;i<6;i++) GUILayout.Label(mission3Rules_Manager_ShengChiehLee.Items[i]+" -> "+(301+mission3Rules_Manager_ShengChiehLee.Owner(i))+(collected[i]?(assignment[i]==mission3Rules_Manager_ShengChiehLee.Owner(i)?"  / RETURNED":"  / WRONG OWNER"):"  / NOT FOUND"),small);
                if(GUILayout.Button("Explore again",button)) Restart();
                if(GUILayout.Button("Return to lift / NEXT LEVEL",button)) SetPage(Page.None);
                break;
            case Page.Pause:
                GUILayout.Label("PAUSED",title); if(GUILayout.Button("Continue",button)) SetPage(Page.None); if(GUILayout.Button("Floor map",button)) SetPage(Page.Map); break;
        }
        GUILayout.EndArea();
    }
    public void Restart()
    {
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(gameObject.scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(gameObject.scene.name);
#endif
    }
    void Back() { GUILayout.Space(15); if(GUILayout.Button("Back to hotel",button)) SetPage(Page.None); }
}
