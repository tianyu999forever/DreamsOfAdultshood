using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class mission3Demo_Editor_ShengChiehLee
{
    const string Demo="Assets/Scenes/Demos/Mission3_Demo.unity";
    const string Output="C:/Users/j1a2m/Documents/Codex/2026-10-25/w/outputs/mission3-demo";
    static TMP_FontAsset font;
    static readonly Color Ink=new Color(.055f,.10f,.12f),Paper=new Color(.95f,.92f,.84f),Gold=new Color(.79f,.60f,.29f);
    static Material wood,wall,brass,carpet,ceiling,traveller,musician,botanist;
    static Transform architecture,decor;
    static Vector3 W(int x,int y,float height)=>new Vector3(x*3,height,-y*3);

    [MenuItem("Mission3/Build Guided Windows Demo")]
    public static void ApplyAndBuild()
    {
        try
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play mode before building the demo.");
            var scene=EditorSceneManager.OpenScene(mission3Paths_Editor_ShengChiehLee.Scene);
            SceneManager.SetActiveScene(scene);Upgrade(scene);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            mission3Presentation_Editor_ShengChiehLee.Validate();
            if(!AssetDatabase.IsValidFolder("Assets/Scenes/Demos"))AssetDatabase.CreateFolder("Assets/Scenes","Demos");
            if(!EditorSceneManager.SaveScene(scene,Demo,true))throw new IOException("Could not create demo scene.");
            scene=EditorSceneManager.OpenScene(Demo);var manager=Find<mission3Hotel_Manager_ShengChiehLee>(scene);manager.demoMode=true;manager.nextSceneName="";
            var check=new GameObject("Demo automated check (command-line only)").AddComponent<mission3DemoCheck_Manager_ShengChiehLee>();
            ButtonLabel(manager.canvasUI.pages[7].transform.Find("ContinueButton").GetComponent<Button>(),"EXIT DEMO");
            var pause=manager.canvasUI.pages[8].transform;
            var quit=MakeButton(pause,"ExitDemoButton","EXIT DEMO",new Vector2(525,-345),manager.QuitDemo);
            quit.GetComponent<RectTransform>().sizeDelta=new Vector2(460,54);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory(Output);
            var oldSize=new Vector2Int(PlayerSettings.defaultScreenWidth,PlayerSettings.defaultScreenHeight);var oldMode=PlayerSettings.fullScreenMode;bool oldBackground=PlayerSettings.runInBackground;
            try
            {
                PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Demo},locationPathName=Output+"/Mission3Demo.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows demo build failed: "+report.summary.result);
                File.WriteAllText(Output+"/build-info.txt","BUILD_PASS\nGuided Mission3 demo\n"+DateTime.Now.ToString("s")+"\nSize: "+report.summary.totalSize+" bytes\nOnly the demo scene is included.");
            }
            finally{PlayerSettings.defaultScreenWidth=oldSize.x;PlayerSettings.defaultScreenHeight=oldSize.y;PlayerSettings.fullScreenMode=oldMode;PlayerSettings.runInBackground=oldBackground;AssetDatabase.SaveAssets();}
            File.WriteAllText(Output+"/HOW-TO-PLAY.txt","MISSION 3 / HOTEL 24\n\nRun Mission3Demo.exe from this folder. Keep its _Data folder and UnityPlayer.dll beside it.\n\nWASD: move / Mouse: look / Shift: run / E: interact\nM: map / TAB: guest journal / Q: watch / ESC: pause\n\nFollow the objective card. Read the guest notes, collect at least three belongings, then return them at reception or inside the lift. The map shows all six belongings, guest notes and public landmarks. Item location does not reveal ownership. No countdown.\n\nUse Explore Again after checkout to restart. ESC > Exit Demo closes the game.\n");
            Debug.Log("MISSION3_GUIDED_DEMO_BUILD_PASS");
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
    public static void UpdateScenes()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play mode before updating the hotel.");
        foreach(string path in new[]{mission3Paths_Editor_ShengChiehLee.Scene,Demo})
        {
            var scene=SceneManager.GetSceneByPath(path);
            if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            File.Copy(path,"Library/"+Path.GetFileNameWithoutExtension(path)+"-BeforeSpace.unity",true);
            Upgrade(scene);mission3Space_Editor_ShengChiehLee.Validate(scene);
            mission3Presentation_Editor_ShengChiehLee.Validate();
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save "+path);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("Library/Mission3SpaceValidation.txt","SPACE_EDIT_PASS\nClear ceiling: 4.8 m; plan width/depth: +15%; six side displays; six unobstructed capsule approaches and E-interaction sightlines; both canonical and demo scenes saved.\n");
    }
    static T Find<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<T>(true)).FirstOrDefault();
    public static void Upgrade(Scene scene)
    {
        mission3Space_Editor_ShengChiehLee.Prepare(scene);
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        architecture=scene.GetRootGameObjects().Single(x=>x.name=="Mission3_HotelArchitecture").transform;
        wood=Mat("mission3_Walnut");wall=Mat("mission3_Wallpaper");brass=Mat("mission3_Brass");carpet=Mat("mission3_BurgundyCarpet");
        ceiling=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Materials/Environment/M_Ceiling.mat");
        traveller=ColorMaterial("mission3_TravellerAccent",new Color(.12f,.31f,.40f));musician=ColorMaterial("mission3_MusicianAccent",new Color(.40f,.10f,.16f));botanist=ColorMaterial("mission3_BotanistAccent",new Color(.16f,.32f,.20f));
        var old=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="Mission3_DemoLandmarks");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
        decor=new GameObject("Mission3_DemoLandmarks").transform;
        AddLoungeLoop();DecorateRooms();
        var m=Find<mission3Hotel_Manager_ShengChiehLee>(scene);var ui=m.canvasUI;
        var guide=m.GetComponent<mission3Guide_UI_ShengChiehLee>();if(guide==null)guide=m.gameObject.AddComponent<mission3Guide_UI_ShengChiehLee>();m.guide=guide;guide.manager=m;
        var canvas=ui.transform;
        var card=Panel(canvas,"Current objective",new Vector2(600,307),new Vector2(560,122),new Color(.035f,.07f,.09f,.94f));
        guide.objectiveCard=card.gameObject;guide.objective=Text(card.transform,"Next step","",Vector2.zero,new Vector2(520,96),22,Paper);
        card.transform.SetSiblingIndex(ui.backdrop.transform.GetSiblingIndex());
        guide.controls=canvas.Find("WatchHintText").GetComponent<TMP_Text>();
        ui.checkoutRequirement=Text(ui.pages[5].transform,"Checkout requirements","",new Vector2(0,-298),new Vector2(1490,42),18,Gold);
        var welcome=ui.pages[1].transform;
        welcome.Find("Brief").GetComponent<TMP_Text>().text="Welcome to the night shift. Six belongings were left behind. Read the guests' notes, work out who owns each item, then return to reception.";
        Text(welcome,"Your first steps","LOOK AT THE MAP  >  READ A NOTE  >  FIND A BELONGING\nYour objective card will guide you one step at a time.",new Vector2(-330,-80),new Vector2(840,110),23,Gold);
        welcome.Find("Score guide").GetComponent<TMP_Text>().text="YOUR SHIFT\n\n06  LOST BELONGINGS\n03  GUEST NOTES\n\nReturn at least 3 to finish.\nFind all 6 for more points.\nNo countdown.";
        ButtonLabel(welcome.Find("BeginButton").GetComponent<Button>(),"START GUIDED EXPLORATION");
        var markerRoot=ui.mapRect;
        MapMarker(markerRoot,"Reception marker","R",new Vector3(6,0,-6),Gold);
        MapMarker(markerRoot,"Lift marker","L",new Vector3(15,0,-2.75f),new Color(.43f,.47f,.75f));
        MapMarker(markerRoot,"Lounge marker","S",new Vector3(36,0,-46.5f),new Color(.31f,.54f,.39f));
        ui.pages[2].transform.Find("Legend key").GetComponent<TMP_Text>().text="RED ARROW: You    /    CYAN: Belonging\nAMBER N: Note    /    MUTED +: Collected\nR: RECEPTION    L: LIFT    S: LOUNGE\n\n301  Traveller / west wing\n302  Musician / north wing\n303  Botanist / east wing\nAll items are shown. North is up.";
        Text(ui.pages[2].transform,"Navigation hint","R / L  Return belongings here",new Vector2(360,203),new Vector2(660,30),19,Gold);
        MakeButton(ui.pages[5].transform,"JournalButton","VIEW GUEST NOTES / TAB",new Vector2(-40,-358),ui.Journal).GetComponent<RectTransform>().sizeDelta=new Vector2(440,54);
        mission3Space_Editor_ShengChiehLee.Apply(scene);
        mission3Presentation_Editor_ShengChiehLee.MakeMap(m);
        ui.mapRect.GetComponent<RawImage>().texture=AssetDatabase.LoadAssetAtPath<Texture2D>(mission3Paths_Editor_ShengChiehLee.Maps+"/mission3FloorPlan.png");
        mission3Architecture_Editor_ShengChiehLee.Organize(scene);
        guide.Refresh();ui.Refresh();EditorUtility.SetDirty(m);EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(scene);
    }
    static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(mission3Paths_Editor_ShengChiehLee.Material(name));
    static Material ColorMaterial(string name,Color color)
    {
        string path=mission3Paths_Editor_ShengChiehLee.Environment+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}material.color=color;EditorUtility.SetDirty(material);return material;
    }
    static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool collision=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,true);go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
        if(!collision)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    static void AddLoungeLoop()
    {
        var transforms=architecture.GetComponentsInChildren<Transform>(true);var cells=new HashSet<Vector2Int>();
        foreach(var t in transforms){var parts=t.name.Split('_');if(parts.Length==3&&parts[0]=="Floor"&&int.TryParse(parts[1],out int x)&&int.TryParse(parts[2],out int y))cells.Add(new Vector2Int(x,y));}
        var added=new[]{new Vector2Int(8,15),new Vector2Int(9,15),new Vector2Int(10,15),new Vector2Int(9,14)};
        var all=new HashSet<Vector2Int>(cells.Concat(added));var dirs=new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down};
        foreach(var p in added)
        {
            if(cells.Contains(p))continue;
            Box(architecture,"Floor_"+p.x+"_"+p.y,W(p.x,p.y,-.15f),new Vector3(3,.3f,3),carpet,true);
            Box(architecture,"Ceiling_"+p.x+"_"+p.y,W(p.x,p.y,3.7f),new Vector3(3,.2f,3),ceiling,true);
            foreach(var d in dirs)
            {
                var midpoint=new Vector3((p.x+d.x*.5f)*3,1.8f,-(p.y+d.y*.5f)*3);
                if(all.Contains(p+d))
                {
                    foreach(var old in transforms.Where(t=>t!=null&&(t.name.StartsWith("Wall_")||t.name=="Walnut wainscot"||t.name=="Brass chair rail")&&Mathf.Abs(t.position.x-midpoint.x)<.01f&&Mathf.Abs(t.position.z-midpoint.z)<.01f).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
                    continue;
                }
                var size=d.x!=0?new Vector3(.16f,3.6f,3):new Vector3(3,3.6f,.16f);
                Box(architecture,"Wall_"+p.x+"_"+p.y+"_"+d,midpoint,size,wall,true);
                Box(architecture,"Walnut wainscot",new Vector3(midpoint.x,.48f,midpoint.z),new Vector3(size.x+.025f,.9f,size.z+.025f),wood,true);
                Box(architecture,"Brass chair rail",new Vector3(midpoint.x,.96f,midpoint.z),new Vector3(size.x+.04f,.04f,size.z+.04f),brass);
            }
        }
        var lamp=new GameObject("Lounge connector light").AddComponent<Light>();lamp.transform.SetParent(decor);lamp.transform.position=new Vector3(27,3,-45);lamp.type=LightType.Point;lamp.range=9;lamp.intensity=1.8f;lamp.color=new Color(1,.8f,.57f);
        WorldLabel("Lounge loop sign","LOUNGE LOOP / RECEPTION",new Vector3(27,2.95f,-43.5f),0,1.45f,new Vector2(2.8f,.4f));
    }
    static void DecorateRooms()
    {
        foreach(var t in architecture.GetComponentsInChildren<Transform>(true))
        {
            var parts=t.name.Split('_');if(parts.Length!=3||parts[0]!="Floor"||!int.TryParse(parts[1],out int x)||!int.TryParse(parts[2],out int y))continue;
            if(x<=2&&y>=9&&y<=11)t.GetComponent<Renderer>().sharedMaterial=traveller;
            else if(x>=10&&x<=12&&y>=3&&y<=5)t.GetComponent<Renderer>().sharedMaterial=musician;
            else if(x>=18&&y>=10&&y<=12)t.GetComponent<Renderer>().sharedMaterial=botanist;
        }
        Box(decor,"Traveller suitcase",new Vector3(2.6f,.38f,-29.8f),new Vector3(.75f,.75f,.35f),traveller);
        Box(decor,"Suitcase handle",new Vector3(2.6f,.82f,-29.8f),new Vector3(.3f,.1f,.06f),brass);
        Box(decor,"Seascape frame",new Vector3(1.6f,2.1f,-30.5f),new Vector3(.1f,1.15f,1.7f),brass);
        Box(decor,"Seascape painting",new Vector3(1.67f,2.1f,-30.5f),new Vector3(.03f,.97f,1.52f),traveller);
        WorldLabel("301 identity","301 / THE TRAVELLER",new Vector3(6,2.9f,-33.6f),0,1.6f,new Vector2(4,.45f));
        Box(decor,"Record cabinet",new Vector3(36.8f,.65f,-11.5f),new Vector3(.25f,1.3f,2.8f),wood);
        for(int i=0;i<3;i++)
        {
            var disk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);disk.name="Music room wall record";disk.transform.SetParent(decor);disk.transform.position=new Vector3(36.6f,1.7f,-10.3f-i*1.1f);disk.transform.rotation=Quaternion.Euler(0,0,90);disk.transform.localScale=new Vector3(.72f,.02f,.72f);disk.GetComponent<Renderer>().sharedMaterial=Mat("mission3_Ink");UnityEngine.Object.DestroyImmediate(disk.GetComponent<Collider>());
        }
        WorldLabel("302 identity","302 / THE MUSICIAN",new Vector3(36.55f,2.75f,-11.4f),90,1.7f,new Vector2(3.2f,.5f));
        for(int i=0;i<3;i++)Plant(new Vector3(57.6f,0,-30.5f-i*1.5f));
        Box(decor,"Botanical specimen frame",new Vector3(58.35f,2.35f,-34),new Vector3(.1f,1.2f,1.4f),brass);
        Box(decor,"Botanical specimen backing",new Vector3(58.28f,2.35f,-34),new Vector3(.03f,1.05f,1.22f),botanist);
        WorldLabel("303 identity","303 / THE BOTANIST",new Vector3(54,2.9f,-36.8f),0,1.6f,new Vector2(4,.5f));
        WorldLabel("Reception identity","RECEPTION / RETURN BELONGINGS",new Vector3(5.5f,3.25f,-1.8f),0,1.5f,new Vector2(3.8f,.45f));
        WorldLabel("Public return loop","LOUNGE / WEST WING",new Vector3(33.2f,2.8f,-45),90,1.6f,new Vector2(3,.45f));
    }
    static void Plant(Vector3 pos)
    {
        var pot=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pot.name="Botanist planter";pot.transform.SetParent(decor);pot.transform.position=pos+Vector3.up*.25f;pot.transform.localScale=new Vector3(.48f,.25f,.48f);pot.GetComponent<Renderer>().sharedMaterial=wood;UnityEngine.Object.DestroyImmediate(pot.GetComponent<Collider>());
        Box(decor,"Plant stem",pos+Vector3.up*.9f,new Vector3(.025f,1.05f,.025f),brass);
        for(int j=0;j<7;j++){var leaf=GameObject.CreatePrimitive(PrimitiveType.Sphere);leaf.name="Plant leaf";leaf.transform.SetParent(decor);float a=j*2.4f;leaf.transform.position=pos+new Vector3(Mathf.Sin(a)*.22f,.58f+j*.13f,Mathf.Cos(a)*.22f);leaf.transform.rotation=Quaternion.Euler(0,j*137,22);leaf.transform.localScale=new Vector3(.5f,.045f,.15f);leaf.GetComponent<Renderer>().sharedMaterial=botanist;UnityEngine.Object.DestroyImmediate(leaf.GetComponent<Collider>());}
    }
    static void WorldLabel(string name,string text,Vector3 position,float yaw,float size,Vector2 bounds)
    {
        var label=new GameObject(name).AddComponent<TextMeshPro>();label.transform.SetParent(decor);label.transform.position=position;label.transform.rotation=Quaternion.Euler(0,yaw,0);label.font=font;label.text=text;label.color=Gold;label.fontSize=size;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=bounds;
    }
    static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var old=parent.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchoredPosition=position;r.sizeDelta=size;return r;
    }
    static Image Panel(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
    {
        var rect=Rect(parent,name,pos,size);var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
    }
    static TMP_Text Text(Transform parent,string name,string text,Vector2 position,Vector2 size,float fontSize,Color color)
    {
        var rect=Rect(parent,name,position,size);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=text;label.fontSize=fontSize;label.color=color;label.raycastTarget=false;return label;
    }
    static Button MakeButton(Transform parent,string name,string text,Vector2 pos,UnityEngine.Events.UnityAction action)
    {
        var image=Panel(parent,name,pos,new Vector2(460,54),new Color(.16f,.2f,.22f));image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        UnityEventTools.AddPersistentListener(button.onClick,action);var label=Text(image.transform,"Text (TMP)",text,Vector2.zero,new Vector2(430,48),20,Paper);label.alignment=TextAlignmentOptions.Center;return button;
    }
    static void ButtonLabel(Button button,string value){button.GetComponentInChildren<TMP_Text>().text=value;}
    static void MapMarker(Transform parent,string name,string glyph,Vector3 world,Color color)
    {
        var marker=Panel(parent,name,Vector2.zero,new Vector2(23,23),color);var r=marker.rectTransform;
        // Markers are authored in the base grid; expansion preserves their floor-plan location.
        r.anchorMin=r.anchorMax=new Vector2((72+(world.x+1.5f)/63*880)/1024,(72+(world.z+61.5f)/63*880)/1024);r.anchoredPosition=Vector2.zero;
        var label=Text(r,"Landmark letter",glyph,Vector2.zero,new Vector2(23,23),16,Ink);label.alignment=TextAlignmentOptions.Center;
    }
}
