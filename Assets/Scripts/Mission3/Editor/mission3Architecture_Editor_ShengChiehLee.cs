using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class mission3Architecture_Editor_ShengChiehLee
{
    static readonly string[] Groups={"01_Floors","02_Walls","03_Ceilings","04_WallDetails","05_Lighting","06_Furniture","07_Other"};
    [MenuItem("Mission3/Organize Hotel Architecture")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before organizing the architecture.");
        var scene=SceneManager.GetSceneByPath(mission3Paths_Editor_ShengChiehLee.Scene);
        if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene(mission3Paths_Editor_ShengChiehLee.Scene,OpenSceneMode.Additive);
        if(scene.isDirty)throw new InvalidOperationException("Save the current scene edits before organizing the architecture.");
        File.Copy(scene.path,"Library/Mission3_HotelArchitecture_BeforeGrouping.unity",true);
        SceneManager.SetActiveScene(scene);
        string summary=Organize(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save Mission3 architecture groups.");
        mission3Presentation_Editor_ShengChiehLee.Validate();
        var root=scene.GetRootGameObjects().Single(x=>x.name=="Mission3_HotelArchitecture");
        Selection.activeGameObject=root;EditorGUIUtility.PingObject(root);
        mission3Session_Editor_ShengChiehLee.Write("ARCHITECTURE_ORGANIZATION_PASS\n"+summary+"\nWorld transforms, renderer/collider/light counts and scene references preserved. Scene saved.");
    }
    public static string Organize(Scene scene)
    {
        var root=scene.GetRootGameObjects().Single(x=>x.name=="Mission3_HotelArchitecture").transform;
        var all=root.GetComponentsInChildren<Transform>(true);
        var matrices=all.ToDictionary(x=>x,x=>x.localToWorldMatrix);
        int renderers=root.GetComponentsInChildren<Renderer>(true).Length;
        int colliders=root.GetComponentsInChildren<Collider>(true).Length;
        int lights=root.GetComponentsInChildren<Light>(true).Length;
        var children=root.Cast<Transform>().Where(x=>!Groups.Contains(x.name)).ToArray();
        int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Organize Mission3 Hotel Architecture");
        foreach(var child in children)Undo.SetTransformParent(child,Folder(root,Category(child)),"Group hotel architecture");
        int sibling=0;
        foreach(string name in Groups)
        {
            var group=root.Find(name);if(group!=null){Undo.RecordObject(group,"Sort architecture groups");group.SetSiblingIndex(sibling++);}
        }
        foreach(var t in matrices)
            for(int i=0;i<16;i++)if(Mathf.Abs(t.Key.localToWorldMatrix[i]-t.Value[i])>.0001f)throw new Exception("Object moved while grouping: "+t.Key.name);
        if(root.GetComponentsInChildren<Renderer>(true).Length!=renderers||root.GetComponentsInChildren<Collider>(true).Length!=colliders||root.GetComponentsInChildren<Light>(true).Length!=lights)throw new Exception("Architecture component count changed.");
        Undo.CollapseUndoOperations(undo);
        return string.Join("\n",root.Cast<Transform>().Select(x=>x.name+": "+x.GetComponentsInChildren<Transform>(true).Count(t=>t!=x&&(t.GetComponent<Renderer>()!=null||t.GetComponent<Light>()!=null))+" objects"));
    }
    static Transform Folder(Transform root,string path)
    {
        foreach(string name in path.Split('/'))
        {
            var child=root.Find(name);
            if(child==null){var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Create architecture group");go.transform.SetParent(root,false);child=go.transform;}
            root=child;
        }
        return root;
    }
    static string Category(Transform child)
    {
        string name=child.name;
        if(name.StartsWith("Floor_")||name=="Service floor")return "01_Floors";
        if(name.StartsWith("Wall_"))return "02_Walls/MainWalls";
        if(name=="Service north jamb"||name=="Service south jamb")return "02_Walls/ServicePassageFrames";
        if(name.StartsWith("Ceiling_")||name=="Service ceiling")return "03_Ceilings";
        if(name=="Walnut wainscot")return "04_WallDetails/Wainscoting";
        if(name=="Brass chair rail")return "04_WallDetails/BrassRails";
        if(name=="Ceiling fixture")return "05_Lighting/Fixtures";
        if(child.GetComponent<Light>()!=null)return "05_Lighting/PointLights";
        foreach(string room in new[]{"301","302","303"})if(name.StartsWith(room+" "))return "06_Furniture/Room"+room;
        if(name=="Lounge sofa"||name=="Sofa back")return "06_Furniture/Lounge";
        return "07_Other";
    }
}
