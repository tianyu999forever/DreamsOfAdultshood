using UnityEditor;

// Keep all generated Mission3 assets in the same type-based folders used by Mission1.
public static class mission3Paths_Editor_ShengChiehLee
{
    public const string Scene = "Assets/Scenes/Mission3.unity";
    public const string Backups = "Assets/Scenes/Backups/Mission3";
    public const string Scripts = "Assets/Scripts/Mission3";
    public const string Materials = "Assets/Materials/Mission3";
    public const string Environment = Materials + "/Environment";
    public const string InterfaceMaterials = Materials + "/UI";
    public const string Textures = "Assets/Textures/Mission3";
    public const string Maps = Textures + "/Maps";
    public const string InterfaceTextures = Textures + "/UI";
    public const string Documentation = "Assets/Documentation/Mission3";
    public static string Material(string name) => (name == "mission3_MapDisplay" ? InterfaceMaterials : Environment) + "/" + name + ".mat";
    public static void EnsureFolders()
    {
        foreach(var path in new[]{Backups, Environment, InterfaceMaterials, Maps, InterfaceTextures, Documentation,
            Scripts+"/Gameplay",Scripts+"/Player",Scripts+"/UI",Scripts+"/Interactions",Scripts+"/Editor"}) Ensure(path);
    }
    static void Ensure(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        int slash=path.LastIndexOf('/');string parent=path.Substring(0,slash);Ensure(parent);AssetDatabase.CreateFolder(parent,path.Substring(slash+1));
    }
}
