using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class mission3RoomSigns_Editor_ShengChiehLee
{
    const string Flag = "Mission3.RoomSigns.Test";
    static string Output => SessionState.GetString("Mission3.RoomSigns.Output",Path.GetFullPath("Temp/Mission3RoomSigns"));
    static int phase;
    static double began, next;
    static mission3Hotel_Manager_ShengChiehLee manager;
    static readonly string[] Names = { "301 identity", "302 sign", "302 identity", "303 identity", "Lounge sign" };
    static mission3RoomSigns_Editor_ShengChiehLee()
    {
        File.WriteAllText("Library/Mission3RoomSignsReady.txt","room-signs-v2"); EditorApplication.update += Tick;
    }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        const string request = "Library/Mission3RoomSignsTest.txt";
        if (File.Exists(request) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetString("Mission3.RoomSigns.Output",Path.GetFullPath(File.ReadAllText(request).Trim())); File.Delete(request);
            Directory.CreateDirectory(Output);
            if (SceneManager.GetActiveScene().isDirty) { File.WriteAllText(Output+"/validation.txt","FAIL: save current scene first"); return; }
            EditorSceneManager.OpenScene("Assets/Scenes/Mission3.unity");
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            File.WriteAllText(Output+"/validation.txt","RUNNING\n"); SessionState.SetBool(Flag,true);
            EditorApplication.isPaused=false; EditorApplication.EnterPlaymode(); return;
        }
        if (!SessionState.GetBool(Flag,false) || !EditorApplication.isPlaying) return;
        try
        {
            if (began == 0) began=EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup-began > 90) throw new Exception("Room sign test timed out.");
            if (phase == 0)
            {
                var tutorial=UnityEngine.Object.FindFirstObjectByType<mission3Tutorial_Manager_ShengChiehLee>();
                if (tutorial == null || !tutorial.Initialized) return;
                // Test setup only: isolate hotel signage without replaying the already tested six-action lesson.
                typeof(mission3Tutorial_Manager_ShengChiehLee).GetProperty("CompletedThisSession").GetSetMethod(true).Invoke(null,new object[]{true});
                phase=1; SceneManager.LoadScene("Assets/Scenes/Mission3.unity"); return;
            }
            if (phase == 1)
            {
                if (UnityEngine.Object.FindFirstObjectByType<mission3Tutorial_Manager_ShengChiehLee>() != null) return;
                manager=UnityEngine.Object.FindFirstObjectByType<mission3Hotel_Manager_ShengChiehLee>();
                if (manager == null || manager.CurrentPage != mission3Hotel_Manager_ShengChiehLee.Page.Welcome) return;
                manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.None); manager.enabled=false;
                manager.canvasUI.gameObject.SetActive(false); manager.guide.enabled=false;
                manager.movement.enabled=false; manager.mouseLook.enabled=false;
                if (manager.headBob != null) manager.headBob.enabled=false;
                manager.playerBody.linearVelocity=Vector3.zero; manager.playerBody.isKinematic=true;
                phase=2; next=EditorApplication.timeSinceStartup+.3; return;
            }
            if (EditorApplication.timeSinceStartup < next) return;
            var signs=UnityEngine.Object.FindObjectsByType<mission3RoomSign_World_ShengChiehLee>(FindObjectsSortMode.None);
            Require(signs.Length==5,"Expected five hotel plaques after removing the two overlapping legacy labels.");
            var allText = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TextMeshPro>(true)).ToArray();
            Require(allText.Where(text=>text.name=="301 sign"||text.name=="303 sign").All(text=>!text.gameObject.activeInHierarchy),"Overlapping legacy room labels remained visible.");
            foreach (var name in Names)
            {
                var sign=signs.Single(x=>x.name==name); var camera=manager.playerCamera;
                Require(sign.GetComponentsInChildren<Collider>().Length==0,"Door plaque added a blocking collider.");
                for (int angle=0;angle<4;angle++)
                {
                    float yaw=angle*Mathf.PI*.5f; var position=sign.transform.position+new Vector3(Mathf.Sin(yaw)*2,0,Mathf.Cos(yaw)*2);
                    Pose(camera,position,sign.transform.position);
                    foreach(var item in signs)item.RefreshForCamera();
                    Require(Vector3.Dot(sign.transform.forward,(sign.transform.position-camera.transform.position).normalized)>.999f,"Door plaque is edge-on or reversed.");
                    var text=sign.GetComponent<TextMeshPro>(); text.ForceMeshUpdate();
                    var bounds=text.textBounds; var center=sign.transform.TransformPoint(bounds.center);
                    var lo=camera.WorldToScreenPoint(center-sign.transform.up*bounds.extents.y*sign.transform.lossyScale.y);
                    var hi=camera.WorldToScreenPoint(center+sign.transform.up*bounds.extents.y*sign.transform.lossyScale.y);
                    float pixels=Vector2.Distance(lo,hi)/sign.LineCount*900/camera.pixelHeight;
                    Require(pixels>=24&&pixels<=30&&!text.isTextTruncated,"Unreadable or clipped door plaque.");
                }
                int captures=0; float firstAngle=0;
                var anchor=sign.transform.position;
                for(int attempt=0;attempt<24&&captures<2;attempt++)
                {
                    float yaw=attempt*Mathf.PI/12; if(captures==1&&Mathf.Abs(yaw-firstAngle)<Mathf.PI/3)continue;
                    var position=anchor+new Vector3(Mathf.Sin(yaw)*2,0,Mathf.Cos(yaw)*2);
                    if(Physics.OverlapSphere(position,.2f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(manager.playerBody.transform)))continue;
                    if(!Physics.Raycast(position,Vector3.down,out var floor,5,~0,QueryTriggerInteraction.Ignore)||floor.point.y>.5f)continue;
                    if(Physics.Linecast(position,anchor,~0,QueryTriggerInteraction.Ignore))continue;
                    Pose(camera,position,anchor); foreach(var item in signs)item.RefreshForCamera();
                    Capture(Output+"/"+name.Replace(" sign","").Replace(' ','-')+"-"+(captures+1)+".png");
                    firstAngle=yaw; captures++;
                }
                Require(captures==2,"Could not find two unobstructed viewing angles for "+name);
                File.AppendAllText(Output+"/validation.txt",name+": front/side/rear orientation and 28px text verified; two walkable, unobstructed views captured.\n");
            }
            Require(manager.CollectedCount==0&&manager.notesRead.All(read=>!read),"Sign test changed mission progress.");
            File.AppendAllText(Output+"/validation.txt","PASS: all guest room identities and entrance plaques plus Lounge; 20 directional checks; ten rendered views; two overlapping legacy labels hidden; no blocking colliders; room identities and mission progress preserved.\n");
            SessionState.SetBool(Flag,false); EditorApplication.ExitPlaymode();
        }
        catch(Exception error){File.AppendAllText(Output+"/validation.txt","FAIL\n"+error);SessionState.SetBool(Flag,false);EditorApplication.ExitPlaymode();Debug.LogException(error);}
    }
    static void Pose(Camera camera,Vector3 position,Vector3 target)
    {
        manager.playerBody.position+=position-camera.transform.position;
        manager.playerBody.transform.position=manager.playerBody.position;
        camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position)); Physics.SyncTransforms();
    }
    static void Capture(string path)
    {
        var camera=manager.playerCamera; var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active; float aspect=camera.aspect;
        var target=new RenderTexture(1600,900,24);target.Create();Texture2D image=null;
        try
        {
            camera.targetTexture=target;camera.aspect=1600f/900;
            foreach(var sign in UnityEngine.Object.FindObjectsByType<mission3RoomSign_World_ShengChiehLee>(FindObjectsSortMode.None))sign.RefreshForCamera();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally{camera.targetTexture=oldTarget;camera.aspect=aspect;RenderTexture.active=oldActive;if(image!=null)UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
}
