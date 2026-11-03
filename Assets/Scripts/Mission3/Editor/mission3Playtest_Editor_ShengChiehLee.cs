using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[InitializeOnLoad]
public static class mission3Playtest_Editor_ShengChiehLee
{
    const string Flag="Mission3.PresentationTest";
    const string Output="C:/Users/j1a2m/Documents/Codex/2026-10-25/w/outputs/mission3-preview";
    static int frame,stage,controlsResumeFrame;
    static double started;
    static bool hadError;
    static mission3Hotel_Manager_ShengChiehLee manager;
    static mission3Canvas_UI_ShengChiehLee ui;
    static mission3Interact_Prop_ShengChiehLee[] props;
    static mission3Playtest_Editor_ShengChiehLee()
    {
        if(SessionState.GetBool(Flag,false)) Subscribe();
    }
    static void Subscribe()
    {
        started=EditorApplication.timeSinceStartup;frame=stage=0;hadError=false;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
    }
    public static void Begin(bool smoke)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play mode before running the presentation check.");
        mission3Presentation_Editor_ShengChiehLee.Validate();
        Directory.CreateDirectory(Output);SessionState.SetBool(Flag,true);Subscribe();EditorApplication.EnterPlaymode();
    }
    static void Log(string message,string stack,LogType type)
    {
        if(stack.Contains("UnityEditor.Search.SearchDatabase")&&!stack.Contains("Assets/"))return;
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)hadError=true;
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup-started>150)throw new Exception("Presentation play test timed out.");
            if(!EditorApplication.isPlaying)return;
            if(stage==8)
            {
                if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/Mission1.unity")return;
                Stop("PLAYMODE_PASS\nLocation flow: only the Lost & Found desk accepts returns; desk confirmation locks assignments without scoring; lift settlement rejects unreturned items and outside passengers; score/result and actual next-level travel work only inside the lift; one notice is installed. Results Canvas: perfect score 100 (60/30/10), partial score 30 (20/10/0), correct/wrong/missing item rows, frozen score, missing-destination guards and replay reset verified. The actual NEXT LEVEL button loaded the saved Mission1 destination without a runtime destination override.\nGuided tutorial, player physics, spatial clearance, map state, six pickups, journal, confirmation, hidden door and one-shot scoring also verified.\nFourteen preview images captured. Play mode stopped; a fresh guided demo opens.");return;
            }
            if(stage==7)
            {
                var fresh=UnityEngine.Object.FindFirstObjectByType<mission3Hotel_Manager_ShengChiehLee>();
                if(fresh==null||fresh==manager||fresh.CurrentPage!=mission3Hotel_Manager_ShengChiehLee.Page.Welcome)return;
                if(++frame<50)return;frame=0;
                manager=fresh;ui=manager.canvasUI;
                Require(!manager.IsCompleted&&manager.CollectedCount==0&&manager.FinalScore==0,"Replay did not reset the mission.");
                props=UnityEngine.Object.FindObjectsByType<mission3Interact_Prop_ShengChiehLee>(FindObjectsSortMode.None);
                foreach(var item in props.Where(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.LostProperty&&x.index<3))manager.Interact(item);
                AtDesk();manager.Interact(manager.returnDesk);
                manager.assignment[0]=manager.assignment[1]=0;manager.assignment[2]=2;ui.Refresh();ui.Review();ui.Finish();
                Require(manager.ReturnsSubmitted&&!manager.IsCompleted&&manager.FinalScore==0,"Desk return settled the partial score early.");
                InsideLift();manager.UseLiftTerminal();ui.Refresh();
                var results=ui.resultsCanvas;
                Require(manager.FinalScore==30&&results.totalScore.text=="30"&&results.itemPoints.text=="+20"&&results.pairPoints.text=="+10"&&results.explorationPoints.text=="+0","Partial settlement breakdown failed.");
                Require(results.itemStates[2].text=="WRONG OWNER  /  +0"&&results.itemStates[3].text=="NOT FOUND  /  +0","Partial settlement rows failed.");
                Shot("12-partial-result");Require(manager.nextSceneName=="Assets/Scenes/Mission1.unity","Saved next-level destination is not Mission1.");manager.RefreshNextLevelStatus();
                Require(results.continueButton.interactable,"Available destination did not enable NEXT LEVEL.");
                stage=8;results.continueButton.onClick.Invoke();return;
            }
            if(++frame<50)return;frame=0;
            if(stage==0)
            {
                Application.runInBackground=true;Time.timeScale=1;
                manager=UnityEngine.Object.FindFirstObjectByType<mission3Hotel_Manager_ShengChiehLee>();ui=manager.canvasUI;
                props=UnityEngine.Object.FindObjectsByType<mission3Interact_Prop_ShengChiehLee>(FindObjectsSortMode.None);
                Require(manager.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.Welcome&&manager.playerBody.isKinematic,"Welcome modal failed to freeze the player.");
                mission3Space_Editor_ShengChiehLee.Validate(manager.gameObject.scene);
                mission3CheckoutFlow_Editor_ShengChiehLee.Validate(manager.gameObject.scene);
                Require(props.Count(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.Checkout)==1,"Lift still accepts item returns.");
                Shot("01-welcome");ui.Begin();controlsResumeFrame=Time.frameCount+6;stage++;return;
            }
            if(stage==1)
            {
                if(Time.frameCount<controlsResumeFrame)return;
                Require(manager.movement.enabled&&manager.mouseLook.enabled&&manager.headBob.enabled,"Player controls were not restored: move="+manager.movement.enabled+", look="+manager.mouseLook.enabled+", bob="+manager.headBob.enabled);
                Require(manager.playerBody.position.y>.9f&&manager.playerBody.position.y<1.8f,"Player collider or reception floor failed.");
                Shot("02-exploration");ui.Map();ui.Refresh();stage++;return;
            }
            if(stage==2)
            {
                // Editor update callbacks can outpace player LateUpdate while the Game view is unfocused.
                ui.Refresh();
                Require(manager.guide!=null&&manager.guide.MapViewed,"Map did not advance the guided tutorial.");
                Require(ui.mapRect.Find("Reception marker")!=null&&ui.mapRect.Find("Lift marker")!=null&&ui.mapRect.Find("Lounge marker")!=null,"Public map landmarks are missing.");
                Require(ui.pages[2].activeInHierarchy&&!manager.movement.enabled&&!manager.headBob.enabled,"Map modal movement lock failed: page="+manager.CurrentPage+", panel="+ui.pages[2].activeInHierarchy+", move="+manager.movement.enabled+", bob="+manager.headBob.enabled);
                Require(ui.itemMarkers.All(x=>x.rectTransform.anchorMin.x>0&&x.rectTransform.anchorMin.x<1&&x.rectTransform.anchorMin.y>0&&x.rectTransform.anchorMin.y<1),"Map marker fell outside the floor plan.");
                Shot("03-floor-plan");
                manager.Interact(props.First(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.GuestNote));stage++;return;
            }
            if(stage==3)
            {
                Require(manager.notesRead.Count(x=>x)==1,"Guest note was not retained.");Shot("04-guest-note");
                Require(manager.guide.ObjectiveText.Contains("KEEP THE CLUES"),"Tutorial did not ask for the journal.");
                ui.Journal();Require(manager.guide.JournalViewed,"Journal did not advance the tutorial.");ui.Begin();
                AtDesk();manager.Interact(manager.returnDesk);Require(!manager.IsCompleted,"Empty checkout was allowed.");
                Require(manager.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.Checkout&&!ui.reviewButton.interactable&&ui.checkoutRequirement.text.Contains("3 more"),"Empty-bag preview or its explanation failed.");
                manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.None);InsideLift();manager.UseLiftTerminal();
                Require(!manager.IsCompleted&&!manager.ReturnsSubmitted&&manager.CurrentPage!=mission3Hotel_Manager_ShengChiehLee.Page.Result,"Lift settled before desk returns.");
                var fake=new GameObject("Rejected remote return test").AddComponent<mission3Interact_Prop_ShengChiehLee>();fake.kind=mission3Interact_Prop_ShengChiehLee.Kind.Checkout;
                manager.Interact(fake);Require(manager.CurrentPage!=mission3Hotel_Manager_ShengChiehLee.Page.Checkout,"A second return location was accepted.");UnityEngine.Object.DestroyImmediate(fake.gameObject);AtDesk();
                foreach(var p in props.Where(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.GuestNote))manager.Interact(p);
                foreach(var p in props.Where(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.LostProperty)){manager.Interact(p);manager.Interact(p);}
                Require(manager.CollectedCount==6,"Collectible duplication or pickup failure.");
                ui.Map();ui.Refresh();stage++;return;
            }
            if(stage==4)
            {
                Require(ui.itemMarkers.All(x=>x.GetComponentInChildren<TMP_Text>().text=="+"),"Collected map markers did not update.");Shot("05-collected-map");
                manager.Interact(props.First(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.Checkout));manager.CompleteCheckout();Require(!manager.IsCompleted,"Unassigned items finalized.");stage++;return;
            }
            if(stage==5)
            {
                for(int i=0;i<6;i++)ui.ownerButtons[i*3+i/2].onClick.Invoke();ui.Refresh();
                Require(ui.reviewButton.interactable,"Fully assigned checkout did not unlock review.");Shot("06-checkout");
                ui.Journal();ui.Begin();Require(manager.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.Checkout&&ui.reviewButton.interactable,"Journal did not return to checkout with assignments retained.");
                ui.reviewButton.onClick.Invoke();Require(manager.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.Confirm,"Review button is not bound.");
                manager.Interact(props.First(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.ServiceDoor));
                Require(manager.ShortcutOpened&&!manager.serviceDoor.activeSelf,"Hidden passage did not open.");
                int events=0;manager.onMissionCompleted.AddListener(_=>events++);ui.Finish();ui.Finish();ui.Refresh();
                Require(manager.ReturnsSubmitted&&!manager.IsCompleted&&manager.FinalScore==0&&events==0,"Desk return must not settle the score.");
                Require(manager.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.None&&!ui.pages[7].activeInHierarchy&&ui.ownerButtons.All(x=>!x.interactable),"Desk did not release the player and lock return choices.");
                Require(manager.guide.ObjectiveText.Contains("ENTER THE LIFT"),"No lift instruction after returns.");
                Shot("13-returned-at-desk");manager.UseLiftTerminal();manager.LoadNextLevel();manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Result);
                Require(!manager.IsCompleted&&events==0&&manager.CurrentPage!=mission3Hotel_Manager_ShengChiehLee.Page.Result,"Score or next level was available from the desk.");
                InsideLift();manager.UseLiftTerminal();manager.UseLiftTerminal();
                Require(manager.FinalScore==100&&events==1,"Lift scoring or one-shot completion failed.");
                OutsideLift();ui.Refresh();Require(!manager.CanEnterNextLevel&&!ui.resultsCanvas.continueButton.interactable,"Outside-lift travel was enabled.");
                ui.resultsCanvas.continueButton.onClick.Invoke();manager.UseLiftTerminal();
                Require(manager.gameObject.scene.path.EndsWith("Mission3_Demo.unity")&&manager.CurrentPage!=mission3Hotel_Manager_ShengChiehLee.Page.Result,"Remote result or travel bypassed the lift.");
                InsideLift();manager.UseLiftTerminal();Require(manager.FinalScore==100&&events==1,"Reopening the lift result changed the score.");stage++;return;
            }
            if(stage==6)
            {
                ui.Refresh();var results=ui.resultsCanvas;
                Require(results!=null&&results.gameObject.activeInHierarchy,"Results Canvas did not open.");
                Require(results.totalScore.text=="100"&&results.itemPoints.text=="+60"&&results.pairPoints.text=="+30"&&results.explorationPoints.text=="+10","Score breakdown does not match settlement.");
                Require(results.itemStates.All(x=>x.text=="RETURNED  /  +10")&&results.ownerLabels.Select(x=>x.text).SequenceEqual(new[]{"301","301","302","302","303","303"}),"Per-item settlement is incorrect.");
                string savedDestination=manager.nextSceneName;manager.nextSceneName="Mission3-Missing-Destination";manager.RefreshNextLevelStatus();
                Require(!results.continueButton.interactable&&manager.nextLevelButton.interactable&&results.nextLevelHint.text.Contains("not available"),"Missing destination did not disable travel controls.");
                manager.LoadNextLevel();Require(manager.gameObject.scene.isLoaded&&manager.FinalScore==100,"Missing destination lost the settled score.");
                manager.nextSceneName=savedDestination;manager.RefreshNextLevelStatus();
                Require(results.replayButton.interactable,"Replay button is disabled after settlement.");
                Shot("07-result");WorldShot("08-maze-corridor",mission3Space_Editor_ShengChiehLee.Expanded(new Vector3(9,2.4f,-16)),mission3Space_Editor_ShengChiehLee.Expanded(new Vector3(9,2.7f,-29)));
                var compass=ui.itemPositions[0].position;ui.itemPositions[0].gameObject.SetActive(true);WorldShot("09-brass-compass",compass+new Vector3(0,.8f,-1.1f),compass);ui.itemPositions[0].gameObject.SetActive(false);
                var fern=ui.itemPositions[5].position;ui.itemPositions[5].gameObject.SetActive(true);WorldShot("10-pressed-fern",fern+new Vector3(0,.75f,-1),fern);ui.itemPositions[5].gameObject.SetActive(false);
                ui.itemPositions[0].gameObject.SetActive(true);WorldShot("11-side-display",mission3Space_Editor_ShengChiehLee.Expanded(new Vector3(18,2,-35)),mission3Space_Editor_ShengChiehLee.Expanded(new Vector3(18,1.7f,-29.15f)));ui.itemPositions[0].gameObject.SetActive(false);
                var notice=manager.gameObject.scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Single(x=>x.name=="CheckoutFlowNotice");
                WorldShot("14-checkout-notice",notice.position+new Vector3(0,.2f,-3.5f),notice.position);
                Require(!hadError,"Unity reported a runtime error; inspect the Editor log.");
                stage=7;results.replayButton.onClick.Invoke();return;
            }
        }
        catch(Exception e){Stop("PLAYMODE_FAIL\n"+e);Debug.LogException(e);}
    }
    static void AtDesk()
    {
        var point=manager.returnDesk.transform.position+Vector3.back*2.1f;Move(point);
    }
    static void InsideLift(){manager.departureLift.Open();Move(manager.departureLift.transform.TransformPoint(new Vector3(0,0,.95f)));}
    static void OutsideLift(){Move(manager.departureLift.transform.TransformPoint(new Vector3(0,0,-1.2f)));}
    static void Move(Vector3 point)
    {
        manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.None);var capsule=manager.playerBody.GetComponent<CapsuleCollider>();point.y=capsule.height*capsule.transform.lossyScale.y/2+.08f;
        manager.playerBody.transform.position=point;manager.playerBody.position=point;if(!manager.playerBody.isKinematic)manager.playerBody.linearVelocity=Vector3.zero;Physics.SyncTransforms();
    }
    static void Stop(string message)
    {
        SessionState.SetBool(Flag,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
        File.WriteAllText("Library/Mission3UpgradeTest.txt",message);mission3Session_Editor_ShengChiehLee.Write(message);EditorApplication.ExitPlaymode();
    }
    static void Shot(string name)
    {
        ui.Refresh();var canvas=ui.GetComponent<Canvas>();var camera=manager.playerCamera;
        var resultCanvas=ui.resultsCanvas==null?null:ui.resultsCanvas.GetComponent<Canvas>();
        var resultMode=resultCanvas==null?RenderMode.ScreenSpaceOverlay:resultCanvas.renderMode;var resultCamera=resultCanvas==null?null:resultCanvas.worldCamera;float resultPlane=resultCanvas==null?0:resultCanvas.planeDistance;
        var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldTarget=camera.targetTexture;float oldPlane=canvas.planeDistance;
        var data=camera.GetComponent<UniversalAdditionalCameraData>();bool oldPost=data!=null&&data.renderPostProcessing;
        var target=new RenderTexture(1600,1000,24);target.Create();var active=RenderTexture.active;Texture2D image=null;
        try
        {
            // Game UI is normally Overlay and unaffected by camera post-processing. Match that clarity for offscreen previews.
            if(data!=null)data.renderPostProcessing=false;
            if(resultCanvas!=null){resultCanvas.renderMode=RenderMode.ScreenSpaceCamera;resultCanvas.worldCamera=camera;resultCanvas.planeDistance=.4f;}
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;camera.targetTexture=target;Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());
        }
        finally
        {
            if(resultCanvas!=null){resultCanvas.renderMode=resultMode;resultCanvas.worldCamera=resultCamera;resultCanvas.planeDistance=resultPlane;}
            camera.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;RenderTexture.active=active;if(data!=null)data.renderPostProcessing=oldPost;
            if(image!=null)UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
    }
    public static void CaptureCheckoutNotice()
    {
        manager=UnityEngine.Object.FindFirstObjectByType<mission3Hotel_Manager_ShengChiehLee>();ui=manager.canvasUI;
        var notice=manager.gameObject.scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Single(x=>x.name=="CheckoutFlowNotice");
        WorldShot("14-checkout-notice",notice.position+new Vector3(0,.2f,-3.5f),notice.position);
    }
    static void WorldShot(string name,Vector3 position,Vector3 target)
    {
        var camera=manager.playerCamera;var pos=camera.transform.position;var rotation=camera.transform.rotation;
        var playerRenderers=manager.playerBody.GetComponentsInChildren<Renderer>(true);var visible=playerRenderers.Select(x=>x.enabled).ToArray();
        foreach(var renderer in playerRenderers)renderer.enabled=false;
        ui.gameObject.SetActive(false);
        try{camera.transform.position=position;camera.transform.LookAt(target);ShotWorldCamera(camera,name);}
        finally{camera.transform.position=pos;camera.transform.rotation=rotation;ui.gameObject.SetActive(true);for(int i=0;i<playerRenderers.Length;i++)playerRenderers[i].enabled=visible[i];}
    }
    static void ShotWorldCamera(Camera camera,string name)
    {
        var target=new RenderTexture(1600,1000,24);target.Create();var old=camera.targetTexture;var active=RenderTexture.active;Texture2D image=null;
        try{camera.targetTexture=target;RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;if(image!=null)UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
