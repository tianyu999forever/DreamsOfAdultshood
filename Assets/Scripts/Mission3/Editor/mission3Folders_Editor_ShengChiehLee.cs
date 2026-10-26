using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class mission3Folders_Editor_ShengChiehLee
{
    const string Snapshot="Library/Mission3FolderGuidSnapshot.json";
    const string Pending="Mission3.FolderValidationPending";
    const string StopPending="Mission3.FolderStopPending";
    [Serializable] class Entry { public string from,to,guid; }
    [Serializable] class Manifest { public List<Entry> entries=new List<Entry>(); }
    static readonly string[,] Scripts={
        {"mission3Hotel_Manager_ShengChiehLee.cs","Gameplay"},
        {"mission3Rules_Manager_ShengChiehLee.cs","Gameplay"},
        {"mission3Interact_Prop_ShengChiehLee.cs","Interactions"},
        {"mission3Lift_Prop_ShengChiehLee.cs","Interactions"},
        {"mission3Canvas_UI_ShengChiehLee.cs","UI"},
        {"mission3Watch_Player_ShengChiehLee.cs","Player"}
    };
    [InitializeOnLoadMethod]
    static void Initialize()
    {
        EditorApplication.playModeStateChanged-=OnPlayState;EditorApplication.playModeStateChanged+=OnPlayState;
        if(SessionState.GetBool(StopPending,false)&&!EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.delayCall+=Apply;
        if(SessionState.GetBool(Pending,false))EditorApplication.delayCall+=ValidateWhenReady;
    }
    static void OnPlayState(PlayModeStateChange change)
    {
        if(change==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(StopPending,false))EditorApplication.delayCall+=Apply;
    }
    [MenuItem("Mission3/Organize Mission3 Folders")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetBool(StopPending,true);EditorApplication.playModeStateChanged-=OnPlayState;EditorApplication.playModeStateChanged+=OnPlayState;EditorApplication.ExitPlaymode();return;
        }
        SessionState.SetBool(StopPending,false);
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save unsaved scene edits before reorganizing assets.");
        if(!Directory.Exists("Assets/Mission3")&&File.Exists(Snapshot)){Validate();return;}
        mission3Paths_Editor_ShengChiehLee.EnsureFolders();
        // Resume an interrupted batch from its original full plan, preserving every GUID record.
        var manifest=File.Exists(Snapshot)?JsonUtility.FromJson<Manifest>(File.ReadAllText(Snapshot)):new Manifest();
        void Add(string from,string to)
        {
            string guid=AssetDatabase.AssetPathToGUID(from);
            if(string.IsNullOrEmpty(guid)){if(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(to)))return;throw new FileNotFoundException("Mission3 asset not found: "+from);}
            if(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(to)))throw new IOException("Destination already exists: "+to);
            manifest.entries.Add(new Entry{from=from,to=to,guid=guid});
        }
        if(manifest.entries.Count==0)
        {
        foreach(var file in Directory.GetFiles("Assets/Mission3","*.mat"))
        {
            string from=file.Replace('\\','/');Add(from,mission3Paths_Editor_ShengChiehLee.Material(Path.GetFileNameWithoutExtension(from)));
        }
        foreach(string name in new[]{"mission3FloorMap.png","mission3FloorPlan.png"})Add("Assets/Mission3/"+name,mission3Paths_Editor_ShengChiehLee.Maps+"/"+name);
        Add("Assets/Mission3/mission3PlayerArrow.png",mission3Paths_Editor_ShengChiehLee.InterfaceTextures+"/mission3PlayerArrow.png");
        Add("Assets/Mission3/README_Mission3.md",mission3Paths_Editor_ShengChiehLee.Documentation+"/README_Mission3.md");
        Add("Assets/Mission 3.unity",mission3Paths_Editor_ShengChiehLee.Backups+"/Mission3_PreUpgrade.unity");
        Add("Assets/Mission3/Mission 3.unity",mission3Paths_Editor_ShengChiehLee.Backups+"/Mission3_Prototype.unity");
        for(int i=0;i<Scripts.GetLength(0);i++)Add(mission3Paths_Editor_ShengChiehLee.Scripts+"/"+Scripts[i,0],mission3Paths_Editor_ShengChiehLee.Scripts+"/"+Scripts[i,1]+"/"+Scripts[i,0]);
        }
        File.WriteAllText(Snapshot,JsonUtility.ToJson(manifest,true));
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var entry in manifest.entries)
            {
                if(!File.Exists(entry.from))
                {
                    if(File.Exists(entry.to+".meta")&&File.ReadAllLines(entry.to+".meta").Any(x=>x=="guid: "+entry.guid))continue;
                    throw new FileNotFoundException("Could not resume asset move: "+entry.from);
                }
                string error=AssetDatabase.MoveAsset(entry.from,entry.to);if(!string.IsNullOrEmpty(error))throw new IOException(error);
                // AssetDatabase path indices are intentionally deferred during StartAssetEditing.
                if(!File.ReadAllLines(entry.to+".meta").Any(x=>x=="guid: "+entry.guid))throw new Exception("GUID changed: "+entry.to);
            }
            // Delete only the verified-empty old container, never its assets or backups.
            if(Directory.Exists("Assets/Mission3")&&!Directory.EnumerateFileSystemEntries("Assets/Mission3").Any())
                if(!AssetDatabase.DeleteAsset("Assets/Mission3"))throw new IOException("Could not remove empty legacy folder.");
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.SaveAssets();SessionState.SetBool(Pending,true);EditorApplication.delayCall+=ValidateWhenReady;
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }
    static void ValidateWhenReady()
    {
        if(!SessionState.GetBool(Pending,false))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=ValidateWhenReady;return;}
        try{Validate();SessionState.SetBool(Pending,false);}
        catch(Exception e){SessionState.SetBool(Pending,false);mission3Session_Editor_ShengChiehLee.Write("FOLDER_VALIDATION_FAIL\n"+e);Debug.LogException(e);}
    }
    [MenuItem("Mission3/Validate Mission3 Folder Organization")]
    public static void Validate()
    {
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Snapshot));
        foreach(var entry in manifest.entries)
        {
            if(AssetDatabase.AssetPathToGUID(entry.to)!=entry.guid||AssetDatabase.GUIDToAssetPath(entry.guid)!=entry.to)throw new Exception("Moved asset GUID mismatch: "+entry.to);
            if(File.Exists(entry.from))throw new Exception("Legacy duplicate remains: "+entry.from);
        }
        if(Directory.Exists("Assets/Mission3"))throw new Exception("Legacy mixed-assets folder remains.");
        foreach(var entry in EditorBuildSettings.scenes)if(entry.path.Contains("/Backups/"))throw new Exception("A backup scene is in Build Settings.");
        if(!EditorBuildSettings.scenes.Any(x=>x.path==mission3Paths_Editor_ShengChiehLee.Scene&&x.enabled))throw new Exception("Canonical Mission3 build entry missing.");
        var scene=SceneManager.GetSceneByPath(mission3Paths_Editor_ShengChiehLee.Scene);
        if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene(mission3Paths_Editor_ShengChiehLee.Scene,OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);mission3Presentation_Editor_ShengChiehLee.Validate();
        var manager=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).Single();
        foreach(var obj in new UnityEngine.Object[]{manager.floorMap,manager.canvasUI.mapRect.GetComponent<UnityEngine.UI.RawImage>().texture,manager.canvasUI.playerMarker.GetComponent<UnityEngine.UI.RawImage>().texture})
            if(obj==null||!AssetDatabase.GetAssetPath(obj).StartsWith(mission3Paths_Editor_ShengChiehLee.Textures+"/"))throw new Exception("Missing moved map/UI texture.");
        var errors=AssetDatabase.GetDependencies(mission3Paths_Editor_ShengChiehLee.Scene,true).Where(x=>!File.Exists(x)&&!AssetDatabase.IsValidFolder(x)&&!x.StartsWith("Resources/")&&AssetDatabase.LoadMainAssetAtPath(x)==null).ToArray();
        if(errors.Length>0)throw new Exception("Missing scene dependencies: "+string.Join(", ",errors));
        mission3Session_Editor_ShengChiehLee.Write("FOLDER_ORGANIZATION_PASS\n"+manifest.entries.Count+" assets moved with original GUIDs preserved. Canonical scene and Build Settings retained. Backups isolated. Map/UI texture references, maze, scoring and scene scripts validated.\n"+string.Join("\n",manifest.entries.Select(x=>x.to)));
    }
}
