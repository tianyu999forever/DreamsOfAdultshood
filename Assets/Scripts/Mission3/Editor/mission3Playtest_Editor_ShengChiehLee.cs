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
    static int frame,stage;
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
            if(++frame<50)return;frame=0;
            if(stage==0)
            {
                manager=UnityEngine.Object.FindFirstObjectByType<mission3Hotel_Manager_ShengChiehLee>();ui=manager.canvasUI;
                props=UnityEngine.Object.FindObjectsByType<mission3Interact_Prop_ShengChiehLee>(FindObjectsSortMode.None);
                Require(manager.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.Welcome&&manager.playerBody.isKinematic,"Welcome modal failed to freeze the player.");
                Shot("01-welcome");ui.Begin();stage++;return;
            }
            if(stage==1)
            {
                Require(manager.movement.enabled&&manager.mouseLook.enabled&&manager.headBob.enabled,"Player controls were not restored.");
                Require(manager.playerBody.position.y>.9f&&manager.playerBody.position.y<1.8f,"Player collider or reception floor failed.");
                Shot("02-exploration");ui.Map();ui.Refresh();stage++;return;
            }
            if(stage==2)
            {
                // Editor update callbacks can outpace player LateUpdate while the Game view is unfocused.
                ui.Refresh();
                Require(ui.pages[2].activeInHierarchy&&!manager.movement.enabled&&!manager.headBob.enabled,"Map modal movement lock failed: page="+manager.CurrentPage+", panel="+ui.pages[2].activeInHierarchy+", move="+manager.movement.enabled+", bob="+manager.headBob.enabled);
                Require(ui.itemMarkers.All(x=>x.rectTransform.anchorMin.x>0&&x.rectTransform.anchorMin.x<1&&x.rectTransform.anchorMin.y>0&&x.rectTransform.anchorMin.y<1),"Map marker fell outside the floor plan.");
                Shot("03-floor-plan");
                manager.Interact(props.First(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.GuestNote));stage++;return;
            }
            if(stage==3)
            {
                Require(manager.notesRead.Count(x=>x)==1,"Guest note was not retained.");Shot("04-guest-note");
                manager.Interact(props.First(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.Checkout));Require(!manager.IsCompleted,"Empty checkout was allowed.");
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
                ui.reviewButton.onClick.Invoke();Require(manager.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.Confirm,"Review button is not bound.");
                manager.Interact(props.First(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.ServiceDoor));
                Require(manager.ShortcutOpened&&!manager.serviceDoor.activeSelf,"Hidden passage did not open.");
                int events=0;manager.onMissionCompleted.AddListener(_=>events++);ui.Finish();ui.Finish();
                Require(manager.FinalScore==100&&events==1,"Scoring or one-shot completion failed.");stage++;return;
            }
            if(stage==6)
            {
                Shot("07-result");WorldShot("08-maze-corridor",new Vector3(9,2.4f,-16),new Vector3(9,2.2f,-29));
                var compass=ui.itemPositions[0].position;ui.itemPositions[0].gameObject.SetActive(true);WorldShot("09-brass-compass",compass+new Vector3(0,.8f,-1.1f),compass);ui.itemPositions[0].gameObject.SetActive(false);
                var fern=ui.itemPositions[5].position;ui.itemPositions[5].gameObject.SetActive(true);WorldShot("10-pressed-fern",fern+new Vector3(0,.75f,-1),fern);ui.itemPositions[5].gameObject.SetActive(false);
                Require(!hadError,"Unity reported a runtime error; inspect the Editor log.");
                Stop("PLAYMODE_PASS\nWelcome, movement/head bob/physics, map pause, all marker positions, notes, duplicate pickup guard, collected marker changes, invalid checkout, owner buttons, review/confirm, secret door, 100-point result and one-shot completion verified.\nTen preview images captured. The editor remains open; Play mode stopped.");
            }
        }
        catch(Exception e){Stop("PLAYMODE_FAIL\n"+e);Debug.LogException(e);}
    }
    static void Stop(string message)
    {
        SessionState.SetBool(Flag,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
        File.WriteAllText("Library/Mission3UpgradeTest.txt",message);mission3Session_Editor_ShengChiehLee.Write(message);EditorApplication.ExitPlaymode();
    }
    static void Shot(string name)
    {
        ui.Refresh();var canvas=ui.GetComponent<Canvas>();var camera=manager.playerCamera;
        var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldTarget=camera.targetTexture;float oldPlane=canvas.planeDistance;
        var data=camera.GetComponent<UniversalAdditionalCameraData>();bool oldPost=data!=null&&data.renderPostProcessing;
        var target=new RenderTexture(1600,1000,24);target.Create();var active=RenderTexture.active;Texture2D image=null;
        try
        {
            // Game UI is normally Overlay and unaffected by camera post-processing. Match that clarity for offscreen previews.
            if(data!=null)data.renderPostProcessing=false;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;camera.targetTexture=target;Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;RenderTexture.active=active;if(data!=null)data.renderPostProcessing=oldPost;
            if(image!=null)UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
    }
    static void WorldShot(string name,Vector3 position,Vector3 target)
    {
        var camera=manager.playerCamera;var pos=camera.transform.position;var rotation=camera.transform.rotation;
        ui.gameObject.SetActive(false);
        try{camera.transform.position=position;camera.transform.LookAt(target);ShotWorldCamera(camera,name);}
        finally{camera.transform.position=pos;camera.transform.rotation=rotation;ui.gameObject.SetActive(true);}
    }
    static void ShotWorldCamera(Camera camera,string name)
    {
        var target=new RenderTexture(1600,1000,24);target.Create();var old=camera.targetTexture;var active=RenderTexture.active;Texture2D image=null;
        try{camera.targetTexture=target;RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;if(image!=null)UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
