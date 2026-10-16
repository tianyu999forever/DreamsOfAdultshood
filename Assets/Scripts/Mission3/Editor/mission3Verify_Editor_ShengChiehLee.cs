using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class mission3Verify_Editor_ShengChiehLee
{
    const string Flag="Mission3.SmokeRunning";
    static double began;
    static int stage, frames;
    static bool hadError;
    static mission3Hotel_Manager_ShengChiehLee manager;
    static mission3Verify_Editor_ShengChiehLee()
    {
        if(SessionState.GetBool(Flag,false)) { began=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;Application.logMessageReceived+=Log; }
    }
    public static void SmokeTest()
    {
        EditorSceneManager.OpenScene(mission3Build_Editor_ShengChiehLee.ScenePath);
        SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;
    }
    static void Log(string text,string stack,LogType type)
    {
        // Unity 6000.3 can report a startup search-index exception unrelated to scene code.
        if(stack.Contains("UnityEditor.Search.SearchDatabase") && !stack.Contains("Assets/")) { Debug.LogWarning("Mission3 test: editor SearchDatabase startup exception excluded; see original stack."); return; }
        if(type==LogType.Exception || type==LogType.Error || type==LogType.Assert)hadError=true;
    }
    static void Require(bool ok,string why) { if(!ok)throw new Exception(why); }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup-began>90)throw new Exception("Mission3 play-mode smoke test timed out");
            if(!EditorApplication.isPlaying)return;
            if(++frames<60)return;
            if(stage==0)
            {
                manager=UnityEngine.Object.FindFirstObjectByType<mission3Hotel_Manager_ShengChiehLee>();Require(manager!=null,"Manager did not start");
                Require(manager.playerBody.isKinematic,"Welcome screen failed to freeze player");
                if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();
                var enumType=typeof(mission3Hotel_Manager_ShengChiehLee).GetNestedType("Page",BindingFlags.NonPublic);
                typeof(mission3Hotel_Manager_ShengChiehLee).GetMethod("SetPage",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(manager,new[]{Enum.Parse(enumType,"None")});
                Require(manager.movement.enabled&&manager.mouseLook.enabled,"Shared movement was not restored");stage=1;frames=0;return;
            }
            Require(manager.playerBody.position.y>.7f&&manager.playerBody.position.y<1.3f,"Player fell through reception floor");
            var props=UnityEngine.Object.FindObjectsByType<mission3Interact_Prop_ShengChiehLee>(FindObjectsSortMode.None);
            manager.Interact(props.First(p=>p.kind==mission3Interact_Prop_ShengChiehLee.Kind.Checkout));Require(!manager.IsCompleted,"Empty checkout completed");
            foreach(var p in props.Where(p=>p.kind==mission3Interact_Prop_ShengChiehLee.Kind.GuestNote))manager.Interact(p);
            Require(manager.notesRead.All(x=>x),"Journal did not retain notes");
            foreach(var p in props.Where(p=>p.kind==mission3Interact_Prop_ShengChiehLee.Kind.LostProperty)){manager.Interact(p);manager.Interact(p);}
            Require(manager.collected.Count(x=>x)==6,"Pickup state invalid");
            manager.Interact(props.First(p=>p.kind==mission3Interact_Prop_ShengChiehLee.Kind.ServiceDoor));Require(!manager.serviceDoor.activeSelf,"Shortcut collider remained active");
            manager.CompleteCheckout();Require(!manager.IsCompleted,"Unassigned items finalized");
            for(int i=0;i<6;i++)manager.assignment[i]=i/2;int events=0;manager.onMissionCompleted.AddListener(s=>events++);
            manager.CompleteCheckout();manager.CompleteCheckout();Require(manager.FinalScore==100&&events==1,"Scoring or one-shot completion failed");
            Require(!hadError,"Runtime errors occurred; inspect log");
            Debug.Log("MISSION3_PLAYMODE_PASS: welcome/pause, shared movement, floor physics, pickups, journal, shortcut, invalid checkout, 100-point result, one-shot event");
            SessionState.SetBool(Flag,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);
        }
        catch(Exception e){SessionState.SetBool(Flag,false);EditorApplication.update-=Tick;Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void Capture()
    {
        Directory.CreateDirectory("../outputs/mission3-preview");
        AssetDatabase.ExportPackage(new[]{mission3Build_Editor_ShengChiehLee.ScenePath,"Assets/Mission3","Assets/Scripts/Mission3"}, "../outputs/mission3-preview/Mission3_Hotel24_ShengChiehLee.unitypackage", ExportPackageOptions.Recurse);
        EditorSceneManager.OpenScene(mission3Build_Editor_ShengChiehLee.ScenePath);
        var m=UnityEngine.Object.FindFirstObjectByType<mission3Hotel_Manager_ShengChiehLee>();var cam=m.playerCamera;
        Directory.CreateDirectory("../outputs/mission3-preview");
        RenderSettings.ambientMode=AmbientMode.Flat;
        Shot(cam,"entrance",cam.transform.position,cam.transform.rotation);
        Shot(cam,"lounge",new Vector3(36,1.7f,-49),Quaternion.LookRotation(new Vector3(0,0,1)));
        Debug.Log("MISSION3_CAPTURE_PASS");
    }
    static void Shot(Camera cam,string name,Vector3 pos,Quaternion rotation)
    {
        cam.transform.position=pos;cam.transform.rotation=rotation;
        var rt=new RenderTexture(1440,900,24);rt.Create();
        var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};RenderPipeline.SubmitRenderRequest(cam,request);
        var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();File.WriteAllBytes("../outputs/mission3-preview/"+name+".png",tex.EncodeToPNG());RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
}
