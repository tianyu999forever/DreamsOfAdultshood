using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class mission3CheckoutFlow_Editor_ShengChiehLee
{
    const string Demo="Assets/Scenes/Demos/Mission3_Demo.unity";
    const string Output="C:/Users/j1a2m/Documents/Codex/2026-10-25/w/outputs/mission3-checkout-flow";
    static mission3CheckoutFlow_Editor_ShengChiehLee(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(File.Exists("Library/Mission3CheckoutFlow.txt")){File.Delete("Library/Mission3CheckoutFlow.txt");SessionState.SetBool("Mission3.CheckoutFlow.Apply",true);}
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists("Library/Mission3CheckoutNoticePreview.txt")&&!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            mission3DemoPreview_Editor_ShengChiehLee.Play();return;
        }
        if(File.Exists("Library/Mission3CheckoutNoticePreview.txt")&&EditorApplication.isPlaying)
        {
            File.Delete("Library/Mission3CheckoutNoticePreview.txt");mission3Playtest_Editor_ShengChiehLee.CaptureCheckoutNotice();return;
        }
        if(SessionState.GetBool("Mission3.CheckoutFlow.Apply",false))
        {
            if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            SessionState.SetBool("Mission3.CheckoutFlow.Apply",false);
            try
            {
                Apply();EditorSceneManager.OpenScene(Demo);
                var gameView=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");if(gameView!=null){var window=EditorWindow.GetWindow(gameView,false,"Game",true);window.Show();window.Focus();}
                EditorApplication.isPaused=false;SessionState.SetBool("Mission3.CheckoutFlow.Test",true);mission3Playtest_Editor_ShengChiehLee.Begin(true);
            }
            catch(Exception e){Report("CHECKOUT_FLOW_FAIL\n"+e);Debug.LogException(e);}
            return;
        }
        if(!SessionState.GetBool("Mission3.CheckoutFlow.Test",false)||EditorApplication.isPlayingOrWillChangePlaymode||SessionState.GetBool("Mission3.PresentationTest",false))return;
        SessionState.SetBool("Mission3.CheckoutFlow.Test",false);
        var report=File.ReadAllText("Library/Mission3UpgradeTest.txt");Report(report);
        if(report.StartsWith("PLAYMODE_PASS"))mission3DemoPreview_Editor_ShengChiehLee.Play();
    }
    static void Report(string report){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/validation.txt",report);mission3Session_Editor_ShengChiehLee.Write(report);}
    [MenuItem("Mission3/Apply Desk Returns and Lift Settlement")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play mode before updating checkout locations.");
        foreach(string path in new[]{mission3Paths_Editor_ShengChiehLee.Scene,Demo})
        {
            var scene=SceneManager.GetSceneByPath(path);if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);Install(scene);Validate(scene);
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save "+path);
        }
        AssetDatabase.SaveAssets();
    }
    public static void Install(Scene scene)
    {
        var roots=scene.GetRootGameObjects();var manager=roots.SelectMany(x=>x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).Single();
        var lift=roots.SelectMany(x=>x.GetComponentsInChildren<mission3Lift_Prop_ShengChiehLee>(true)).Single();manager.departureLift=lift;
        var actions=roots.SelectMany(x=>x.GetComponentsInChildren<mission3Interact_Prop_ShengChiehLee>(true)).ToArray();
        manager.returnDesk=actions.Single(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.Checkout&&x.GetComponentInParent<mission3Lift_Prop_ShengChiehLee>()==null);
        manager.returnDesk.displayName="Return belongings / Checkout - Lost & Found desk";
        foreach(var old in actions.Where(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.Checkout&&x.GetComponentInParent<mission3Lift_Prop_ShengChiehLee>()!=null))
        {
            var collision=old.GetComponent<Collider>();if(collision!=null)UnityEngine.Object.DestroyImmediate(collision);
            old.gameObject.name="Lift departure reminder";UnityEngine.Object.DestroyImmediate(old);
        }
        var reminder=lift.transform.Find("Lift checkout text");if(reminder!=null)reminder.GetComponent<TMP_Text>().text="RETURN ITEMS AT\nLOBBY DESK FIRST";
        var worldButton=manager.nextLevelButton;
        worldButton.onClick.RemoveAllListeners();while(worldButton.onClick.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(worldButton.onClick,0);
        UnityEventTools.AddPersistentListener(worldButton.onClick,manager.UseLiftTerminal);
        worldButton.GetComponent<mission3Interact_Prop_ShengChiehLee>().displayName="Next Level / settle score inside lift";
        var ui=manager.canvasUI;
        ui.pages[6].transform.Find("Title").GetComponent<TMP_Text>().text="Return these belongings?";
        ui.pages[6].transform.Find("Confirmation").GetComponent<TMP_Text>().text="Confirming ends collection and locks the owner choices for your returned belongings.\n\nThen enter the lift and use NEXT LEVEL to settle your score. Your final score and NEXT LEVEL are available only inside the lift.";
        ui.pages[6].transform.Find("FinalizeButton").GetComponentInChildren<TMP_Text>().text="CONFIRM RETURN / THEN GO TO LIFT";
        ui.pages[2].transform.Find("Navigation hint").GetComponent<TMP_Text>().text="R  RETURN ITEMS / L  SETTLE + NEXT LEVEL";
        ui.pages[2].transform.Find("Legend key").GetComponent<TMP_Text>().text="RED ARROW: You    /    CYAN: Belonging\nAMBER N: Note    /    MUTED +: Collected\nR: RETURN DESK    L: SETTLE / NEXT    S: LOUNGE\n\n301 Traveller / west wing\n302 Musician / north wing\n303 Botanist / east wing\nAll items are shown. North is up.";
        ui.pages[1].transform.Find("Brief").GetComponent<TMP_Text>().text="A note beside your keys reads: \"Find lost items. Use guest notes to identify their owners. Return them at Lost & Found, then take the lift.\"";
        var props=roots.Single(x=>x.name=="Mission3_PropsAndInteractions").transform;
        var checkoutLabel=props.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(x=>x.name=="Checkout label");if(checkoutLabel!=null)checkoutLabel.text="CHECKOUT\nLOST & FOUND\nRETURN ITEMS HERE";
        AddNotice(props);
        manager.RefreshNextLevelStatus();ui.Refresh();EditorUtility.SetDirty(manager);EditorUtility.SetDirty(ui);EditorUtility.SetDirty(worldButton);EditorUtility.SetDirty(lift);
        EditorSceneManager.MarkSceneDirty(scene);
    }
    static void AddNotice(Transform props)
    {
        var old=props.Find("CheckoutFlowNotice");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var notice=new GameObject("CheckoutFlowNotice").transform;notice.SetParent(props,false);notice.localPosition=new Vector3(12.2f,1.9f,-5.7f);
        var brass=AssetDatabase.LoadAssetAtPath<Material>(mission3Paths_Editor_ShengChiehLee.Material("mission3_Brass"));
        var ink=AssetDatabase.LoadAssetAtPath<Material>(mission3Paths_Editor_ShengChiehLee.Material("mission3_Ink"));
        Box(notice,"Notice brass frame",Vector3.zero,new Vector3(1.78f,2.1f,.12f),brass);
        Box(notice,"Notice face",new Vector3(0,0,-.068f),new Vector3(1.65f,1.97f,.018f),ink);
        Box(notice,"Notice support",new Vector3(0,-1.3f,.02f),new Vector3(.1f,1.2f,.1f),brass);
        Box(notice,"Notice base",new Vector3(0,-1.84f,0),new Vector3(1.25f,.12f,.52f),brass);
        Text(notice,"Notice heading","CHECKOUT NOTICE",new Vector3(0,.78f,-.085f),1.55f,new Vector2(1.5f,.25f),new Color(.97f,.81f,.48f));
        Text(notice,"Return step","01 / RETURN ITEMS\nCHECKOUT / LOST & FOUND\nDESK ONLY",new Vector3(0,.31f,-.085f),1.12f,new Vector2(1.5f,.57f),new Color(.94f,.92f,.84f));
        Text(notice,"Departure step","02 / ENTER THE LIFT\nSETTLE SCORE\nNEXT LEVEL INSIDE",new Vector3(0,-.40f,-.085f),1.18f,new Vector2(1.5f,.65f),new Color(.55f,.87f,.86f));
    }
    static void Box(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
    }
    static void Text(Transform parent,string name,string value,Vector3 position,float size,Vector2 bounds,Color color)
    {
        var text=new GameObject(name).AddComponent<TextMeshPro>();text.transform.SetParent(parent,false);text.transform.localPosition=position;
        text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");text.text=value;text.fontSize=size;text.color=color;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=bounds;
    }
    public static void Validate(Scene scene)
    {
        var manager=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).Single();
        var checkouts=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Interact_Prop_ShengChiehLee>(true)).Where(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.Checkout).ToArray();
        if(checkouts.Length!=1||checkouts[0]!=manager.returnDesk||manager.returnDesk.GetComponent<Collider>()==null)throw new Exception("Returns must be available only at the lobby desk.");
        if(manager.departureLift==null||manager.returnDesk.GetComponentInParent<mission3Lift_Prop_ShengChiehLee>()!=null)throw new Exception("Checkout locations are not assigned correctly.");
        mission3NextLevel_Editor_ShengChiehLee.Validate(manager);
        if(scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Count(x=>x.name=="CheckoutFlowNotice")!=1)throw new Exception("Checkout notice is missing or duplicated.");
    }
}
