using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class mission3EntranceMap_Editor_ShengChiehLee
{
    const string Flag="Mission3.EntranceMap.Move";
    const string Demo="Assets/Scenes/Demos/Mission3_Demo.unity";
    const string Output="C:/Users/j1a2m/Documents/Codex/2026-10-25/w/outputs/mission3-map-position";
    static int previewFrames;
    static mission3EntranceMap_Editor_ShengChiehLee(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(File.Exists("Library/Mission3MoveEntranceMap.txt")){File.Delete("Library/Mission3MoveEntranceMap.txt");SessionState.SetBool(Flag,true);}
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(SessionState.GetBool(Flag,false))
        {
            if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            SessionState.SetBool(Flag,false);
            try
            {
                Apply();EditorSceneManager.OpenScene(Demo);previewFrames=0;SessionState.SetBool("Mission3.EntranceMap.Preview",true);
                var view=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");if(view!=null)EditorWindow.GetWindow(view,false,"Game",true).Show();
                EditorApplication.EnterPlaymode();
            }
            catch(Exception error){Report("MAP_POSITION_FAIL\n"+error);Debug.LogException(error);}
            return;
        }
        if(!SessionState.GetBool("Mission3.EntranceMap.Preview",false)||!EditorApplication.isPlaying||++previewFrames<50)return;
        SessionState.SetBool("Mission3.EntranceMap.Preview",false);
        try
        {
            var scene=SceneManager.GetActiveScene();var manager=Manager(scene);Validate(scene);
            var map=MapGroup(scene).GetComponentsInChildren<mission3Interact_Prop_ShengChiehLee>(true).Single(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.FloorMap);
            manager.Interact(map);
            if(manager.CurrentPage!=mission3Hotel_Manager_ShengChiehLee.Page.Map||!manager.canvasUI.pages[2].activeInHierarchy)throw new Exception("Entrance map no longer opens the floor-plan panel.");
            Capture(manager,map.transform);
            manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Welcome);
            Report("MAP_POSITION_PASS\nBoth scenes saved. Entrance map center raised from 1.85 m to 2.35 m; title and legend aligned; four parts grouped together. Camera-height sightline and standing clearance verified; map interaction opens the existing M panel. Fresh demo ready.\nPreview: "+Output+"/entrance-map.png");
        }
        catch(Exception error){Report("MAP_POSITION_FAIL\n"+error);Debug.LogException(error);}
    }
    [MenuItem("Mission3/Align Entrance Wall Map")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play mode before moving the entrance map.");
        foreach(string path in new[]{mission3Paths_Editor_ShengChiehLee.Scene,Demo})
        {
            var scene=SceneManager.GetSceneByPath(path);if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);Align(scene);Validate(scene);
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save "+path);
        }
    }
    public static void Align(Scene scene)
    {
        var props=scene.GetRootGameObjects().Single(x=>x.name=="Mission3_PropsAndInteractions").transform;
        var group=props.Find("EntranceMapDisplay");if(group==null){group=new GameObject("EntranceMapDisplay").transform;group.SetParent(props,false);}
        var all=props.GetComponentsInChildren<Transform>(true);
        var names=new[]{"Map frame","Entrance wall floor map","Map header","Map legend"};
        var positions=new[]{new Vector3(9,2.35f,-1.64f),new Vector3(9,2.35f,-1.73f),new Vector3(9,4.15f,-1.82f),new Vector3(5.5f,2.35f,-1.82f)};
        group.localPosition=Vector3.zero;group.localRotation=Quaternion.identity;group.localScale=Vector3.one;
        for(int i=0;i<names.Length;i++)
        {
            var item=all.Single(x=>x.name==names[i]);item.SetParent(group,true);item.localPosition=positions[i];
            EditorUtility.SetDirty(item);
        }
        var legend=group.Find("Map legend").GetComponent<TextMeshPro>();legend.rectTransform.sizeDelta=new Vector2(3.2f,1.6f);
        EditorSceneManager.MarkSceneDirty(scene);Physics.SyncTransforms();
    }
    static mission3Hotel_Manager_ShengChiehLee Manager(Scene scene)=>scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).Single();
    static Transform MapGroup(Scene scene)=>scene.GetRootGameObjects().Single(x=>x.name=="Mission3_PropsAndInteractions").transform.Find("EntranceMapDisplay");
    static void Validate(Scene scene)
    {
        var manager=Manager(scene);var group=MapGroup(scene);var map=group.Find("Entrance wall floor map");var frame=group.Find("Map frame");
        if(group.childCount!=4||Mathf.Abs(map.position.y-2.35f)>.01f||Mathf.Abs(frame.position.y-map.position.y)>.001f)throw new Exception("Entrance map parts are not aligned.");
        var board=map.GetComponent<Renderer>();if(board.bounds.max.y>4.3f||board.bounds.min.y<.75f)throw new Exception("Entrance map extends outside its wall space.");
        var capsule=manager.playerBody.GetComponent<CapsuleCollider>();var scale=capsule.transform.lossyScale;
        float radius=capsule.radius*Mathf.Max(scale.x,scale.z),height=capsule.height*scale.y;
        var eye=map.position+Vector3.back*2.7f;eye.y=height/2+.03f+manager.playerCamera.transform.position.y-manager.playerBody.position.y;
        var direction=map.position-eye;
        var hits=Physics.RaycastAll(eye,direction.normalized,3.4f,~0,QueryTriggerInteraction.Collide).Where(x=>x.collider.gameObject.scene==scene&&!x.transform.IsChildOf(manager.playerBody.transform)).OrderBy(x=>x.distance).ToArray();
        if(hits.Length==0||hits[0].collider.GetComponent<mission3Interact_Prop_ShengChiehLee>()?.kind!=mission3Interact_Prop_ShengChiehLee.Kind.FloorMap)throw new Exception("Entrance map inspection sightline is blocked.");
        var low=new Vector3(eye.x,radius+.03f,eye.z);var high=new Vector3(eye.x,height-radius+.03f,eye.z);
        if(Physics.OverlapCapsule(low,high,radius,~0,QueryTriggerInteraction.Ignore).Any(x=>x.gameObject.scene==scene&&!x.transform.IsChildOf(manager.playerBody.transform)))throw new Exception("Entrance map standing area is obstructed.");
    }
    static void Capture(mission3Hotel_Manager_ShengChiehLee manager,Transform map)
    {
        Directory.CreateDirectory(Output);var camera=manager.playerCamera;var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
        var target=new RenderTexture(1600,1000,24);target.Create();Texture2D image=null;
        try
        {
            camera.transform.position=new Vector3(map.position.x-1.15f,2.55f,map.position.z-5.4f);camera.transform.LookAt(new Vector3(map.position.x-.8f,2.45f,map.position.z));camera.targetTexture=target;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;
            image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Output+"/entrance-map.png",image.EncodeToPNG());
        }
        finally{camera.transform.position=oldPosition;camera.transform.rotation=oldRotation;camera.targetTexture=oldTarget;RenderTexture.active=oldActive;if(image!=null)UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
    static void Report(string message){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/validation.txt",message);mission3Session_Editor_ShengChiehLee.Write(message);}
}
