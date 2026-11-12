using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class mission3Welcome_Editor_ShengChiehLee
{

    const string Brief = "A note beside your keys reads: \"Find lost items. Use guest notes to identify their owners. Return them at Lost & Found, then take the lift. After two finds, stay quiet and turn corners to avoid the night patrol.\"";
    static mission3Welcome_Editor_ShengChiehLee() { File.WriteAllText("Library/Mission3WelcomeReady.txt","welcome-short-v3"); EditorApplication.update += Poll; }
    static void Poll()
    {
        const string request="Library/Mission3WelcomeCopy.txt";
        if (!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode) return;
        string backupDirectory=Path.GetFullPath(File.ReadAllText(request).Trim()); File.Delete(request);
        Directory.CreateDirectory(backupDirectory);
        try
        {
            var active=SceneManager.GetActiveScene();
            if(active.path!="Assets/Scenes/Mission3.unity") throw new Exception("Keep the current Mission3 scene open to preserve its unsaved edits.");
            string report=Apply(active,backupDirectory,"Mission3");
            var demo=SceneManager.GetSceneByPath("Assets/Mission3_ShengChiehLee/Scenes/Mission3_Preview.unity"); bool opened=!demo.IsValid()||!demo.isLoaded;
            if(opened) demo=EditorSceneManager.OpenScene("Assets/Mission3_ShengChiehLee/Scenes/Mission3_Preview.unity",OpenSceneMode.Additive);
            try { report+=Apply(demo,backupDirectory,"Mission3_Demo"); }
            finally { if(opened) EditorSceneManager.CloseScene(demo,true); SceneManager.SetActiveScene(active); }
            File.WriteAllText("Library/Mission3WelcomeCopyReport.txt","STORY_SHORT_PASS\n"+report);
        }
        catch(Exception error) { File.WriteAllText("Library/Mission3WelcomeCopyReport.txt","FAIL\n"+error); Debug.LogException(error); }
    }
    static string Apply(Scene scene,string backupDirectory,string name)
    {
        var manager=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).Single();
        var page=manager.canvasUI.pages[1].transform;
        var brief=page.Find("Brief").GetComponent<TMP_Text>();
        string backup="Assets/Mission3_ShengChiehLee/Welcome/"+name+"_BeforeCopy.unity";
        if (!EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Could not back up the current scene.");
        File.Copy(backup,Path.Combine(backupDirectory,name+"_BeforeCopy.unity"),true);
        if(!AssetDatabase.DeleteAsset(backup)) throw new Exception("Could not clean up the temporary scene backup.");
        Undo.RecordObject(brief,"Update Mission3 welcome story");
        brief.text=Brief;
        float height=brief.GetPreferredValues(Brief,brief.rectTransform.rect.width,Mathf.Infinity).y;
        if(height>brief.rectTransform.rect.height) throw new Exception("The new description exceeds its text area.");
        EditorUtility.SetDirty(brief); EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save the new welcome text.");
        return name+": story saved; title and other current edits preserved; description height "+height.ToString("F1")+" / "+brief.rectTransform.rect.height.ToString("F1")+".\n";
    }
}
