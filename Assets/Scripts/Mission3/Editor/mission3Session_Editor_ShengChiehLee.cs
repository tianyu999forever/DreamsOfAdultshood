using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class mission3Session_Editor_ShengChiehLee
{
    const string Command = "Library/Mission3UpgradeCommand.txt";
    const string ReportPath = "Library/Mission3UpgradeReport.txt";
    static double nextPoll;
    static mission3Session_Editor_ShengChiehLee()
    {
        EditorApplication.update += Poll;
        EditorApplication.delayCall += Report;
    }
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        if(!File.Exists(Command)) return;
        var command = File.ReadAllText(Command).Trim();
        if(command != "report" && !SessionState.GetBool("Mission3.RefreshedForCommand",false))
        {
            SessionState.SetBool("Mission3.RefreshedForCommand",true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return;
        }
        SessionState.SetBool("Mission3.RefreshedForCommand",false);
        File.Delete(Command);
        try
        {
            if(command == "report") Report();
            else if(command == "apply") Invoke("Apply");
            else if(command == "validate") Invoke("Validate");
            else if(command == "capture") Invoke("Capture");
            else if(command == "smoke") Invoke("Smoke");
            else if(command == "finalize") Invoke("FinalizeUpgrade");
            else if(command == "organize") mission3Folders_Editor_ShengChiehLee.Apply();
            else if(command == "folders-validate") mission3Folders_Editor_ShengChiehLee.Validate();
            else if(command == "architecture") mission3Architecture_Editor_ShengChiehLee.Apply();
            else throw new InvalidOperationException("Unknown Mission3 operation");
        }
        catch(Exception e) { File.WriteAllText(ReportPath,"ERROR\n"+e); Debug.LogException(e); }
    }
    static void Invoke(string method)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("mission3Presentation_Editor_ShengChiehLee")).FirstOrDefault(t=>t!=null);
        if(type == null) throw new InvalidOperationException("Presentation upgrade has not compiled yet.");
        type.GetMethod(method).Invoke(null,null);
    }
    public static void Write(string message) { File.WriteAllText(ReportPath,DateTime.Now.ToString("s")+"\n"+message); }
    static void Report()
    {
        string text="SESSION\nPlaying="+EditorApplication.isPlaying+"\n";
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            text+=scene.path+" | loaded="+scene.isLoaded+" | dirty="+scene.isDirty+" | active="+(scene==SceneManager.GetActiveScene())+"\n";
        }
        Write(text);
    }
}
