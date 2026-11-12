using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class mission3MapStyle_Editor_ShengChiehLee
{
    const string Demo="Assets/Mission3_ShengChiehLee/Scenes/Mission3_Preview.unity";
    const string Output="C:/Users/j1a2m/Documents/Codex/2026-10-25/w/outputs/mission3-map-landmarks";
    static int frames;
    static mission3MapStyle_Editor_ShengChiehLee(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(File.Exists("Library/Mission3FitMap.txt")){File.Delete("Library/Mission3FitMap.txt");SessionState.SetBool("Mission3.MapFit.Apply",true);}
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(SessionState.GetBool("Mission3.MapFit.Apply",false))
        {
            if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            SessionState.SetBool("Mission3.MapFit.Apply",false);
            try
            {
                Apply();EditorSceneManager.OpenScene(Demo);frames=0;SessionState.SetBool("Mission3.MapFit.Preview",true);
                var game=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");if(game!=null){var window=EditorWindow.GetWindow(game,false,"Game",true);window.Show();window.Focus();}
                EditorApplication.isPaused=false;EditorApplication.EnterPlaymode();
            }
            catch(Exception error){Report("MAP_LANDMARKS_FAIL\n"+error);Debug.LogException(error);}
            return;
        }
        if(!SessionState.GetBool("Mission3.MapFit.Preview",false)||!EditorApplication.isPlaying||++frames<50)return;
        SessionState.SetBool("Mission3.MapFit.Preview",false);
        try
        {
            var manager=Find(SceneManager.GetActiveScene());var ui=manager.canvasUI;Application.runInBackground=true;
            manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Map);ui.Refresh();Validate(manager);
            Capture(manager,true,"floor-plan.png");Capture(manager,false,"wall-map.png");
            Report("MAP_LANDMARKS_PASS\nCanonical and demo saved. Elevator icon is projected from the actual cabin passenger space. Lobby now has a larger gold-rimmed coral map pin with a reception bell pointing to the actual return desk. Matching legend symbols replace the old WALLS line and red 0. Both public map symbols, legend labels, six items, three notes, player/landmarks and 12-pixel wall stroke checked. Checkout/lift references preserved. Fresh demo restarted after capture.");
            manager.Restart();
        }
        catch(Exception error){Report("MAP_LANDMARKS_FAIL\n"+error);Debug.LogException(error);}
    }
    static mission3Hotel_Manager_ShengChiehLee Find(Scene scene)=>scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).Single();
    [MenuItem("Mission3/Fit Floor Map and Thicken Walls")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play mode before updating the map artwork.");
        foreach(string path in new[]{mission3Paths_Editor_ShengChiehLee.Scene,Demo})
        {
            var scene=SceneManager.GetSceneByPath(path);if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);var manager=Find(scene);
            mission3Presentation_Editor_ShengChiehLee.MakeMap(manager);
            manager.canvasUI.mapRect.GetComponent<RawImage>().texture=AssetDatabase.LoadAssetAtPath<Texture2D>(mission3Paths_Editor_ShengChiehLee.Maps+"/mission3FloorPlan.png");
            manager.canvasUI.Refresh();Validate(manager);EditorUtility.SetDirty(manager);EditorUtility.SetDirty(manager.canvasUI);
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save "+path);
        }
        AssetDatabase.SaveAssets();
    }
    static Vector2 Pixel(mission3Canvas_UI_ShengChiehLee ui,Vector3 world)=>ui.mapPixelMin+Vector2.Scale(new Vector2(Mathf.InverseLerp(ui.mapWorldMin.x,ui.mapWorldMax.x,world.x),Mathf.InverseLerp(ui.mapWorldMin.y,ui.mapWorldMax.y,world.z)),ui.mapPixelMax-ui.mapPixelMin);
    static void Validate(mission3Hotel_Manager_ShengChiehLee manager)
    {
        var ui=manager.canvasUI;ui.Refresh();
        if(ui.mapPixelMin!=new Vector2(24,24)||ui.mapPixelMax!=new Vector2(1000,1000))throw new Exception("Map padding does not match the new artwork.");
        foreach(var pair in ui.itemMarkers.Select((marker,i)=>(marker.rectTransform,ui.itemPositions[i])).Concat(ui.noteMarkers.Select((marker,i)=>(marker.rectTransform,ui.notePositions[i]))))
            if(Vector2.Distance(pair.Item1.anchorMin*1024,Pixel(ui,pair.Item2.position))>1)throw new Exception("World marker no longer matches the map: "+pair.Item2.name);
        if(Vector2.Distance(ui.playerMarker.anchorMin*1024,Pixel(ui,manager.playerBody.position))>1)throw new Exception("Player marker projection is incorrect.");
        var architecture=manager.gameObject.scene.GetRootGameObjects().Single(x=>x.name=="Mission3_HotelArchitecture").transform;
        var names=new[]{"Reception marker","Lift marker","Lounge marker"};var positions=new[]{new Vector3(6,0,-6),new Vector3(15,0,-2.75f),new Vector3(36,0,-46.5f)};
        for(int i=0;i<names.Length;i++)if(Vector2.Distance(((RectTransform)ui.mapRect.Find(names[i])).anchorMin*1024,Pixel(ui,architecture.TransformPoint(positions[i])))>1)throw new Exception("Public landmark projection is incorrect: "+names[i]);
        var texture=new Texture2D(2,2);
        try
        {
            texture.LoadImage(File.ReadAllBytes(mission3Paths_Editor_ShengChiehLee.Maps+"/mission3FloorMap.png"));
            foreach(var t in ui.itemPositions)
            {
                var p=Pixel(ui,t.position);var color=texture.GetPixel(Mathf.RoundToInt(p.x)+12,Mathf.RoundToInt(p.y));
                if(color.r>.2f||color.g<.5f||color.b<.5f)throw new Exception("Baked item marker is not aligned: "+t.name);
            }
            foreach(var t in ui.notePositions)
            {
                var p=Pixel(ui,t.position);var color=texture.GetPixel(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y));
                if(color.r<.5f||color.b>.5f)throw new Exception("Baked note marker is not aligned: "+t.name);
            }
            var key=texture.GetPixel(316,984);
            if(key.r>.2f||key.g<.5f||key.b<.5f)throw new Exception("Wall-map item key is missing.");
            key=texture.GetPixel(319,945);
            if(key.r<.5f||key.b>.5f)throw new Exception("Wall-map note key is missing.");
            foreach(var p in new[]{new Vector2Int(350,974),new Vector2Int(353,961),new Vector2Int(350,912),new Vector2Int(690,974),new Vector2Int(693,961),new Vector2Int(690,912)})
                if(texture.GetPixel(p.x,p.y).r>.2f)throw new Exception("Wall-map legend text is missing at "+p);
            void CheckLiftPixel(int x,int y)
            {
                var color=texture.GetPixel(x,y);
                if(color.r<.25f||color.r>.45f||color.g<.25f||color.g>.45f||color.b<.55f)throw new Exception("Elevator map symbol is missing at "+x+","+y);
            }
            var cabin=Pixel(ui,manager.departureLift.transform.TransformPoint(manager.departureLift.settlementCenter));
            CheckLiftPixel(Mathf.RoundToInt(cabin.x)-15,Mathf.RoundToInt(cabin.y));CheckLiftPixel(320,922);
            var desk=Pixel(ui,manager.returnDesk.transform.position);
            var lobby=texture.GetPixel(Mathf.RoundToInt(desk.x)+18,Mathf.RoundToInt(desk.y)+34);
            if(lobby.r<.7f||lobby.g>.4f)throw new Exception("Lobby bell pin is missing at the return desk.");
            lobby=texture.GetPixel(646,987);
            if(lobby.r<.7f||lobby.g>.4f)throw new Exception("Lobby legend does not match the map pin.");
            texture.LoadImage(File.ReadAllBytes(mission3Paths_Editor_ShengChiehLee.Maps+"/mission3FloorPlan.png"));
            var wall=Pixel(ui,architecture.TransformPoint(new Vector3(9,0,-1.5f)));int count=0;
            for(int y=Mathf.RoundToInt(wall.y)-10;y<=Mathf.RoundToInt(wall.y)+10;y++)if(texture.GetPixel(Mathf.RoundToInt(wall.x),y).r<.2f)count++;
            if(count!=12)throw new Exception("Expected a 12-pixel wall stroke, got "+count);
        }
        finally{UnityEngine.Object.DestroyImmediate(texture);}
        if(manager.returnDesk==null||manager.departureLift==null||ui.resultsCanvas==null)throw new Exception("Map update lost a gameplay reference.");
    }
    static void Capture(mission3Hotel_Manager_ShengChiehLee manager,bool showUI,string name)
    {
        Directory.CreateDirectory(Output);var camera=manager.playerCamera;var ui=manager.canvasUI;var canvas=ui.GetComponent<Canvas>();
        var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
        var data=camera.GetComponent<UniversalAdditionalCameraData>();bool oldPost=data.renderPostProcessing;
        var renderers=manager.playerBody.GetComponentsInChildren<Renderer>(true);var enabled=renderers.Select(x=>x.enabled).ToArray();
        var target=new RenderTexture(1600,1000,24);target.Create();Texture2D image=null;
        try
        {
            foreach(var renderer in renderers)renderer.enabled=false;
            if(showUI){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;data.renderPostProcessing=false;}
            else
            {
                var map=manager.gameObject.scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Single(x=>x.name=="Entrance wall floor map");
                camera.transform.position=new Vector3(map.position.x-.8f,2.55f,map.position.z-4.7f);camera.transform.LookAt(new Vector3(map.position.x-.4f,2.4f,map.position.z));
            }
            camera.targetTexture=target;Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name,image.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;camera.targetTexture=oldTarget;camera.transform.position=oldPosition;camera.transform.rotation=oldRotation;data.renderPostProcessing=oldPost;RenderTexture.active=oldActive;
            for(int i=0;i<renderers.Length;i++)renderers[i].enabled=enabled[i];if(image!=null)UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
    }
    static void Report(string message){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/validation.txt",message);mission3Session_Editor_ShengChiehLee.Write(message);}
}
