using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class mission3Build_Editor_ShengChiehLee
{
    public const string ScenePath = mission3Paths_Editor_ShengChiehLee.Scene;
    static bool[,] cells;
    static Transform environment, props;
    static Material wall, wood, carpet, gold, cream, ink, green, sharedCeiling;
    static mission3Hotel_Manager_ShengChiehLee manager;
    static readonly Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    static Vector3 W(float x,float y,float height=0) => new Vector3(x*3,height,-y*3);
    static void Room(int x0,int y0,int x1,int y1) { for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)cells[x,y]=true; }
    static void Layout()
    {
        // Mission1-inspired outer circuit and staggered inner branches, with distinct hotel rooms.
        cells=new bool[21,21];Room(1,1,5,5);Room(3,5,3,18);Room(3,7,18,7);Room(18,4,18,18);Room(3,18,18,18);
        Room(9,4,9,13);Room(5,4,9,4);Room(9,12,18,12);Room(3,13,9,13);Room(14,12,14,18);
        Room(1,9,2,11);Room(5,10,7,12);Room(5,14,7,16);Room(10,3,12,5);Room(14,3,16,5);Room(17,4,18,4);
        Room(18,10,19,12);Room(11,14,13,16);Room(14,15,14,15);Room(4,11,4,11);Room(4,15,4,15);Room(11,6,11,7);
        cells[10,4]=false;
    }
    static Material Mat(string name, Color color, string source=null)
    {
        string path=mission3Paths_Editor_ShengChiehLee.Material(name);
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        bool fresh=m==null;
        if(fresh) { var shared=source==null?null:AssetDatabase.LoadAssetAtPath<Material>(source); m=shared!=null?new Material(shared):new Material(Shader.Find("Universal Render Pipeline/Lit")); }
        m.name=name;m.color=color;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);
        if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.25f);
        // The shared shader graphs expose their own palette fields; tint only our copies.
        if(m.HasProperty("_CarpetColorA")){m.SetColor("_CarpetColorA",new Color(.32f,.035f,.065f));m.SetColor("_CarpetColorB",new Color(.19f,.018f,.035f));}
        if(m.HasProperty("_ColorA")){m.SetColor("_ColorA",new Color(.67f,.55f,.39f));m.SetColor("_ColorB",new Color(.56f,.44f,.29f));}
        if(fresh)AssetDatabase.CreateAsset(m,path);else EditorUtility.SetDirty(m);return m;
    }
    static GameObject Box(string name,Vector3 pos,Vector3 size,Material mat,Transform parent=null,bool collide=true)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.position=pos;o.transform.localScale=size;o.transform.SetParent(parent==null?environment:parent,true);o.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collide)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;
    }
    static TextMeshPro Label(string name,string text,Vector3 pos,float size,float yaw=0,Transform parent=null)
    {
        var o=new GameObject(name);o.transform.SetParent(parent==null?props:parent);o.transform.position=pos;o.transform.rotation=Quaternion.Euler(0,yaw,0);
        var t=o.AddComponent<TextMeshPro>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");t.text=text;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.color=new Color(1,.9f,.65f);t.rectTransform.sizeDelta=new Vector2(7,2);return t;
    }
    static mission3Interact_Prop_ShengChiehLee Action(GameObject o,mission3Interact_Prop_ShengChiehLee.Kind kind,string name,int index=0)
    { var a=o.AddComponent<mission3Interact_Prop_ShengChiehLee>();a.kind=kind;a.displayName=name;a.index=index;return a; }
    static void Lamp(float x,float y,bool shadow=false)
    {
        Box("Ceiling fixture",W(x,y,3.48f),new Vector3(.8f,.12f,.8f),gold);
        var light=new GameObject("Warm hotel light").AddComponent<Light>();light.transform.SetParent(environment);light.transform.position=W(x,y,3.15f);light.type=LightType.Point;light.color=new Color(1,.77f,.48f);light.intensity=2.5f;light.range=10;light.shadows=shadow?LightShadows.Soft:LightShadows.None;
    }
    static void Bed(float x,float y,string room)
    {
        Box(room+" Bed frame",W(x,y,.25f),new Vector3(2,.45f,2.7f),wood);
        Box(room+" Mattress",W(x,y,.58f),new Vector3(1.9f,.25f,2.6f),cream);
        Box(room+" Cover",W(x,y+.15f,.74f),new Vector3(1.92f,.08f,1.7f),carpet);
        Box(room+" Pillow",W(x,y-.28f,.82f),new Vector3(1.4f,.17f,.55f),cream);
    }
    static void Note(float x,float y,int guest)
    {
        Vector3 p=W(x,y,1.3f);Box("Guest note table",p-Vector3.up*.32f,new Vector3(1,.12f,.75f),wood,props);
        var note=Box("Guest note "+(301+guest),p,new Vector3(.8f,.6f,.07f),cream,props);
        Action(note,mission3Interact_Prop_ShengChiehLee.Kind.GuestNote,"Read guest note "+(301+guest),guest);
        Label("Guest note text",(301+guest)+"\nGUEST NOTE",p+Vector3.back*.05f,2.1f,0,note.transform);
    }
    static void Item(float x,float y,int id)
    {
        Vector3 p=W(x,y,.95f);Box("Lost property display",p-Vector3.up*.65f,new Vector3(.9f,.55f,.9f),wood,props);
        var root=new GameObject("LostProperty_"+id+"_"+mission3Rules_Manager_ShengChiehLee.Items[id]);root.transform.SetParent(props);root.transform.position=p;
        var collider=root.AddComponent<BoxCollider>();collider.size=new Vector3(.9f,.8f,.9f);
        Action(root,mission3Interact_Prop_ShengChiehLee.Kind.LostProperty,mission3Rules_Manager_ShengChiehLee.Items[id],id);
        if(id==0 || id==3)
        {
            var disk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);disk.name=id==0?"Compass":"Vinyl";disk.transform.SetParent(root.transform);disk.transform.localPosition=Vector3.zero;disk.transform.localScale=new Vector3(.65f,.04f,.65f);disk.GetComponent<Renderer>().sharedMaterial=id==0?gold:ink;UnityEngine.Object.DestroyImmediate(disk.GetComponent<Collider>());
            Box("Center label",p+Vector3.up*.06f,new Vector3(.14f,.04f,.14f),id==0?ink:carpet,root.transform,false);
            if(id==0)Box("Compass needle",p+Vector3.up*.09f,new Vector3(.04f,.03f,.4f),carpet,root.transform,false);
        }
        else if(id==2)
        {
            Box("Metronome body",p,new Vector3(.4f,.65f,.3f),wood,root.transform,false);Box("Brass pendulum",p+new Vector3(0,.05f,-.17f),new Vector3(.025f,.5f,.025f),gold,root.transform,false);
        }
        else
        {
            Box("Paper",p,new Vector3(.6f,.05f,.42f),cream,root.transform,false);
            Box(id==1?"Blue coastline":"Botanical motif",p+Vector3.up*.04f,new Vector3(.45f,.02f,.24f),id==1?ink:green,root.transform,false);
        }
        Label("Property label",mission3Rules_Manager_ShengChiehLee.Items[id],p+new Vector3(0,.55f,0),1.3f,0,root.transform);
    }
    static Texture2D MapTexture()
    {
        var t=new Texture2D(504,504,TextureFormat.RGBA32,false);var colors=new Color[504*504];
        for(int y=0;y<504;y++)for(int x=0;x<504;x++){int cx=x/24,cy=20-y/24;colors[y*504+x]=cells[cx,cy]?new Color(.86f,.75f,.52f):new Color(.18f,.13f,.11f);}
        void Mark(int cx,int cy,Color c){for(int y=0;y<14;y++)for(int x=0;x<14;x++)colors[((20-cy)*24+5+y)*504+cx*24+5+x]=c;}
        Mark(3,1,Color.red);Mark(5,2,Color.cyan);Mark(10,4,Color.green);Mark(2,10,new Color(.95f,.5f,.2f));Mark(11,4,new Color(.95f,.5f,.2f));Mark(18,11,new Color(.95f,.5f,.2f));
        t.SetPixels(colors);t.Apply();string p=mission3Paths_Editor_ShengChiehLee.Maps+"/mission3FloorMap.png";File.WriteAllBytes(p,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(p);
        var imp=(TextureImporter)AssetImporter.GetAtPath(p);imp.textureType=TextureImporterType.Default;imp.sRGBTexture=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.filterMode=FilterMode.Point;imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }
    [MenuItem("Mission3/Build Hotel 24 Scene (replaces Mission3 only)")]
    public static void Build()
    {
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        mission3Paths_Editor_ShengChiehLee.EnsureFolders();AssetDatabase.Refresh();Layout();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        environment=new GameObject("Mission3_HotelArchitecture").transform;props=new GameObject("Mission3_PropsAndInteractions").transform;
        const string common="Assets/Materials/Materials/Environment/";
        wall=Mat("mission3_Wallpaper",new Color(.59f,.46f,.31f),common+"M_Wall.mat");wood=Mat("mission3_Walnut",new Color(.19f,.105f,.065f));carpet=Mat("mission3_BurgundyCarpet",new Color(.29f,.035f,.055f),common+"M_Floor.mat");gold=Mat("mission3_Brass",new Color(.7f,.48f,.19f));cream=Mat("mission3_Linen",new Color(.83f,.76f,.61f));ink=Mat("mission3_Ink",new Color(.035f,.1f,.15f));green=Mat("mission3_Leaf",new Color(.12f,.31f,.16f));sharedCeiling=AssetDatabase.LoadAssetAtPath<Material>(common+"M_Ceiling.mat");
        for(int y=0;y<21;y++)for(int x=0;x<21;x++)if(cells[x,y])
        {
            Box("Floor_"+x+"_"+y,W(x,y,-.15f),new Vector3(3,.3f,3),x<6&&y<6?cream:carpet);
            Box("Ceiling_"+x+"_"+y,W(x,y,3.7f),new Vector3(3,.2f,3),sharedCeiling);
            foreach(var d in dirs)
            {
                int a=x+d.x,b=y+d.y;
                if(a>=0&&b>=0&&a<21&&b<21&&cells[a,b])continue;
                if(a==10&&b==4 && d.x!=0)continue;
                Vector3 pos=W(x+d.x*.5f,y+d.y*.5f,1.8f);Vector3 size=d.x!=0?new Vector3(.16f,3.6f,3):new Vector3(3,3.6f,.16f);
                Box("Wall_"+x+"_"+y+"_"+d,pos,size,wall);
                Box("Walnut wainscot",new Vector3(pos.x,.48f,pos.z),new Vector3(size.x+.025f,.9f,size.z+.025f),wood);
                Box("Brass chair rail",new Vector3(pos.x,.96f,pos.z),new Vector3(size.x+.04f,.04f,size.z+.04f),gold,null,false);
            }
            if((x+y)%5==0)Lamp(x,y);
        }
        // Closed service cell is a real walkable cell once its barrier is opened.
        Box("Service floor",W(10,4,-.15f),new Vector3(3,.3f,3),carpet);
        Box("Service ceiling",W(10,4,3.7f),new Vector3(3,.2f,3),sharedCeiling);
        Box("Service north jamb",W(10,3.5f,1.8f),new Vector3(3,3.6f,.16f),wood);
        Box("Service south jamb",W(10,4.5f,1.8f),new Vector3(3,3.6f,.16f),wood);
        Lamp(3,3,true);Lamp(6,11);Lamp(11,4);Lamp(18,11);Lamp(12,15);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.39f,.33f,.29f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.09f,.065f,.05f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.012f;
        var player=new GameObject("Player_Mission3");player.tag="Player";player.transform.position=W(3,4,1.05f);
        var rb=player.AddComponent<Rigidbody>();rb.constraints=RigidbodyConstraints.FreezeRotation;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.Continuous;
        var capsule=player.AddComponent<CapsuleCollider>();capsule.height=1.8f;capsule.radius=.3f;
        var cam=new GameObject("Main Camera_Mission3").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.SetParent(player.transform,false);cam.transform.localPosition=Vector3.up*.62f;cam.fieldOfView=72;cam.nearClipPlane=.05f;cam.farClipPlane=180;cam.gameObject.AddComponent<AudioListener>();var cameraData=cam.gameObject.AddComponent<UniversalAdditionalCameraData>();cameraData.renderPostProcessing=true;cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;cameraData.antialiasingQuality=AntialiasingQuality.High;
        var moving=player.AddComponent<Moving>();moving.walkSpeed=4;moving.runSpeed=6;moving.cameraTransform=cam.transform;
        var look=cam.gameObject.AddComponent<MouseLook>();look.player=player.transform;
        manager=new GameObject("Mission3_Manager_ShengChiehLee").AddComponent<mission3Hotel_Manager_ShengChiehLee>();manager.playerCamera=cam;manager.movement=moving;manager.mouseLook=look;manager.playerBody=rb;manager.floorMap=MapTexture();
        // Entry map is physical, directly ahead of the spawn, with an inspectable copy in the HUD.
        var mapMat=Mat("mission3_MapDisplay",Color.white);mapMat.shader=Shader.Find("Universal Render Pipeline/Unlit");mapMat.SetTexture("_BaseMap",manager.floorMap);EditorUtility.SetDirty(mapMat);
        Box("Map frame",new Vector3(9,1.85f,-1.64f),new Vector3(3.05f,3.05f,.14f),gold,props);
        var board=GameObject.CreatePrimitive(PrimitiveType.Quad);board.name="Entrance wall floor map";board.transform.SetParent(props);board.transform.position=new Vector3(9,1.85f,-1.73f);board.transform.localScale=new Vector3(2.9f,2.9f,1);board.GetComponent<Renderer>().sharedMaterial=mapMat;Action(board,mission3Interact_Prop_ShengChiehLee.Kind.FloorMap,"Inspect floor map");
        Label("Map header","HOTEL 24 / FLOOR 3",new Vector3(9,3.45f,-1.8f),2.3f);
        Label("Map legend","YOU ARE HERE\n301  WEST WING\n302  NORTH WING\n303  EAST WING\nE : inspect    M : map",new Vector3(13,2,-1.8f),1.7f);
        Box("Reception desk",W(2,2,.55f),new Vector3(3.2f,1.1f,1.3f),wood,props);
        var checkout=Box("Checkout terminal",W(2,2,1.35f),new Vector3(1,.5f,.25f),ink,props);Action(checkout,mission3Interact_Prop_ShengChiehLee.Kind.Checkout,"Checkout / return belongings");Label("Checkout label","CHECKOUT\nLOST & FOUND",W(2,2.06f,1.4f),1.8f);
        mission3Lift_Editor_ShengChiehLee.Create(props);
        manager.serviceDoor=Box("Service door shortcut",W(10,4,1.7f),new Vector3(.2f,3.4f,2.95f),wood,props);
        var latch=Box("Service door latch",W(10.2f,4,1.5f),new Vector3(.3f,.5f,.5f),gold,props);Action(latch,mission3Interact_Prop_ShengChiehLee.Kind.ServiceDoor,"Open service shortcut");
        Label("Service sign","SERVICE PASSAGE\nOPEN FROM THIS SIDE",W(10.3f,4,2.6f),1.6f,90);
        Bed(1.5f,9.3f,"301");Bed(11.6f,3.3f,"302");Bed(18.7f,10.2f,"303");
        Note(2,10.8f,0);Note(11,5,1);Note(18,12,2);
        Label("301 sign","301 / TRAVELLER",W(2,11.4f,2.8f),2);
        Label("302 sign","302 / MUSICIAN",W(11,5.4f,2.8f),2);
        Label("303 sign","303 / BOTANIST",W(18,12.4f,2.8f),2);
        Label("Lounge sign","THE MIDNIGHT LOUNGE",W(12,16.3f,2.8f),2.2f);
        Box("Lounge sofa",W(12,14.4f,.5f),new Vector3(3.5f,.65f,1.1f),carpet,props);Box("Sofa back",W(12,14.25f,1),new Vector3(3.5f,.7f,.25f),wood,props);
        // Deliberately mixed locations: infer owners from notes rather than room of discovery.
        Item(6,11,0);Item(18,11,1);Item(1.2f,11,2);Item(12,15.5f,3);Item(11,4,4);Item(6,15,5);
        mission3Presentation_Editor_ShengChiehLee.BuildPresentation(scene);
        mission3Architecture_Editor_ShengChiehLee.Organize(scene);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);Validate();Debug.Log("MISSION3_BUILD_SUCCESS "+ScenePath);
    }
    public static void BuildAndCapture() { Build(); mission3Verify_Editor_ShengChiehLee.Capture(); }
    [MenuItem("Mission3/Validate Hotel Scene")]
    public static void Validate()
    {
        var m=UnityEngine.Object.FindFirstObjectByType<mission3Hotel_Manager_ShengChiehLee>();if(m==null)throw new Exception("No Mission3 manager");
        if(m.playerCamera==null||m.movement==null||m.mouseLook==null||m.floorMap==null)throw new Exception("Missing required references");
        var objects=UnityEngine.Object.FindObjectsByType<mission3Interact_Prop_ShengChiehLee>(FindObjectsSortMode.None);
        if(objects.Count(o=>o.kind==mission3Interact_Prop_ShengChiehLee.Kind.LostProperty)!=6)throw new Exception("Expected six belongings");
        if(objects.Count(o=>o.kind==mission3Interact_Prop_ShengChiehLee.Kind.GuestNote)!=3)throw new Exception("Expected three notes");
        var ids=objects.Where(o=>o.kind==mission3Interact_Prop_ShengChiehLee.Kind.LostProperty).Select(o=>o.index).Distinct();if(ids.Count()!=6)throw new Exception("Duplicate item ids");
        Layout();var seen=new HashSet<Vector2Int>();var queue=new Queue<Vector2Int>();queue.Enqueue(new Vector2Int(3,4));seen.Add(new Vector2Int(3,4));
        while(queue.Count>0){var p=queue.Dequeue();foreach(var d in dirs){var n=p+d;if(n.x<0||n.y<0||n.x>=21||n.y>=21||!cells[n.x,n.y]||!seen.Add(n))continue;queue.Enqueue(n);}}
        int count=0;foreach(bool c in cells)if(c)count++;if(seen.Count!=count)throw new Exception("Disconnected hotel map");
        bool[] all=Enumerable.Repeat(true,6).ToArray();int[] correct={0,0,1,1,2,2};var r=mission3Rules_Manager_ShengChiehLee.Settle(all,correct,true);if(r.score!=100||r.correct!=6||r.sets!=3)throw new Exception("Perfect score failed");
        var wrong=mission3Rules_Manager_ShengChiehLee.Settle(all,new[]{1,1,2,2,0,0},false);if(wrong.score!=0)throw new Exception("Wrong assignments scored");
        var partial=mission3Rules_Manager_ShengChiehLee.Settle(new[]{true,true,true,false,false,false},correct,false);if(partial.score!=40)throw new Exception("Partial score failed");
        foreach(var go in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script on "+t.name);
        Debug.Log("MISSION3_VALIDATION_PASS: references, 6 unique items, 3 notes, connected map, scoring and missing scripts");
    }
}
