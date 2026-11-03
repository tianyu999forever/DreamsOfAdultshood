using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class mission3Result_Editor_ShengChiehLee
{
    public const string PrefabPath="Assets/Prefabs/Mission3/UI/Mission3ResultCanvas.prefab";
    const string Demo="Assets/Scenes/Demos/Mission3_Demo.unity";
    const string Output="C:/Users/j1a2m/Documents/Codex/2026-10-25/w/outputs/mission3-results";
    static readonly Color Paper=new Color(.94f,.93f,.86f),Gold=new Color(.80f,.63f,.34f),Cyan=new Color(.06f,.65f,.74f),Muted=new Color(.58f,.67f,.67f);
    static TMP_FontAsset font;
    static int previewFrames;
    static mission3Result_Editor_ShengChiehLee(){EditorApplication.update+=Tick;}

    static void Tick()
    {
        if(File.Exists("Library/Mission3ConnectResultsToMission1.txt"))
        {
            File.Delete("Library/Mission3ConnectResultsToMission1.txt");SessionState.SetBool("Mission3.Results.UseMission1",true);SessionState.SetBool("Mission3.Results.Apply",true);
        }
        if(File.Exists("Library/Mission3UpdateResults.txt"))
        {File.Delete("Library/Mission3UpdateResults.txt");SessionState.SetBool("Mission3.Results.Apply",true);}
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(SessionState.GetBool("Mission3.Results.Apply",false))
        {
            if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            SessionState.SetBool("Mission3.Results.Apply",false);
            try{Apply();SessionState.SetBool("Mission3.Results.Test",true);EditorSceneManager.OpenScene(Demo);mission3Playtest_Editor_ShengChiehLee.Begin(true);}
            catch(Exception error){File.WriteAllText("Library/Mission3ResultsValidation.txt","RESULTS_FAIL\n"+error);Debug.LogException(error);}
            return;
        }
        if(SessionState.GetBool("Mission3.Results.Test",false)&&!EditorApplication.isPlayingOrWillChangePlaymode&&!SessionState.GetBool("Mission3.PresentationTest",false))
        {
            SessionState.SetBool("Mission3.Results.Test",false);
            var report=File.ReadAllText("Library/Mission3UpgradeTest.txt");Directory.CreateDirectory(Output);File.WriteAllText(Output+"/validation.txt",report);
            if(report.StartsWith("PLAYMODE_PASS"))Preview();return;
        }
        if(!SessionState.GetBool("Mission3.Results.Preview",false)||!EditorApplication.isPlaying)return;
        if(++previewFrames<30)return;
        SessionState.SetBool("Mission3.Results.Preview",false);
        var manager=UnityEngine.Object.FindFirstObjectByType<mission3Hotel_Manager_ShengChiehLee>();
        foreach(var p in manager.gameObject.scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Interact_Prop_ShengChiehLee>(true)).Where(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.LostProperty||x.kind==mission3Interact_Prop_ShengChiehLee.Kind.ServiceDoor).ToArray())manager.Interact(p);
        for(int i=0;i<6;i++)manager.assignment[i]=i/2;
        var desk=manager.returnDesk.transform.position+Vector3.back*2.1f;desk.y=1.42f;
        manager.playerBody.transform.position=desk;manager.playerBody.position=desk;Physics.SyncTransforms();manager.Interact(manager.returnDesk);manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Confirm);manager.CompleteCheckout();
        manager.departureLift.Open();var passenger=manager.departureLift.transform.TransformPoint(new Vector3(0,1.42f,.95f));manager.playerBody.transform.position=passenger;manager.playerBody.position=passenger;Physics.SyncTransforms();
        manager.UseLiftTerminal();manager.canvasUI.Refresh();
        mission3Session_Editor_ShengChiehLee.Write("RESULTS_PREVIEW_READY\n100-point example result. Replay starts an ordinary new run.\n"+PrefabPath);
    }

    [MenuItem("Mission3/Update Score Results Canvas")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play mode before updating the results Canvas.");
        EnsurePrefab();
        bool connectMission1=SessionState.GetBool("Mission3.Results.UseMission1",false);
        const string destination="Assets/Scenes/Mission1.unity";
        if(connectMission1)
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(destination)==null)throw new Exception("Mission1 scene is missing.");
            var scenes=EditorBuildSettings.scenes.ToList();var entry=scenes.Find(x=>x.path==destination);
            if(entry==null)scenes.Add(new EditorBuildSettingsScene(destination,true));else entry.enabled=true;
            EditorBuildSettings.scenes=scenes.ToArray();
        }
        foreach(string path in new[]{mission3Paths_Editor_ShengChiehLee.Scene,Demo})
        {
            var scene=SceneManager.GetSceneByPath(path);
            if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);Install(scene);
            if(connectMission1)
            {
                var manager=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).Single();
                manager.nextSceneName=destination;manager.RefreshNextLevelStatus();EditorUtility.SetDirty(manager);
            }
            Validate(scene);
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save "+path);
        }
        SessionState.SetBool("Mission3.Results.UseMission1",false);
        AssetDatabase.SaveAssets();
        File.WriteAllText("Library/Mission3ResultsValidation.txt","RESULTS_EDIT_PASS\nReusable Canvas prefab, score fields, six item rows, persistent replay/continue actions, scene manager references and next-level gating installed in canonical and demo scenes.\n");
    }
    [MenuItem("Mission3/Connect Results to Mission1")]
    public static void ConnectMission1()
    {
        SessionState.SetBool("Mission3.Results.UseMission1",true);Apply();
    }
    [MenuItem("Mission3/Preview Score Results")]
    public static void Preview()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play mode before opening the results preview.");
        EditorSceneManager.OpenScene(Demo);SessionState.SetBool("Mission3.Results.Preview",true);previewFrames=0;
        var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");if(type!=null)EditorWindow.GetWindow(type,false,"Game",true).Show();
        EditorApplication.EnterPlaymode();
    }
    public static void Install(Scene scene)
    {
        EnsurePrefab();
        var manager=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).Single();
        var ui=manager.canvasUI;var existing=ui.pages[7];
        var result=existing==null?null:existing.GetComponent<mission3Result_UI_ShengChiehLee>();
        if(result==null)
        {
            if(existing!=null)UnityEngine.Object.DestroyImmediate(existing);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
            result=go.GetComponent<mission3Result_UI_ShengChiehLee>();ui.pages[7]=go;
        }
        result.manager=manager;ui.resultsCanvas=result;ui.result=result.summary;
        if(string.IsNullOrWhiteSpace(manager.nextSceneName))manager.nextSceneName="Assets/Scenes/Mission4.unity";
        result.gameObject.SetActive(manager.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.Result);
        EditorUtility.SetDirty(result);EditorUtility.SetDirty(ui);EditorUtility.SetDirty(manager);
        PrefabUtility.RecordPrefabInstancePropertyModifications(result);
        manager.RefreshNextLevelStatus();EditorSceneManager.MarkSceneDirty(scene);
    }
    public static void Validate(Scene scene)
    {
        var manager=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).Single();var result=manager.canvasUI.resultsCanvas;
        if(result==null||result.manager!=manager||manager.canvasUI.pages[7]!=result.gameObject)throw new Exception("Results Canvas is not connected to the mission.");
        if(result.GetComponent<Canvas>().renderMode!=RenderMode.ScreenSpaceOverlay||result.GetComponent<GraphicRaycaster>()==null||result.GetComponent<CanvasScaler>()==null)throw new Exception("Results Canvas components are incomplete.");
        if(result.itemStates.Length!=6||result.ownerLabels.Length!=6||result.itemStates.Any(x=>x==null)||result.ownerLabels.Any(x=>x==null))throw new Exception("Results rows are incomplete.");
        foreach(var entry in new[]{(result.continueButton,nameof(result.Continue)),(result.replayButton,nameof(result.Replay))})
            if(entry.Item1.onClick.GetPersistentEventCount()!=1||entry.Item1.onClick.GetPersistentTarget(0)!=result||entry.Item1.onClick.GetPersistentMethodName(0)!=entry.Item2)throw new Exception("Results button is not bound: "+entry.Item2);
    }
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;int cut=path.LastIndexOf('/');EnsureFolder(path.Substring(0,cut));AssetDatabase.CreateFolder(path.Substring(0,cut),path.Substring(cut+1));
    }
    static void EnsurePrefab()
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)!=null)return;
        EnsureFolder("Assets/Prefabs/Mission3/UI");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        var root=new GameObject("Mission3_ResultCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(mission3Result_UI_ShengChiehLee));
        try
        {
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=50;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var result=root.GetComponent<mission3Result_UI_ShengChiehLee>();
            var shade=Panel(root.transform,"Backdrop",Vector2.zero,Vector2.zero,new Color(.01f,.025f,.03f,.94f));shade.anchorMin=Vector2.zero;shade.anchorMax=Vector2.one;shade.sizeDelta=Vector2.zero;
            var card=Panel(root.transform,"ResultPanel",Vector2.zero,new Vector2(1660,900),new Color(.035f,.075f,.085f));
            Panel(card,"Gold edge",new Vector2(-827,0),new Vector2(5,900),Gold);
            Label(card,"Section","MISSION 03 / SHIFT COMPLETE",new Vector2(-150,385),new Vector2(1210,30),17,Gold);
            Label(card,"Title","The last checkout",new Vector2(-150,322),new Vector2(1210,70),46,Paper);
            Label(card,"Hotel","HOTEL 24",new Vector2(630,378),new Vector2(250,40),23,Gold,TextAlignmentOptions.Right);
            Panel(card,"Header divider",new Vector2(0,270),new Vector2(1510,1),new Color(.24f,.34f,.35f));
            var score=Panel(card,"Final score card",new Vector2(-550,-5),new Vector2(410,480),new Color(.06f,.13f,.14f));
            Label(score,"Final score heading","FINAL SCORE",new Vector2(0,200),new Vector2(350,30),18,Gold,TextAlignmentOptions.Center);
            result.totalScore=Label(score,"Score","0",new Vector2(0,110),new Vector2(350,145),116,Paper,TextAlignmentOptions.Center);
            Label(score,"Maximum","/ 100 POINTS",new Vector2(0,18),new Vector2(350,40),20,Muted,TextAlignmentOptions.Center);
            Panel(score,"Score divider",new Vector2(0,-28),new Vector2(330,1),new Color(.24f,.34f,.35f));
            result.summary=Label(score,"Summary","BELONGINGS FOUND   0 / 6\nCORRECT RETURNS      0 / 6\nGUEST PAIRS                 0 / 3",new Vector2(0,-100),new Vector2(340,110),20,Paper);
            Label(score,"Locked hint","FINAL CHECKOUT / SCORE LOCKED",new Vector2(0,-208),new Vector2(360,28),14,Gold,TextAlignmentOptions.Center);
            var items=ScoreCard(card,"Correct returns",new Vector2(-115,170),"CORRECT RETURNS");result.itemPoints=items.Item1;result.itemDetail=items.Item2;
            var pairs=ScoreCard(card,"Guest pairs",new Vector2(220,170),"GUEST PAIRS");result.pairPoints=pairs.Item1;result.pairDetail=pairs.Item2;
            var bonus=ScoreCard(card,"Exploration",new Vector2(555,170),"EXPLORATION");result.explorationPoints=bonus.Item1;result.explorationDetail=bonus.Item2;
            Label(card,"Items heading","BELONGING",new Vector2(-55,57),new Vector2(430,25),15,Gold);
            Label(card,"Owners heading","OWNER",new Vector2(320,57),new Vector2(125,25),15,Gold,TextAlignmentOptions.Center);
            Label(card,"Returns heading","RETURN RESULT",new Vector2(590,57),new Vector2(260,25),15,Gold,TextAlignmentOptions.Center);
            result.ownerLabels=new TMP_Text[6];result.itemStates=new TMP_Text[6];
            for(int i=0;i<6;i++)
            {
                float y=11-i*49;var row=Panel(card,"Belonging "+(i+1),new Vector2(230,y),new Vector2(1040,43),i%2==0?new Color(.065f,.12f,.13f):new Color(.045f,.095f,.105f));
                Label(row,"Name",(i+1).ToString("00")+"  "+mission3Rules_Manager_ShengChiehLee.Items[i],new Vector2(-280,0),new Vector2(440,37),20,Paper);
                result.ownerLabels[i]=Label(row,"Owner",(301+i/2).ToString(),new Vector2(90,0),new Vector2(125,37),19,Paper,TextAlignmentOptions.Center);
                result.itemStates[i]=Label(row,"Status","NOT FOUND / +0",new Vector2(360,0),new Vector2(260,37),17,Muted,TextAlignmentOptions.Center);
            }
            result.nextLevelHint=Label(card,"Next level hint","Complete checkout to continue.",new Vector2(0,-306),new Vector2(1510,45),19,Muted,TextAlignmentOptions.Center);
            result.replayButton=Button(card,"PlayAgainButton","PLAY AGAIN",new Vector2(-520,-388),new Vector2(460,64),result.Replay,false);
            result.continueButton=Button(card,"ContinueButton","NEXT LEVEL",new Vector2(405,-388),new Vector2(700,64),result.Continue,true);
            if(PrefabUtility.SaveAsPrefabAsset(root,PrefabPath)==null)throw new IOException("Could not save results Canvas prefab.");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    static (TMP_Text,TMP_Text) ScoreCard(Transform parent,string name,Vector2 position,string heading)
    {
        var card=Panel(parent,name,position,new Vector2(300,140),new Color(.06f,.13f,.14f));
        Label(card,"Heading",heading,new Vector2(0,43),new Vector2(276,25),15,Gold,TextAlignmentOptions.Center);
        var points=Label(card,"Points","+0",new Vector2(0,0),new Vector2(270,50),37,Paper,TextAlignmentOptions.Center);
        var detail=Label(card,"Detail","",new Vector2(0,-45),new Vector2(286,30),14,Muted,TextAlignmentOptions.Center);return(points,detail);
    }
    static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
    }
    static RectTransform Panel(Transform parent,string name,Vector2 position,Vector2 size,Color color)
    {
        var rect=Rect(parent,name,position,size);var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=name=="Backdrop"||name=="ResultPanel";return rect;
    }
    static TMP_Text Label(Transform parent,string name,string value,Vector2 position,Vector2 size,float points,Color color,TextAlignmentOptions align=TextAlignmentOptions.Left)
    {
        var text=Rect(parent,name,position,size).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=points;text.color=color;text.alignment=align;text.raycastTarget=false;return text;
    }
    static Button Button(Transform parent,string name,string label,Vector2 position,Vector2 size,UnityAction action,bool accent)
    {
        var rect=Panel(parent,name,position,size,accent?Cyan:new Color(.14f,.22f,.23f));var image=rect.GetComponent<Image>();image.raycastTarget=true;
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};
        var colors=button.colors;colors.highlightedColor=new Color(1,.90f,.70f);colors.pressedColor=new Color(.65f,.8f,.8f);colors.disabledColor=new Color(.50f,.55f,.55f,.65f);button.colors=colors;
        Label(rect,"Text (TMP)",label,Vector2.zero,size-new Vector2(24,8),22,Paper,TextAlignmentOptions.Center);UnityEventTools.AddPersistentListener(button.onClick,action);return button;
    }
}
