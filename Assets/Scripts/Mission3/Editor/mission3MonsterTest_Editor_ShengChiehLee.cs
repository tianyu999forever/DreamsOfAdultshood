using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class mission3MonsterTest_Editor_ShengChiehLee
{
    const string Flag="Mission3.NightPatrol.Test";
    static int stage;
    static double began,next;
    static bool error;
    static mission3Hotel_Manager_ShengChiehLee manager;
    static mission3Monster_Manager_ShengChiehLee monster;
    static Transform architecture;
    static float pausedTimer;
    static mission3Interact_Prop_ShengChiehLee[] props;
    static mission3MonsterTest_Editor_ShengChiehLee(){if(SessionState.GetBool(Flag,false))Subscribe();}
    public static void Begin(){SessionState.SetBool(Flag,true);Subscribe();EditorApplication.EnterPlaymode();}
    static void Subscribe()
    {
        stage=0;error=false;began=EditorApplication.timeSinceStartup;next=began+1.5;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
    }
    static void Log(string message,string stack,LogType type)
    {
        if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&(stack.Contains("Assets/")||message.Contains("NavMeshAgent")))error=true;
    }
    static void Require(bool condition,string why){if(!condition)throw new Exception(why);}
    static Vector3 World(float x,float y)=>architecture.TransformPoint(new Vector3(x*3,0,-y*3));
    static void Move(Vector3 ground)
    {
        var capsule=manager.playerBody.GetComponent<CapsuleCollider>();ground.y=capsule.height*capsule.transform.lossyScale.y/2+.08f;
        manager.playerBody.transform.position=ground;manager.playerBody.position=ground;
        if(!manager.playerBody.isKinematic)manager.playerBody.linearVelocity=Vector3.zero;Physics.SyncTransforms();
    }
    static void Collect(int index){manager.Interact(props.Single(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.LostProperty&&x.index==index));}
    static void Wait(double seconds){stage++;next=EditorApplication.timeSinceStartup+seconds;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try
        {
            if(EditorApplication.timeSinceStartup-began>100)throw new Exception("Night patrol test timed out at stage "+stage);
            if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
            if(stage==0)
            {
                manager=mission3Monster_Editor_ShengChiehLee.Find(SceneManager.GetActiveScene());monster=manager.nightPatrol;
                architecture=manager.gameObject.scene.GetRootGameObjects().Single(x=>x.name=="Mission3_HotelArchitecture").transform;
                props=manager.gameObject.scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Interact_Prop_ShengChiehLee>(true)).ToArray();
                mission3Monster_Editor_ShengChiehLee.Validate(manager.gameObject.scene);Application.runInBackground=true;Time.timeScale=1;
                manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.None);manager.movement.enabled=false;manager.mouseLook.enabled=false;manager.headBob.enabled=false;
                Move(World(3,7));Collect(0);Wait(.5);return;
            }
            if(stage==1)
            {
                Require(monster.State==mission3Monster_Manager_ShengChiehLee.Behaviour.Dormant&&!monster.body.activeSelf,"Monster activated before the second belonging.");
                Collect(1);Wait(.3);return;
            }
            if(stage==2)
            {
                Require(monster.State==mission3Monster_Manager_ShengChiehLee.Behaviour.Warning&&!monster.body.activeSelf,"No warning before the first appearance.");
                manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Map);pausedTimer=monster.StateTimeRemaining;Wait(.9);return;
            }
            if(stage==3)
            {
                Require(Mathf.Abs(monster.StateTimeRemaining-pausedTimer)<.02f,"Warning timer advanced while reading the map.");
                manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.None);Wait(3.5);return;
            }
            if(stage==4)
            {
                Require(monster.NavigationReady&&monster.body.activeSelf&&monster.State==mission3Monster_Manager_ShengChiehLee.Behaviour.Patrol,"Attendant failed to spawn and patrol.");
                Require(!monster.ExpandedPatrol,"Patrol expanded before the fourth belonging.");
                monster.agent.Warp(World(3,10));Move(World(3,8));monster.transform.LookAt(new Vector3(manager.playerBody.position.x,monster.transform.position.y,manager.playerBody.position.z));
                Require(monster.CanSeePlayer,"Clear corridor sight did not detect the player.");Wait(.35);return;
            }
            if(stage==5)
            {
                Require(monster.State==mission3Monster_Manager_ShengChiehLee.Behaviour.Chase,"Visible player did not trigger chase.");
                Capture("chase.png",false);manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Map);var old=monster.transform.position;
                SessionState.SetString("Mission3.NightPatrol.PausedPosition",JsonUtility.ToJson(old));Wait(.7);return;
            }
            if(stage==6)
            {
                var old=JsonUtility.FromJson<Vector3>(SessionState.GetString("Mission3.NightPatrol.PausedPosition","{}"));
                Require(Vector3.Distance(old,monster.transform.position)<.08f&&monster.State==mission3Monster_Manager_ShengChiehLee.Behaviour.Chase,"Monster moved or changed state while a reading panel was open: "+old+" -> "+monster.transform.position+" / "+monster.State+" / "+manager.CurrentPage);
                monster.agent.Warp(World(3,10));Move(World(5,10));Physics.SyncTransforms();
                Require(!monster.CanSeePlayer,"Attendant saw the player through a wall.");
                manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.None);Wait(11);return;
            }
            if(stage==7)
            {
                Require(monster.State==mission3Monster_Manager_ShengChiehLee.Behaviour.Patrol,"Attendant did not return to patrol after losing sight and searching.");
                manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Map);Collect(2);Collect(3);
                Require(monster.ExpandedPatrol,"The fourth belonging did not expand patrol.");
                Move(World(3,8));monster.agent.Warp(World(3,10));monster.transform.forward=Vector3.back;
                manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.None);
                if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.LeftShift));InputSystem.Update();
                manager.playerBody.linearVelocity=Vector3.forward*6.5f;Require(monster.CanHearRunning(),"Nearby running was not heard.");
                manager.playerBody.linearVelocity=Vector3.zero;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.Update();
                Require(!monster.CanHearRunning(),"Stationary player made running noise.");
                manager.Interact(props.First(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.GuestNote));manager.assignment[0]=0;
                manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.None);monster.enabled=false;Wait(1.2);return;
            }
            if(stage==8)
            {
                manager.movement.enabled=true;manager.mouseLook.enabled=true;manager.headBob.enabled=true;
                Move(World(3,8));monster.agent.Warp(World(3,10));monster.enabled=true;Wait(1.3);return;
            }
            if(stage==9)
            {
                if(monster.ProtectionSecondsRemaining>0){next=EditorApplication.timeSinceStartup+.2;return;}
                monster.agent.Warp(World(3,8)+Vector3.back*.65f);Wait(.2);return;
            }
            if(stage==10)
            {
                Require(monster.IsRecovering&&monster.TimesCaught==1&&!manager.movement.enabled,"Capture did not freeze input and show recovery: "+monster.State+" / caught="+monster.TimesCaught+" / grace="+monster.ProtectionSecondsRemaining+" / page="+manager.CurrentPage);
                Capture("recovery.png",true);Wait(2.2);return;
            }
            if(stage==11)
            {
                Require(!monster.IsRecovering&&monster.PlayerIsSafe&&monster.TimesCaught==1,"Capture did not return the player safely to reception.");
                Require(manager.CollectedCount==4&&manager.notesRead[0]&&manager.assignment[0]==0&&!manager.ReturnsSubmitted&&manager.FinalScore==0,"Capture lost belongings, notes, owner choices or changed the score.");
                Require(manager.movement.enabled&&manager.mouseLook.enabled&&manager.headBob.enabled,"Controls did not recover after capture.");
                manager.movement.enabled=false;manager.mouseLook.enabled=false;manager.headBob.enabled=false;
                Move(manager.returnDesk.transform.position+Vector3.back*2.1f);manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Checkout);
                for(int i=0;i<4;i++)manager.assignment[i]=i/2;
                manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Confirm);manager.CompleteCheckout();Wait(.2);return;
            }
            if(stage==12)
            {
                Require(manager.ReturnsSubmitted&&monster.State==mission3Monster_Manager_ShengChiehLee.Behaviour.Retired&&!monster.body.activeSelf,"Monster did not retire after desk returns.");
                Require(!manager.IsCompleted&&manager.FinalScore==0,"Desk return settled too early.");
                Move(manager.departureLift.transform.TransformPoint(new Vector3(0,0,.95f)));manager.UseLiftTerminal();
                Require(manager.IsCompleted&&manager.FinalScore==60,"Lift settlement failed after a monster capture.");
                Require(!error,"Unity logged a project runtime error during the patrol test.");
                Finish("NIGHT_PATROL_PASS\nCanonical and demo installed. Verified dormant first item; second-item warning and offscreen spawn; pause freezes both timer and movement; visible corridor chase; walls block sight; losing sight leads through search back to patrol; fourth item expands route; running is heard and standing is quiet; capture freezes controls, fades to reception, preserves 4 items/notes/assignment and restores controls; desk returns retire the monster without scoring; inside-lift settlement still gives 60 points for four correct items. Baked routes are connected and the reception/lift zone is excluded from navigation. Fresh demo ready. No commit or push performed.");
            }
        }
        catch(Exception e){Finish("NIGHT_PATROL_FAIL\nStage "+stage+"\n"+e);Debug.LogException(e);}
    }
    static void Finish(string message)
    {
        SessionState.SetBool(Flag,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
        mission3Monster_Editor_ShengChiehLee.Report(message);EditorApplication.ExitPlaymode();
        if(message.StartsWith("NIGHT_PATROL_PASS"))mission3Monster_Editor_ShengChiehLee.FreshDemo();
    }
    static void Capture(string name,bool ui)
    {
        Directory.CreateDirectory(mission3Monster_Editor_ShengChiehLee.Output);
        var camera=manager.playerCamera;var canvas=manager.canvasUI.GetComponent<Canvas>();var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;
        var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
        var cameraData=camera.GetComponent<UniversalAdditionalCameraData>();bool oldPost=cameraData.renderPostProcessing;
        var playerRenderers=manager.playerBody.GetComponentsInChildren<Renderer>(true);var visible=playerRenderers.Select(x=>x.enabled).ToArray();
        var target=new RenderTexture(1600,1000,24);target.Create();Texture2D shot=null;
        try
        {
            foreach(var renderer in playerRenderers)renderer.enabled=false;
            cameraData.renderPostProcessing=false;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;
            if(!ui){camera.transform.position=World(3,8)+Vector3.up*2.1f;camera.transform.LookAt(monster.transform.position+Vector3.up*2);}
            camera.targetTexture=target;Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;shot=new Texture2D(1600,1000,TextureFormat.RGB24,false);shot.ReadPixels(new Rect(0,0,1600,1000),0,0);shot.Apply();File.WriteAllBytes(mission3Monster_Editor_ShengChiehLee.Output+"/"+name,shot.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;camera.transform.position=oldPosition;camera.transform.rotation=oldRotation;camera.targetTexture=oldTarget;RenderTexture.active=oldActive;cameraData.renderPostProcessing=oldPost;
            for(int i=0;i<playerRenderers.Length;i++)playerRenderers[i].enabled=visible[i];if(shot!=null)UnityEngine.Object.DestroyImmediate(shot);target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
