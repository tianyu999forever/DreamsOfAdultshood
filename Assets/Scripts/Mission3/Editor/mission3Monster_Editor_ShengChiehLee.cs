using System;
using System.IO;
using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class mission3Monster_Editor_ShengChiehLee
{
    public const string Demo="Assets/Mission3_ShengChiehLee/Scenes/Mission3_Preview.unity";
    public const string Output="C:/Users/j1a2m/Documents/Codex/2026-10-25/w/outputs/mission3-night-patrol";
    const string Prefab="Assets/Prefabs/Mission3/Characters/Mission3NightAttendant.prefab";
    static bool editReady;
    static mission3Monster_Editor_ShengChiehLee()
    {
        editReady=!EditorApplication.isPlayingOrWillChangePlaymode&&!Application.isPlaying;
        EditorApplication.playModeStateChanged+=state=>editReady=state==PlayModeStateChange.EnteredEditMode;
        EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        if(File.Exists("Library/Mission3NightPatrolApply.txt")){File.Delete("Library/Mission3NightPatrolApply.txt");SessionState.SetBool("Mission3.NightPatrol.Apply",true);}
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(SessionState.GetBool("Mission3.NightPatrol.Apply",false))
        {
            if(!editReady){if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
            SessionState.SetBool("Mission3.NightPatrol.Apply",false);
            try{ApplyAndTest();}catch(Exception e){Report("NIGHT_PATROL_FAIL\n"+e);Debug.LogException(e);}return;
        }
        if(SessionState.GetBool("Mission3.NightPatrol.PlayFresh",false)&&editReady)
        {
            SessionState.SetBool("Mission3.NightPatrol.PlayFresh",false);mission3DemoPreview_Editor_ShengChiehLee.Play();
        }
    }
    [MenuItem("Mission3/Apply and Test Night Patrol Prototype")]
    public static void ApplyAndTest()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play mode before installing the prototype.");
        foreach(string path in new[]{mission3Paths_Editor_ShengChiehLee.Scene,Demo})
        {
            var scene=EditorSceneManager.OpenScene(path);string backup="Library/"+Path.GetFileNameWithoutExtension(path)+"-BeforeNightPatrol.unity";if(!File.Exists(backup))File.Copy(path,backup);
            Install(scene);Validate(scene);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save "+path);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Demo);Test();
    }
    public static void Install(Scene scene)
    {
        var manager=Find(scene);var architecture=scene.GetRootGameObjects().Single(x=>x.name=="Mission3_HotelArchitecture").transform;
        var old=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="Mission3_NightPatrol");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
        foreach(string name in new[]{"Night patrol status","Night patrol recovery"})
        {var t=manager.canvasUI.transform.Find(name);if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);}
        EnsureFolder("Assets/Navigation/Mission3");EnsureFolder("Assets/Prefabs/Mission3/Characters");
        CreatePrefab();
        var root=new GameObject("Mission3_NightPatrol");SceneManager.MoveGameObjectToScene(root,scene);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);instance.transform.SetParent(root.transform,false);
        var monster=instance.GetComponent<mission3Monster_Manager_ShengChiehLee>();monster.manager=manager;manager.nightPatrol=monster;
        Vector3 World(float x,float z)=>architecture.TransformPoint(new Vector3(x*3,0,-z*3));
        Transform[] Points(string name,Vector2Int[] grid)
        {
            var group=new GameObject(name).transform;group.SetParent(root.transform,false);
            return grid.Select((p,i)=>{var point=new GameObject((i+1).ToString("00")+" / "+p).transform;point.SetParent(group,false);point.position=World(p.x,p.y);return point;}).ToArray();
        }
        monster.earlyRoute=Points("01_WestPatrol",new[]{new Vector2Int(3,7),new Vector2Int(9,7),new Vector2Int(9,13),new Vector2Int(3,13)});
        monster.expandedRoute=Points("02_AllWingsPatrol",new[]{new Vector2Int(3,7),new Vector2Int(18,7),new Vector2Int(18,18),new Vector2Int(14,18),new Vector2Int(14,12),new Vector2Int(9,12),new Vector2Int(9,18),new Vector2Int(3,18),new Vector2Int(3,13)});
        monster.spawnPoints=Points("03_OffscreenSpawnPoints",new[]{new Vector2Int(3,18),new Vector2Int(18,7),new Vector2Int(18,18),new Vector2Int(9,13)});
        instance.transform.position=monster.spawnPoints[0].position;
        var safe=new GameObject("04_ReceptionAndLiftSafeArea").AddComponent<NavMeshModifierVolume>();safe.transform.SetParent(root.transform,false);
        safe.transform.position=architecture.TransformPoint(new Vector3(9,2,-7.5f));safe.size=new Vector3(18.8f*architecture.lossyScale.x,6,18.6f*architecture.lossyScale.z);safe.area=1;
        monster.lobbySafeBounds=new Bounds(safe.transform.position,safe.size);
        var ignored=manager.playerBody.GetComponent<NavMeshModifier>();if(ignored==null)ignored=manager.playerBody.gameObject.AddComponent<NavMeshModifier>();ignored.ignoreFromBuild=true;
        var ui=manager.canvasUI;
        var card=Panel(ui.transform,"Night patrol status",new Vector2(600,205),new Vector2(560,58),new Color(.035f,.07f,.09f,.94f));
        monster.statusCard=card.gameObject;monster.status=Text(card.transform,"Threat status","NIGHT PATROL / QUIET FOR NOW",new Vector2(530,48),19);
        card.transform.SetSiblingIndex(ui.backdrop.transform.GetSiblingIndex());
        var shade=Panel(ui.transform,"Night patrol recovery",Vector2.zero,new Vector2(1920,1080),new Color(.012f,.018f,.025f,1));
        var rect=shade.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
        monster.recoveryShade=shade.gameObject.AddComponent<CanvasGroup>();monster.recoveryShade.alpha=0;monster.recoveryShade.blocksRaycasts=true;
        Text(shade.transform,"Recovery message","RETURNED TO RECEPTION\n\nYour belongings and guest notes are safe.",new Vector2(1200,280),34);
        shade.gameObject.SetActive(false);
        monster.warningLights=architecture.GetComponentsInChildren<Light>(true).Where(x=>x.type==LightType.Point).OrderBy(x=>Vector3.Distance(x.transform.position,World(3,7))).Take(4).ToArray();
        var welcome=ui.pages[1].transform;
        welcome.Find("Brief").GetComponent<TMP_Text>().text="A note beside your keys reads: \"Find lost items. Use guest notes to identify their owners. Return them at Lost & Found, then take the lift. After two finds, stay quiet and turn corners to avoid the night patrol.\"";
        welcome.Find("Your first steps").GetComponent<TMP_Text>().text="MAP > NOTE > BELONGING\nAFTER 2 FINDS: THE NIGHT PATROL WAKES\nRECEPTION + LIFT ARE SAFE. RUN TO ESCAPE.";
        var surface=root.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.All;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize=true;surface.voxelSize=.12f;surface.BuildNavMesh();
        if(surface.navMeshData==null)throw new Exception("Could not bake the night patrol navigation.");
        string navPath="Assets/Navigation/Mission3/"+Path.GetFileNameWithoutExtension(scene.path)+"_NightPatrol.asset";
        var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
        if(existing==null)AssetDatabase.CreateAsset(surface.navMeshData,navPath);
        else
        {
            var data=surface.navMeshData;surface.RemoveData();EditorUtility.CopySerialized(data,existing);surface.navMeshData=existing;surface.AddData();UnityEngine.Object.DestroyImmediate(data);
        }
        EditorUtility.SetDirty(manager);EditorUtility.SetDirty(monster);EditorUtility.SetDirty(surface);EditorSceneManager.MarkSceneDirty(scene);
    }
    static void CreatePrefab()
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab)!=null)return;
        var root=new GameObject("Night attendant / Mission2 silhouette");
        try
        {
            var monster=root.AddComponent<mission3Monster_Manager_ShengChiehLee>();monster.agent=root.AddComponent<NavMeshAgent>();
            monster.agent.enabled=false;monster.agent.radius=.38f;monster.agent.height=3.6f;monster.agent.acceleration=15;monster.agent.angularSpeed=300;monster.agent.stoppingDistance=.25f;
            monster.agent.avoidancePriority=35;
            var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="Body / same capsule mesh as Mission2";body.transform.SetParent(root.transform,false);
            body.transform.localPosition=new Vector3(0,1.8f,0);body.transform.localScale=new Vector3(.9f,1.8f,.9f);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            var original=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("31321ba15b8f8eb4c954353edc038b1d"));
            if(original!=null)body.GetComponent<Renderer>().sharedMaterial=original;
            monster.body=body;body.SetActive(false);
            root.AddComponent<NavMeshModifier>().ignoreFromBuild=true;
            monster.footsteps=root.AddComponent<AudioSource>();monster.footsteps.playOnAwake=false;monster.footsteps.spatialBlend=1;monster.footsteps.minDistance=2;monster.footsteps.maxDistance=24;monster.footsteps.rolloffMode=AudioRolloffMode.Linear;monster.footsteps.pitch=.65f;
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;var parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    static Image Panel(Transform parent,string name,Vector2 position,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.sizeDelta=size;rect.anchoredPosition=position;
        var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=name=="Night patrol recovery";return image;
    }
    static TMP_Text Text(Transform parent,string name,string value,Vector2 size,int fontSize)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TextMeshProUGUI>();
        text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");text.text=value;text.fontSize=fontSize;text.color=new Color(.97f,.81f,.48f);text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.rectTransform.sizeDelta=size;return text;
    }
    public static mission3Hotel_Manager_ShengChiehLee Find(Scene scene)=>scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).Single();
    public static void Validate(Scene scene)
    {
        var manager=Find(scene);var monster=manager.nightPatrol;
        if(monster==null||monster.manager!=manager||monster.earlyRoute.Length!=4||monster.expandedRoute.Length!=9||monster.spawnPoints.Length!=4)throw new Exception("Night patrol references are incomplete.");
        if(!monster.lobbySafeBounds.Contains(manager.returnDesk.transform.position)||!monster.lobbySafeBounds.Contains(manager.departureLift.transform.position+Vector3.up))throw new Exception("Return desk or lift is outside the safe area.");
        var points=monster.earlyRoute.Concat(monster.expandedRoute).Concat(monster.spawnPoints).ToArray();
        foreach(var point in points)if(!NavMesh.SamplePosition(point.position,out var hit,1.5f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y)>.25f)throw new Exception("No floor navigation at "+point.name);
        foreach(var from in monster.earlyRoute)foreach(var to in monster.expandedRoute)
        {
            NavMesh.SamplePosition(from.position,out var start,1.5f,NavMesh.AllAreas);NavMesh.SamplePosition(to.position,out var end,1.5f,NavMesh.AllAreas);var path=new NavMeshPath();
            if(!NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Disconnected patrol route: "+from.name+" -> "+to.name);
        }
        if(NavMesh.SamplePosition(manager.returnDesk.transform.position,out var ignored,1,NavMesh.AllAreas))throw new Exception("Attendant can enter the lobby safe area.");
        mission3Space_Editor_ShengChiehLee.Validate(scene);mission3CheckoutFlow_Editor_ShengChiehLee.Validate(scene);
    }
    [MenuItem("Mission3/Test Night Patrol Demo")]
    public static void Test()
    {
        EditorSceneManager.OpenScene(Demo);var game=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if(game!=null){var window=EditorWindow.GetWindow(game,false,"Game",true);window.Show();window.Focus();}
        EditorApplication.isPaused=false;mission3MonsterTest_Editor_ShengChiehLee.Begin();
    }
    public static void Report(string value){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/validation.txt",value);mission3Session_Editor_ShengChiehLee.Write(value);}
    public static void FreshDemo(){SessionState.SetBool("Mission3.NightPatrol.PlayFresh",true);}
}
