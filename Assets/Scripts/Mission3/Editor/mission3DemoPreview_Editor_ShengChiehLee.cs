using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class mission3DemoPreview_Editor_ShengChiehLee
{
    const string Pending="Mission3.GuidedPreview.Pending";
    const string Scene="Assets/Scenes/Demos/Mission3_Demo.unity";
    static mission3DemoPreview_Editor_ShengChiehLee(){EditorApplication.update+=Tick;}
    [MenuItem("Mission3/Test and Play Guided Demo")]
    public static void TestAndPlay()
    {
        EditorSceneManager.OpenScene(Scene);
        SessionState.SetBool(Pending,true);
        mission3Playtest_Editor_ShengChiehLee.Begin(true);
    }
    static void Tick()
    {
        if(File.Exists("Library/Mission3UpdateSpace.txt"))
        {
            File.Delete("Library/Mission3UpdateSpace.txt");SessionState.SetBool("Mission3.Space.Update",true);
        }
        if(SessionState.GetBool("Mission3.Space.Update",false)&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating)
        {
            if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}
            if(!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool("Mission3.Space.Update",false);
                try{mission3Demo_Editor_ShengChiehLee.UpdateScenes();TestAndPlay();}
                catch(Exception error){File.WriteAllText("Library/Mission3SpaceValidation.txt","SPACE_EDIT_FAIL\n"+error);UnityEngine.Debug.LogException(error);}
                return;
            }
        }
        if(File.Exists("Library/Mission3RestartGuidedDemo.txt")){File.Delete("Library/Mission3RestartGuidedDemo.txt");SessionState.SetBool("Mission3.GuidedPreview.Restart",true);}
        if(SessionState.GetBool("Mission3.GuidedPreview.Restart",false)&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating)
        {
            if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}
            if(!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetBool("Mission3.GuidedPreview.Restart",false);TestAndPlay();return;}
        }
        if(!SessionState.GetBool(Pending,false)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||SessionState.GetBool("Mission3.PresentationTest",false))return;
        SessionState.SetBool(Pending,false);
        if(!File.Exists("Library/Mission3UpgradeTest.txt")||!File.ReadAllText("Library/Mission3UpgradeTest.txt").StartsWith("PLAYMODE_PASS"))
        {mission3Session_Editor_ShengChiehLee.Write("GUIDED_DEMO_PREVIEW_TEST_FAILED / inspect Library/Mission3UpgradeTest.txt");return;}
        File.Copy("Library/Mission3UpgradeTest.txt","C:/Users/j1a2m/Documents/Codex/2026-10-25/w/outputs/mission3-demo/validation.txt",true);
        Play();
    }
    [MenuItem("Mission3/Play Guided Demo")]
    public static void Play()
    {
        EditorSceneManager.OpenScene(Scene);
        var gameView=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if(gameView!=null)EditorWindow.GetWindow(gameView,false,"Game",true).Show();
        mission3Session_Editor_ShengChiehLee.Write("GUIDED_DEMO_READY\nOpening fresh guided demo in Unity Game view.\n"+Scene);
        EditorApplication.EnterPlaymode();
    }
}
