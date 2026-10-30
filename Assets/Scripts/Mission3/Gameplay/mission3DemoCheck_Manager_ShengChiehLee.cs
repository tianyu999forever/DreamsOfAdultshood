using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Opt-in standalone regression run; normal launches start at the unmodified welcome screen.
public class mission3DemoCheck_Manager_ShengChiehLee : MonoBehaviour
{
    const string Out="C:/Users/j1a2m/Documents/Codex/2026-10-25/w/outputs";
    bool failed;
    IEnumerator Start()
    {
        if(!Environment.GetCommandLineArgs().Contains("-mission3-test"))yield break;
        Application.logMessageReceived+=OnLog;Application.runInBackground=true;
        yield return null;yield return null;
        var m=FindFirstObjectByType<mission3Hotel_Manager_ShengChiehLee>();
        Check(m!=null&&m.guide!=null,"Guide and manager loaded");
        if(failed){Finish();yield break;}
        var ui=m.canvasUI;var props=FindObjectsByType<mission3Interact_Prop_ShengChiehLee>(FindObjectsSortMode.None);
        Check(m.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.Welcome&&m.playerBody.isKinematic,"Welcome locks player");
        yield return Shot("demo-welcome");
        ui.Begin();Check(m.movement.enabled&&m.headBob.enabled,"Begin restores player controls");
        yield return new WaitForSeconds(1);
        Check(m.playerBody.position.y>.9f&&m.playerBody.position.y<1.8f,"Player remains on lobby floor");
        yield return Shot("demo-objective");
        m.Interact(props.First(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.Checkout));ui.Refresh();
        Check(m.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.Checkout&&!ui.reviewButton.interactable,"Empty bag can preview returns but cannot finalize");
        Check(ui.checkoutRequirement.text.Contains("3 more"),"Checkout explains missing items");
        m.CompleteCheckout();Check(!m.IsCompleted,"Empty checkout rejected");
        ui.Begin();ui.Map();Check(m.guide.MapViewed&&!m.movement.enabled,"Map advances guide and pauses movement");
        yield return Shot("demo-map");ui.Begin();
        Check(m.guide.ObjectiveText.Contains("MEET THE GUESTS"),"Guide moves to first note");
        m.Interact(props.First(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.GuestNote));ui.Begin();
        Check(m.guide.ObjectiveText.Contains("KEEP THE CLUES"),"Guide requests saved journal");ui.Journal();
        Check(m.guide.JournalViewed,"Journal action advances guide");ui.Begin();
        foreach(var p in props.Where(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.LostProperty)){m.Interact(p);m.Interact(p);}
        Check(m.CollectedCount==6,"Six unique pickups, no duplicate collection");ui.Map();yield return null;ui.Refresh();
        Check(ui.itemMarkers.All(x=>x.GetComponentInChildren<TMPro.TMP_Text>().text=="+"),"Collected map markers updated");ui.Begin();
        foreach(var p in props.Where(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.GuestNote))m.Interact(p);
        m.Interact(props.First(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.Checkout));ui.Refresh();
        Check(!ui.reviewButton.interactable&&ui.checkoutRequirement.text.Contains("6 remaining"),"Unassigned items explain disabled review");
        for(int i=0;i<6;i++)ui.ownerButtons[i*3+i/2].onClick.Invoke();ui.Refresh();
        Check(ui.reviewButton.interactable,"Owner buttons unlock review");yield return Shot("demo-checkout");
        ui.Review();Check(m.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.Confirm,"Confirmation page required");
        m.Interact(props.First(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.ServiceDoor));
        int events=0;m.onMissionCompleted.AddListener(_=>events++);ui.Finish();ui.Finish();
        Check(m.FinalScore==100&&events==1,"100-point scoring and one-shot completion");yield return Shot("demo-result");
        Finish();
    }
    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Out+"/mission3-demo/"+name+".png");yield return null;
    }
    void Check(bool ok,string message)
    {
        if(!ok){failed=true;Debug.LogError("DEMO_TEST_FAIL: "+message);}else Debug.Log("DEMO_TEST_OK: "+message);
    }
    void OnLog(string text,string trace,LogType type)
    {
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failed=true;
    }
    void Finish()
    {
        Application.logMessageReceived-=OnLog;
        File.WriteAllText(Out+"/mission3-demo/validation.txt",failed?"DEMO_PLAYTEST_FAIL: inspect test-player.log":"DEMO_PLAYTEST_PASS: standalone startup, physics, progressive tutorial, map, return preview, disabled-button explanations, six pickups, ownership assignment, confirmation and 100-point result verified.");
        Application.Quit(failed?1:0);
    }
}
