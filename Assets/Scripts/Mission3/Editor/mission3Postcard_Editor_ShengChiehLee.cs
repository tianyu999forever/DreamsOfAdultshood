using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.AI.Navigation;
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
public static class mission3Postcard_Editor_ShengChiehLee
{
    const string Output="C:/Users/j1a2m/Documents/Codex/2026-10-25/w/outputs/mission3-postcard";
    const string Pending="Mission3.Postcard.Apply",Preview="Mission3.Postcard.Preview";
    static bool editReady;
    static int stage,frame,nextGameFrame;
    static mission3Hotel_Manager_ShengChiehLee manager;
    static mission3Interact_Prop_ShengChiehLee postcard;
    static Keyboard testKeyboard;
    static mission3Postcard_Editor_ShengChiehLee()
    {
        editReady=!EditorApplication.isPlayingOrWillChangePlaymode&&!Application.isPlaying;
        EditorApplication.playModeStateChanged+=state=>editReady=state==PlayModeStateChange.EnteredEditMode;
        EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        if(File.Exists("Library/Mission3MovePostcard.txt")){File.Delete("Library/Mission3MovePostcard.txt");SessionState.SetBool(Pending,true);}
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists("Library/Mission3InspectPostcard.txt"))
        {
            if(!EditorApplication.isPlaying){if(editReady)EditorApplication.EnterPlaymode();return;}
            if(++frame<30)return;frame=0;
            File.Delete("Library/Mission3InspectPostcard.txt");InspectRuntime();return;
        }
        if(SessionState.GetBool(Pending,false))
        {
            if(!editReady){if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
            SessionState.SetBool(Pending,false);
            try{Apply();EditorSceneManager.OpenScene(mission3Monster_Editor_ShengChiehLee.Demo);stage=frame=0;SessionState.SetBool(Preview,true);var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");if(type!=null){var window=EditorWindow.GetWindow(type,false,"Game",true);window.Show();window.Focus();}EditorApplication.isPaused=false;EditorApplication.EnterPlaymode();}
            catch(Exception e){Finish(false,e.ToString());}return;
        }
        if(!SessionState.GetBool(Preview,false)||!EditorApplication.isPlaying||++frame<20||(stage>0&&Time.frameCount<nextGameFrame))return;frame=0;
        try
        {
            if(stage==0)
            {
                Application.runInBackground=true;Time.timeScale=1;
                manager=mission3Monster_Editor_ShengChiehLee.Find(SceneManager.GetActiveScene());postcard=manager.canvasUI.itemPositions[1].GetComponent<mission3Interact_Prop_ShengChiehLee>();
                manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.None);nextGameFrame=Time.frameCount+6;stage++;return;
            }
            if(stage==1)
            {
                manager.movement.enabled=false;manager.mouseLook.enabled=false;manager.headBob.enabled=false;
                var capsule=manager.playerBody.GetComponent<CapsuleCollider>();var ground=postcard.transform.position-postcard.transform.forward*1.65f;ground.y=capsule.height*capsule.transform.lossyScale.y/2+.04f;
                manager.playerBody.position=ground;manager.playerBody.transform.position=ground;manager.playerBody.linearVelocity=Vector3.zero;
                var visiblePaper=postcard.transform.Find("Blue coastline").GetComponent<Renderer>();
                manager.playerCamera.transform.LookAt(visiblePaper.bounds.center);Physics.SyncTransforms();nextGameFrame=Time.frameCount+6;stage++;return;
            }
            if(stage==2)
            {
                manager.mouseLook.enabled=false;
                var visiblePaper=postcard.transform.Find("Blue coastline").GetComponent<Renderer>();manager.playerCamera.transform.LookAt(visiblePaper.bounds.center);Physics.SyncTransforms();
                var screen=manager.playerCamera.WorldToViewportPoint(visiblePaper.bounds.center);
                if(Vector2.Distance(new Vector2(screen.x,screen.y),new Vector2(.5f,.5f))>.01f)throw new Exception("Visible postcard model is not under the crosshair: "+screen+" / camera="+manager.playerCamera.transform.position+" / paper="+visiblePaper.bounds.center);
                typeof(mission3Hotel_Manager_ShengChiehLee).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,null);
                if(!manager.FocusPrompt.Contains("Coastal Postcard"))throw new Exception("Aiming at the visible postcard model did not focus the postcard: "+manager.FocusPrompt);
                Capture();testKeyboard=InputSystem.AddDevice<Keyboard>("PostcardPickupTest");InputSystem.EnableDevice(testKeyboard);testKeyboard.MakeCurrent();
                InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.E));InputSystem.Update();
                // Execute the real Update input/raycast path in the same input frame, before Editor updates consume the press.
                typeof(mission3Hotel_Manager_ShengChiehLee).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,null);
            }
            if(!manager.collected[1]||postcard.gameObject.activeSelf||manager.CollectedCount!=1)throw new Exception("The real E interaction did not collect exactly the postcard. count="+manager.CollectedCount+"; active="+postcard.gameObject.activeSelf+"; page="+manager.CurrentPage+"; prompt="+manager.FocusPrompt+"; key="+testKeyboard.eKey.isPressed+"; press="+testKeyboard.eKey.wasPressedThisFrame);
            Finish(true,"Both scenes saved. Corrected the demo's 2.05-metre offset between the visible postcard/label and its interaction root. Paper, coastline, stamp, label, display and pickup now align. Three standing approaches and raycasts aimed at actual renderer centers checked. Runtime crosshair is centered on the visible blue coastline model and the real E-input/raycast path collects the postcard. Navigation and map updated. Fresh demo opened; no commit or push.");
        }
        catch(Exception e){Finish(false,e.ToString());}
    }
    static void InspectRuntime()
    {
        manager=mission3Monster_Editor_ShengChiehLee.Find(SceneManager.GetActiveScene());postcard=manager.canvasUI.itemPositions[1].GetComponent<mission3Interact_Prop_ShengChiehLee>();
        var camera=manager.playerCamera;var box=postcard.GetComponent<BoxCollider>();
        string data="Page="+manager.CurrentPage+"; returns="+manager.ReturnsSubmitted+"; collected="+manager.CollectedCount+"; postcard="+manager.collected[1]+"; focus="+manager.FocusPrompt+"\nCamera="+camera.transform.position+" forward="+camera.transform.forward+"\nPostcard="+postcard.transform.position+" rotation="+postcard.transform.eulerAngles+" active="+postcard.gameObject.activeInHierarchy+" collider="+box.bounds+"\n";
        foreach(var renderer in postcard.GetComponentsInChildren<Renderer>(true))data+="MODEL "+renderer.name+" position="+renderer.transform.position+" bounds="+renderer.bounds+" enabled="+renderer.enabled+"\n";
        foreach(var hit in Physics.RaycastAll(camera.transform.position,camera.transform.forward,12,~0,QueryTriggerInteraction.Collide).OrderBy(x=>x.distance))
        {var prop=hit.collider.GetComponentInParent<mission3Interact_Prop_ShengChiehLee>();data+="RAY "+hit.distance+" "+hit.collider.name+" trigger="+hit.collider.isTrigger+" action="+(prop==null?"none":prop.kind+" / "+prop.index+" / "+prop.displayName)+"\n";}
        foreach(var keyboard in InputSystem.devices.OfType<Keyboard>())data+="KEYBOARD "+keyboard.name+" native="+keyboard.native+" enabled="+keyboard.enabled+" current="+(keyboard==Keyboard.current)+" E="+keyboard.eKey.isPressed+"\n";
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"/live-inspection.txt",data);Capture();
    }
    [MenuItem("Mission3/Move Coastal Postcard to Accessible Display")]
    public static void Apply()
    {
        foreach(string path in new[]{mission3Paths_Editor_ShengChiehLee.Scene,mission3Monster_Editor_ShengChiehLee.Demo})
        {
            var scene=EditorSceneManager.OpenScene(path);manager=mission3Monster_Editor_ShengChiehLee.Find(scene);
            mission3Space_Editor_ShengChiehLee.RelocatePostcard(scene);
            postcard=manager.canvasUI.itemPositions[1].GetComponent<mission3Interact_Prop_ShengChiehLee>();ValidateApproach(scene);
            mission3Presentation_Editor_ShengChiehLee.MakeMap(manager);manager.canvasUI.Refresh();
            var surface=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<NavMeshSurface>(true)).Single();
            var existing=surface.navMeshData;surface.BuildNavMesh();var data=surface.navMeshData;
            surface.RemoveData();EditorUtility.CopySerialized(data,existing);surface.navMeshData=existing;surface.AddData();UnityEngine.Object.DestroyImmediate(data);EditorUtility.SetDirty(existing);EditorUtility.SetDirty(surface);
            mission3Space_Editor_ShengChiehLee.Validate(scene);mission3Monster_Editor_ShengChiehLee.Validate(scene);
            EditorUtility.SetDirty(postcard);EditorUtility.SetDirty(postcard.transform);EditorUtility.SetDirty(postcard.GetComponent<BoxCollider>());EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save "+path);
        }
        AssetDatabase.SaveAssets();
    }
    static void ValidateApproach(Scene scene)
    {
        var paper=postcard.transform.Find("Paper").GetComponent<Renderer>();var coastline=postcard.transform.Find("Blue coastline").GetComponent<Renderer>();var label=postcard.transform.Find("Property label");
        if(Vector3.Distance(paper.bounds.center,postcard.transform.position)>.02f)throw new Exception("Postcard model and pickup root are misaligned.");
        if(!postcard.GetComponent<BoxCollider>().bounds.Contains(label.position))throw new Exception("Postcard name label is outside the pickup volume.");
        var capsule=manager.playerBody.GetComponent<CapsuleCollider>();float radius=capsule.radius*Mathf.Max(capsule.transform.lossyScale.x,capsule.transform.lossyScale.z),height=capsule.height*capsule.transform.lossyScale.y;
        float eyes=height/2+.04f+manager.playerCamera.transform.position.y-manager.playerBody.position.y;
        foreach(float side in new[]{-.4f,0,.4f})
        {
            var front=postcard.transform.position-postcard.transform.forward*1.65f+postcard.transform.right*side;front.y=eyes;
            foreach(var target in new[]{paper.bounds.center,coastline.bounds.center,label.position})
            {
                var direction=target-front;
                var hits=Physics.RaycastAll(front,direction.normalized,3.4f,~0,QueryTriggerInteraction.Collide).Where(x=>x.collider.gameObject.scene==scene&&!x.transform.IsChildOf(manager.playerBody.transform)).OrderBy(x=>x.distance).ToArray();
                if(hits.Length==0||hits[0].collider.GetComponentInParent<mission3Interact_Prop_ShengChiehLee>()!=postcard)throw new Exception("Postcard model aim blocked: "+side+" / "+target+" / "+(hits.Length==0?"no hit":hits[0].collider.name));
            }
            var low=new Vector3(front.x,radius+.04f,front.z);var high=new Vector3(front.x,height-radius+.04f,front.z);
            if(Physics.OverlapCapsule(low,high,radius,~0,QueryTriggerInteraction.Ignore).Any(x=>x.gameObject.scene==scene&&!x.transform.IsChildOf(manager.playerBody.transform)))throw new Exception("Postcard standing space is blocked.");
        }
    }
    static void Capture()
    {
        Directory.CreateDirectory(Output);var camera=manager.playerCamera;var canvas=manager.canvasUI.GetComponent<Canvas>();var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float plane=canvas.planeDistance;
        var data=camera.GetComponent<UniversalAdditionalCameraData>();bool post=data.renderPostProcessing;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
        var renderers=manager.playerBody.GetComponentsInChildren<Renderer>(true);var visible=renderers.Select(x=>x.enabled).ToArray();var target=new RenderTexture(1600,1000,24);target.Create();Texture2D image=null;
        try
        {
            foreach(var renderer in renderers)renderer.enabled=false;data.renderPostProcessing=false;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;camera.targetTexture=target;Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Output+"/postcard.png",image.EncodeToPNG());
        }
        finally{canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=plane;data.renderPostProcessing=post;camera.targetTexture=oldTarget;RenderTexture.active=oldActive;for(int i=0;i<renderers.Length;i++)renderers[i].enabled=visible[i];if(image!=null)UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
    static void Finish(bool pass,string message)
    {
        if(testKeyboard!=null){InputSystem.RemoveDevice(testKeyboard);testKeyboard=null;}
        SessionState.SetBool(Preview,false);Directory.CreateDirectory(Output);string result=(pass?"POSTCARD_MOVE_PASS":"POSTCARD_MOVE_FAIL")+"\n"+message;File.WriteAllText(Output+"/validation.txt",result);mission3Session_Editor_ShengChiehLee.Write(result);
        if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();if(pass)mission3Monster_Editor_ShengChiehLee.FreshDemo();
    }
}
