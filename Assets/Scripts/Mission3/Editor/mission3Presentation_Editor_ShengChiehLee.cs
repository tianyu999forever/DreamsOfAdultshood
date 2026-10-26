using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class mission3Presentation_Editor_ShengChiehLee
{
    const string ScenePath="Assets/Scenes/Mission3.unity";
    const string Output="C:/Users/j1a2m/Documents/Codex/2026-10-25/w/outputs/mission3-preview";
    static readonly Color Ink=new Color(.055f,.10f,.12f), Paper=new Color(.95f,.92f,.84f), Gold=new Color(.79f,.60f,.29f), Cyan=new Color(.025f,.70f,.85f);
    static TMP_FontAsset font;
    static Sprite round;
    static mission3Canvas_UI_ShengChiehLee ui;

    [MenuItem("Mission3/Apply Mission1 Player, Canvas and Maze Upgrade")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before applying Mission3 changes.");
        for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the open scenes before applying the upgrade.");
        mission3Build_Editor_ShengChiehLee.Build();
        FinalizeUpgrade();
    }
    public static void FinalizeUpgrade()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play mode before finalizing the scene.");
        var manager=Find<mission3Hotel_Manager_ShengChiehLee>();if(manager==null||manager.canvasUI==null)throw new Exception("Generate the upgraded Mission3 scene first.");
        var build=EditorBuildSettings.scenes.ToList();
        // Replace only the duplicate Mission3 entry, retaining the team's other scene order.
        for(int i=0;i<build.Count;i++) if(build[i].path=="Assets/Mission 3.unity") build[i]=new EditorBuildSettingsScene(ScenePath,build[i].enabled);
        if(!build.Any(x=>x.path==ScenePath)) build.Add(new EditorBuildSettingsScene(ScenePath,true));
        var seen=new HashSet<string>(); EditorBuildSettings.scenes=build.Where(x=>seen.Add(x.path)).ToArray();
        // Compact curved grooves replace hundreds of individual decorative blocks.
        var vinyl=manager.canvasUI.itemPositions[3];
        foreach(var child in vinyl.Cast<Transform>().Where(x=>x.name=="Record groove"||x.name=="Record grooves").ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        VinylGrooves(vinyl,AssetDatabase.LoadAssetAtPath<Material>(mission3Paths_Editor_ShengChiehLee.Material("mission3_Brass")));
        // Improve physical-map annotations and keep UI panels opaque against scene lighting.
        foreach(var p in manager.canvasUI.pages.Where(x=>x!=null))p.GetComponent<Image>().color=new Color(.055f,.10f,.12f,1);
        manager.canvasUI.Refresh();
        var legend=manager.gameObject.scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).FirstOrDefault(x=>x.name=="Map legend");
        if(legend!=null){legend.position=new Vector3(5.5f,2.15f,-1.8f);var text=legend.GetComponent<TMP_Text>();text.text="FLOOR 03\nCYAN 1-6 : BELONGINGS\nAMBER : GUEST NOTES\nE : INSPECT    M : MAP";text.fontSize=1.15f;text.rectTransform.sizeDelta=new Vector2(3.2f,1.6f);}
        var wayfinding=manager.gameObject.scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).FirstOrDefault(x=>x.name=="Mission3_Wayfinding");
        if(wayfinding!=null)
        {
            foreach(var back in wayfinding.Cast<Transform>().Where(x=>x.name=="Direction text reverse").ToArray())UnityEngine.Object.DestroyImmediate(back.gameObject);
            foreach(var front in wayfinding.GetComponentsInChildren<TextMeshPro>(true).Where(x=>x.name=="Direction text").ToArray())
            {
                front.text=front.text.Replace("<","").Replace(">","");
                var reverse=UnityEngine.Object.Instantiate(front.gameObject,wayfinding);reverse.name="Direction text reverse";reverse.transform.position=front.transform.position+Vector3.forward*.11f;reverse.transform.rotation=Quaternion.Euler(0,180,0);
            }
        }
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(manager.gameObject.scene);
        Validate();
        Selection.activeGameObject=Find<mission3Canvas_UI_ShengChiehLee>().gameObject;
        EditorGUIUtility.PingObject(Selection.activeGameObject);
        mission3Session_Editor_ShengChiehLee.Write("APPLY_PASS\nCanonical scene: "+ScenePath+"\nMission1 player and Canvas cloned; six item markers; three note markers; hidden passage excluded; new maze and Build Settings saved.");
    }
    static T Find<T>() where T:Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<T>(true)).FirstOrDefault();

    public static void BuildPresentation(Scene scene)
    {
        mission3Paths_Editor_ShengChiehLee.EnsureFolders();
        SceneManager.SetActiveScene(scene);
        var manager=Find<mission3Hotel_Manager_ShengChiehLee>();
        var source=SceneManager.GetSceneByPath("Assets/Scenes/Mission1.unity");
        bool opened=!source.IsValid()||!source.isLoaded;
        if(opened) source=EditorSceneManager.OpenScene("Assets/Scenes/Mission1.unity",OpenSceneMode.Additive);
        try
        {
            var originalPlayer=source.GetRootGameObjects().First(x=>x.name=="Player");
            var originalCanvas=source.GetRootGameObjects().First(x=>x.name=="Canvas");
            var oldPlayer=manager.playerBody.gameObject; Vector3 spawn=oldPlayer.transform.position;
            var player=UnityEngine.Object.Instantiate(originalPlayer);player.name="Player";SceneManager.MoveGameObjectToScene(player,scene);
            player.transform.position=new Vector3(spawn.x,1.4f,spawn.z);player.transform.rotation=Quaternion.identity;
            manager.playerBody=player.GetComponent<Rigidbody>();manager.movement=player.GetComponent<Moving>();
            manager.playerCamera=player.GetComponentInChildren<Camera>(true);manager.mouseLook=player.GetComponentInChildren<MouseLook>(true);manager.headBob=player.GetComponentInChildren<HeadBob>(true);
            if(manager.mouseLook==null) throw new Exception("Mission1 MouseLook did not resolve.");
            manager.playerBody.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            // Preserve Mission1's speeds, collider, head bob, camera offset, and complete hand/watch model.
            manager.playerCamera.clearFlags=CameraClearFlags.SolidColor;manager.playerCamera.backgroundColor=Ink;
            manager.playerCamera.nearClipPlane=.04f;manager.playerCamera.farClipPlane=180;
            var data=manager.playerCamera.GetComponent<UniversalAdditionalCameraData>();if(data!=null) data.renderPostProcessing=true;
            var oldWatch=player.GetComponentInChildren<PocketWatch>(true);
            if(oldWatch!=null)
            {
                var watch=oldWatch.gameObject.AddComponent<mission3Watch_Player_ShengChiehLee>();watch.manager=manager;watch.visuals=oldWatch.watchVisuals;watch.timer=oldWatch.timerText;
                watch.hiddenPosition=oldWatch.hiddenPosition;watch.shownPosition=oldWatch.shownPosition;watch.hiddenRotation=oldWatch.hiddenRotation;watch.shownRotation=oldWatch.shownRotation;
                UnityEngine.Object.DestroyImmediate(oldWatch);
            }
            UnityEngine.Object.DestroyImmediate(oldPlayer);
            var canvasObject=UnityEngine.Object.Instantiate(originalCanvas);canvasObject.name="Canvas";SceneManager.MoveGameObjectToScene(canvasObject,scene);
            foreach(var behaviour in canvasObject.GetComponentsInChildren<MonoBehaviour>(true)) if(behaviour!=null && behaviour.GetType().Assembly==typeof(Moving).Assembly) UnityEngine.Object.DestroyImmediate(behaviour);
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;canvas.sortingOrder=20;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            round=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            ui=canvasObject.AddComponent<mission3Canvas_UI_ShengChiehLee>();ui.manager=manager;manager.canvasUI=ui;
            MakeMap(manager);
            BuildCanvas(canvasObject.transform);
            foreach(var c in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Canvas>(true))) if(c.renderMode==RenderMode.WorldSpace)c.worldCamera=manager.playerCamera;
            var events=Find<EventSystem>();if(events==null)new GameObject("Mission3 EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            PolishEnvironment(scene);
            EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(scene);
        }
        finally { if(opened)EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(scene); }
    }

    static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
        var r=(RectTransform)go.transform;r.anchoredPosition=position;r.sizeDelta=size;return r;
    }
    static Image Image(Transform parent,string name,Vector2 position,Vector2 size,Color color)
    {
        var r=Rect(parent,name,position,size);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
    }
    static TMP_Text Text(Transform parent,string name,string value,Vector2 pos,Vector2 size,float fontSize,Color color,TextAlignmentOptions alignment=TextAlignmentOptions.TopLeft)
    {
        var r=Rect(parent,name,pos,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=fontSize;t.color=color;t.alignment=alignment;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
    }
    static Button Button(Transform parent,string name,string value,Vector2 position,Vector2 size,UnityAction action,bool accent=false)
    {
        var image=Image(parent,name,position,size,accent?Cyan:new Color(.16f,.20f,.22f));image.sprite=round;image.type=UnityEngine.UI.Image.Type.Sliced;image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};
        var colors=button.colors;colors.highlightedColor=new Color(1,.87f,.62f);colors.pressedColor=new Color(.7f,.75f,.76f);button.colors=colors;
        Text(button.transform,"Text (TMP)",value,Vector2.zero,size-new Vector2(18,6),21,Paper,TextAlignmentOptions.Center);
        if(action!=null)UnityEventTools.AddPersistentListener(button.onClick,action);return button;
    }
    static GameObject Panel(Transform parent,string name,string eyebrow,string heading)
    {
        var r=Rect(parent,name,Vector2.zero,new Vector2(1660,860));
        var image=r.gameObject.AddComponent<Image>();image.color=new Color(.055f,.10f,.12f,.985f);image.raycastTarget=true;
        Image(r,"Gold rule",new Vector2(-826,0),new Vector2(5,860),Gold);
        Text(r,"Section",eyebrow,new Vector2(0,360),new Vector2(1510,35),17,Gold);
        Text(r,"Title",heading,new Vector2(0,301),new Vector2(1510,70),42,Paper);
        Image(r,"Divider",new Vector2(0,246),new Vector2(1510,1),new Color(.36f,.39f,.36f));
        return r.gameObject;
    }
    static void Back(Transform parent,string label="BACK TO HOTEL") { Button(parent,"BackButton",label,new Vector2(-605,-358),new Vector2(300,54),ui.Begin); }
    static void BuildCanvas(Transform root)
    {
        // Retain Mission1's actual InventoryPanel and WatchHintText; replace its arithmetic/death content.
        var inventory=root.Find("InventoryPanel");var hint=root.Find("WatchHintText");
        var children=root.Cast<Transform>().ToArray();foreach(var child in children)if(child!=inventory && child!=hint)UnityEngine.Object.DestroyImmediate(child.gameObject);
        if(inventory==null)inventory=Rect(root,"InventoryPanel",Vector2.zero,Vector2.one);
        inventory.gameObject.SetActive(true);
        foreach(Transform child in inventory.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        var inv=(RectTransform)inventory;inv.anchorMin=inv.anchorMax=new Vector2(.5f,0);inv.pivot=new Vector2(.5f,0);inv.anchoredPosition=new Vector2(0,60);inv.sizeDelta=new Vector2(1440,66);
        var invImage=inventory.GetComponent<Image>();if(invImage!=null)invImage.color=new Color(.03f,.07f,.09f,.83f);
        ui.bag=Text(inventory,"InventoryText","BAG",Vector2.zero,new Vector2(1380,58),18,Paper,TextAlignmentOptions.Center);
        if(hint==null)hint=Rect(root,"WatchHintText",Vector2.zero,new Vector2(1200,40));hint.gameObject.SetActive(true);
        var hintText=hint.GetComponent<TMP_Text>();if(hintText==null)hintText=hint.gameObject.AddComponent<TextMeshProUGUI>();hintText.font=font;hintText.fontSize=16;hintText.color=new Color(.8f,.82f,.77f);hintText.alignment=TextAlignmentOptions.Center;hintText.raycastTarget=false;
        var hr=(RectTransform)hint;hr.anchorMin=hr.anchorMax=new Vector2(.5f,0);hr.anchoredPosition=new Vector2(0,27);hr.sizeDelta=new Vector2(1680,30);hintText.text="WASD  MOVE     SHIFT  RUN     E  INTERACT     Q  WATCH     M  MAP     TAB  JOURNAL     ESC  PAUSE";
        var header=Image(root,"HUD header",new Vector2(0,465),new Vector2(1800,95),new Color(.03f,.07f,.09f,.86f));
        Text(header.transform,"Mission title","THE LAST CHECKOUT",new Vector2(-270,13),new Vector2(1190,40),28,Paper);
        ui.status=Text(header.transform,"Progress","",new Vector2(-270,-22),new Vector2(1190,24),16,Gold);
        ui.clock=Text(header.transform,"Elapsed time","00:00",new Vector2(790,10),new Vector2(140,40),28,Paper,TextAlignmentOptions.Center);
        Text(header.transform,"Clock label","NIGHT SHIFT",new Vector2(790,-23),new Vector2(160,22),13,Gold,TextAlignmentOptions.Center);
        Text(root,"Crosshair","+",Vector2.zero,new Vector2(30,30),25,Paper,TextAlignmentOptions.Center);
        ui.prompt=Text(root,"InteractionPrompt","",new Vector2(0,-110),new Vector2(1200,50),23,Paper,TextAlignmentOptions.Center);
        ui.notice=Text(root,"Notification","",new Vector2(0,385),new Vector2(1500,42),22,Gold,TextAlignmentOptions.Center);
        var shade=Image(root,"Modal backdrop",Vector2.zero,new Vector2(3000,2200),new Color(0,0,0,.66f));shade.raycastTarget=true;ui.backdrop=shade.gameObject;
        ui.pages=new GameObject[9];
        var welcome=Panel(root,"WelcomePanel","MISSION 03  /  HOTEL 24","Some guests left more than their luggage.");ui.pages[1]=welcome;
        Text(welcome.transform,"Brief","The night shift is yours. Explore the hotel, read the three guest notes, and bring the lost belongings back to reception.\n\nEach item has an owner. Where you find it is only part of the story.",new Vector2(-330,97),new Vector2(840,240),27,Paper);
        Text(welcome.transform,"Score guide","THE SHIFT AT A GLANCE\n\n06   LOST BELONGINGS\n03   GUEST NOTES\n100   POINTS POSSIBLE\n\nReturn at least three items to check out.\nNo countdown. Take time to explore.",new Vector2(505,49),new Vector2(430,330),23,Gold);
        Image(welcome.transform,"Brief separator",new Vector2(210,35),new Vector2(1,350),new Color(.36f,.39f,.36f));
        Text(welcome.transform,"Rewards","CORRECT ITEM  +10     /     COMPLETE PAIR  +10     /     EXPLORATION BONUS  +10",new Vector2(-280,-205),new Vector2(950,65),18,Gold);
        Button(welcome.transform,"BeginButton","BEGIN THE NIGHT SHIFT",new Vector2(-525,-345),new Vector2(460,62),ui.Begin,true);
        Button(welcome.transform,"MapButton","VIEW FLOOR PLAN",new Vector2(5,-345),new Vector2(420,62),ui.Map);
        BuildMap(root);
        var journal=Panel(root,"JournalPanel","THE NIGHT SHIFT  /  CLUES","Guest journal");ui.pages[3]=journal;
        ui.journal=Text(journal.transform,"GuestNotes","",new Vector2(-10,-15),new Vector2(1450,440),25,Paper);Back(journal.transform);
        var note=Panel(root,"NotePanel","HOTEL 24  /  CORRESPONDENCE","A note left behind");ui.pages[4]=note;
        ui.note=Text(note.transform,"GuestLetter","",new Vector2(-125,15),new Vector2(1200,340),32,Paper);
        Text(note.transform,"Saved hint","Saved to your guest journal. TAB opens it during exploration.",new Vector2(0,-240),new Vector2(1510,40),19,Gold);Back(note.transform);
        var checkout=Panel(root,"ExitPanel","RECEPTION  /  FINAL CHECKOUT","Every belonging has a story.");ui.pages[5]=checkout;
        ui.summary=Text(checkout.transform,"CheckoutSummary","",new Vector2(0,189),new Vector2(1510,85),20,Paper);
        Text(checkout.transform,"Owners","301  TRAVELLER                302  MUSICIAN                303  BOTANIST",new Vector2(320,111),new Vector2(850,32),17,Gold,TextAlignmentOptions.Center);
        ui.ownerButtons=new Button[18];ui.itemRows=new TMP_Text[6];
        for(int i=0;i<6;i++)
        {
            float y=50-i*57;Image(checkout.transform,"Item rule "+i,new Vector2(0,y-27),new Vector2(1510,1),new Color(.2f,.26f,.27f));
            ui.itemRows[i]=Text(checkout.transform,"Item "+i,"",new Vector2(-430,y),new Vector2(650,38),20,Paper);
            for(int g=0;g<3;g++)ui.ownerButtons[i*3+g]=Button(checkout.transform,"Owner_"+i+"_"+g,(301+g).ToString(),new Vector2(70+g*280,y),new Vector2(240,42),null);
        }
        Back(checkout.transform);ui.reviewButton=Button(checkout.transform,"ReviewButton","REVIEW CHECKOUT",new Vector2(535,-358),new Vector2(440,54),ui.Review,true);
        var confirm=Panel(root,"ConfirmPanel","RECEPTION  /  CONFIRM","Ready to leave the hotel?");ui.pages[6]=confirm;
        Text(confirm.transform,"Confirmation","Finalizing locks your assignments and ends this run. Missing belongings and incorrect owners receive no return points.\n\nYou can still return to the desk to change your choices.",new Vector2(-125,30),new Vector2(1200,280),29,Paper);
        Button(confirm.transform,"EditButton","EDIT ASSIGNMENTS",new Vector2(-495,-345),new Vector2(510,60),ui.Edit);Button(confirm.transform,"FinalizeButton","FINALIZE CHECKOUT",new Vector2(180,-345),new Vector2(650,60),ui.Finish,true);
        var result=Panel(root,"ResultPanel","HOTEL 24  /  SHIFT COMPLETE","The last checkout");ui.pages[7]=result;
        ui.result=Text(result.transform,"ResultText","",new Vector2(0,-25),new Vector2(1510,485),23,Paper);
        Button(result.transform,"PlayAgainButton","EXPLORE AGAIN",new Vector2(-535,-358),new Vector2(440,54),ui.Restart);Button(result.transform,"ReturnButton","RETURN TO LIFT",new Vector2(20,-358),new Vector2(440,54),ui.Begin);Button(result.transform,"ContinueButton","NEXT LEVEL",new Vector2(575,-358),new Vector2(360,54),ui.Continue,true);
        var pause=Panel(root,"PausePanel","THE NIGHT SHIFT  /  PAUSED","Take a moment.");ui.pages[8]=pause;
        Text(pause.transform,"Pause hint","Your shift is waiting. Review the floor plan or your guest notes before heading back.",new Vector2(-75,50),new Vector2(1300,140),29,Paper);
        Button(pause.transform,"ContinueButton","CONTINUE",new Vector2(-525,-120),new Vector2(460,64),ui.Begin,true);Button(pause.transform,"MapButton","FLOOR PLAN",new Vector2(0,-120),new Vector2(460,64),ui.Map);Button(pause.transform,"JournalButton","GUEST JOURNAL",new Vector2(525,-120),new Vector2(460,64),ui.Journal);
        ui.Refresh();
    }

    static void BuildMap(Transform root)
    {
        // A large, square plan plus an independently readable legend. Coordinates match the PNG generator.
        var panel=Panel(root,"MapPanel","HOTEL 24  /  WAYFINDING","Floor 03  /  Lost & found");ui.pages[2]=panel;
        var r=Rect(panel.transform,"FloorPlan",new Vector2(-410,-95),new Vector2(650,650));ui.mapRect=r;
        var raw=r.gameObject.AddComponent<RawImage>();raw.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(mission3Paths_Editor_ShengChiehLee.Maps+"/mission3FloorPlan.png");raw.raycastTarget=false;
        ui.itemMarkers=new Image[6];ui.noteMarkers=new Image[3];ui.itemPositions=new Transform[6];ui.notePositions=new Transform[3];
        var actions=Find<mission3Hotel_Manager_ShengChiehLee>().gameObject.scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Interact_Prop_ShengChiehLee>(true)).ToArray();
        for(int i=0;i<6;i++)
        {
            ui.itemPositions[i]=actions.Single(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.LostProperty&&x.index==i).transform;
            var marker=Image(r,"Belonging "+(i+1),Vector2.zero,new Vector2(27,27),Cyan);marker.sprite=round;marker.type=UnityEngine.UI.Image.Type.Sliced;ui.itemMarkers[i]=marker;
            Text(marker.transform,"Number",(i+1).ToString(),Vector2.zero,new Vector2(27,27),18,Color.white,TextAlignmentOptions.Center);
            Text(panel.transform,"LegendItem "+i,(i+1).ToString("00")+"   "+mission3Rules_Manager_ShengChiehLee.Items[i],new Vector2(360,70-i*42),new Vector2(660,34),22,Paper);
        }
        for(int i=0;i<3;i++)
        {
            ui.notePositions[i]=actions.Single(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.GuestNote&&x.index==i).transform;
            var marker=Image(r,"Guest note "+(301+i),Vector2.zero,new Vector2(18,18),Gold);ui.noteMarkers[i]=marker;
            Text(marker.transform,"Note icon","N",Vector2.zero,new Vector2(18,18),13,Ink,TextAlignmentOptions.Center);
        }
        var arrow=Rect(r,"Player / you are here",Vector2.zero,new Vector2(25,30));ui.playerMarker=arrow;
        var player=arrow.gameObject.AddComponent<RawImage>();player.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(mission3Paths_Editor_ShengChiehLee.InterfaceTextures+"/mission3PlayerArrow.png");player.raycastTarget=false;
        Text(panel.transform,"Legend heading","BELONGINGS",new Vector2(360,152),new Vector2(660,35),18,Gold);
        Text(panel.transform,"Legend key","RED ARROW  You are here    /    CYAN  Lost belonging\nAMBER N  Guest note    /    MUTED +  Collected\n\n301  Traveller / west wing\n302  Musician / north wing\n303  Botanist / east wing\nReception + lift: return belongings. North is up.",new Vector2(360,-265),new Vector2(660,180),17,Paper);
        Button(panel.transform,"MapBackButton","BACK TO HOTEL  /  M",new Vector2(320,-388),new Vector2(560,52),ui.Begin);
    }

    static void MakeMap(mission3Hotel_Manager_ShengChiehLee manager)
    {
        bool[,] cells=new bool[21,21];
        foreach(var t in manager.gameObject.scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)))
        {
            var parts=t.name.Split('_');if(parts.Length==3&&parts[0]=="Floor"&&int.TryParse(parts[1],out int x)&&int.TryParse(parts[2],out int y))cells[x,y]=true;
        }
        var texture=new Texture2D(1024,1024,TextureFormat.RGBA32,false);var pixels=Enumerable.Repeat(new Color(.93f,.91f,.85f),1024*1024).ToArray();
        void Box(int x,int y,int w,int h,Color c){for(int py=Mathf.Max(0,y);py<Mathf.Min(1024,y+h);py++)for(int px=Mathf.Max(0,x);px<Mathf.Min(1024,x+w);px++)pixels[py*1024+px]=c;}
        int Edge(int n)=>Mathf.RoundToInt(72+n*(880f/21));
        Color floor=new Color(.995f,.985f,.95f);
        for(int y=0;y<21;y++)for(int x=0;x<21;x++)if(cells[x,y])Box(Edge(x),Edge(20-y),Edge(x+1)-Edge(x),Edge(21-y)-Edge(20-y),floor);
        for(int y=0;y<21;y++)for(int x=0;x<21;x++)if(cells[x,y])
        {
            int l=Edge(x),r=Edge(x+1),b=Edge(20-y),t=Edge(21-y);
            if(x==0||!cells[x-1,y])Box(l-3,b-3,6,t-b+6,Ink);
            if(x==20||!cells[x+1,y])Box(r-3,b-3,6,t-b+6,Ink);
            if(y==0||!cells[x,y-1])Box(l-3,t-3,r-l+6,6,Ink);
            if(y==20||!cells[x,y+1])Box(l-3,b-3,r-l+6,6,Ink);
        }
        var digits=new[]{"111101101101111","010110010010111","111001111100111","111001111001111","101101111001001","111100111001111","111100111101111","111001001001001","111101111101111","111101111001111"};
        void Number(string n,int x,int y,int scale,Color color){foreach(char ch in n){if(ch>='0'&&ch<='9'){var glyph=digits[ch-'0'];for(int gy=0;gy<5;gy++)for(int gx=0;gx<3;gx++)if(glyph[gy*3+gx]=='1')Box(x+gx*scale,y+(4-gy)*scale,scale,scale,color);}x+=4*scale;}}
        // Room numbers label real rooms; the closed service passage is drawn as ordinary solid wall.
        Number("301",Edge(1)+7,Edge(11)+5,3,Gold);Number("302",Edge(10)+7,Edge(17)+5,3,Gold);Number("303",Edge(18)+2,Edge(10)+4,3,Gold);
        // Discreet north arrow in the paper margin.
        Box(948,975,3,23,Ink);for(int a=0;a<8;a++){Box(949-a,995-a,2,2,Ink);Box(949+a,995-a,2,2,Ink);}
        texture.SetPixels(pixels);texture.Apply();SaveTexture(texture,mission3Paths_Editor_ShengChiehLee.Maps+"/mission3FloorPlan.png");
        var actions=manager.gameObject.scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Interact_Prop_ShengChiehLee>(true)).ToArray();
        foreach(var p in actions.Where(a=>a.kind==mission3Interact_Prop_ShengChiehLee.Kind.LostProperty||a.kind==mission3Interact_Prop_ShengChiehLee.Kind.GuestNote))
        {
            int px=Mathf.RoundToInt(72+(p.transform.position.x+1.5f)/63*880),py=Mathf.RoundToInt(72+(p.transform.position.z+61.5f)/63*880);
            if(p.kind==mission3Interact_Prop_ShengChiehLee.Kind.LostProperty){for(int dy=-17;dy<=17;dy++)for(int dx=-17;dx<=17;dx++)if(dx*dx+dy*dy<=289)Box(px+dx,py+dy,1,1,Cyan);Number((p.index+1).ToString(),px-6,py-10,4,Color.white);}
            else { Box(px-10,py-10,20,20,Gold); }
        }
        // Static reception/lift markers, deliberately no service-door marker.
        Number("0",Edge(2)+13,Edge(18)+10,3,new Color(.78f,.23f,.18f));
        texture.SetPixels(pixels);texture.Apply();SaveTexture(texture,mission3Paths_Editor_ShengChiehLee.Maps+"/mission3FloorMap.png");UnityEngine.Object.DestroyImmediate(texture);
        manager.floorMap=AssetDatabase.LoadAssetAtPath<Texture2D>(mission3Paths_Editor_ShengChiehLee.Maps+"/mission3FloorMap.png");
        var map=AssetDatabase.LoadAssetAtPath<Material>(mission3Paths_Editor_ShengChiehLee.Material("mission3_MapDisplay"));map.SetTexture("_BaseMap",manager.floorMap);EditorUtility.SetDirty(map);
        var arrow=new Texture2D(40,48,TextureFormat.RGBA32,false);var ap=new Color[40*48];for(int y=0;y<48;y++)for(int x=0;x<40;x++){float width=(47-y)*.37f;if(Mathf.Abs(x-19.5f)<=width&&y>3)ap[y*40+x]=new Color(.9f,.20f,.17f);}
        arrow.SetPixels(ap);arrow.Apply();SaveTexture(arrow,mission3Paths_Editor_ShengChiehLee.InterfaceTextures+"/mission3PlayerArrow.png");UnityEngine.Object.DestroyImmediate(arrow);
    }
    static void SaveTexture(Texture2D texture,string path)
    {
        File.WriteAllBytes(path,texture.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.alphaIsTransparency=true;importer.maxTextureSize=2048;importer.SaveAndReimport();
    }

    static void PolishEnvironment(Scene scene)
    {
        var props=scene.GetRootGameObjects().First(x=>x.name=="Mission3_PropsAndInteractions").transform;
        var signs=new GameObject("Mission3_Wayfinding").transform;signs.SetParent(props,false);
        Sign(signs,new Vector3(9,3.05f,-19.5f),"301  WEST WING    /    302 + 303  >",0);
        Sign(signs,new Vector3(27,3.05f,-33),"<  RECEPTION    /    EAST WING  >",0);
        Sign(signs,new Vector3(54,3.05f,-27),"303  BOTANIST    /    LOUNGE  >",0);
        Sign(signs,new Vector3(42,3.05f,-51),"<  WEST WING    /    RECEPTION  >",0);
        var mapLegend=props.Find("Map legend");if(mapLegend!=null)mapLegend.GetComponent<TMP_Text>().text="FLOOR 03\nCYAN 1-6 : BELONGINGS\nAMBER : GUEST NOTES\nE : INSPECT    M : MAP";
        foreach(var t in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)))
        {
            if(t.name.StartsWith("Wall_")||t.name.StartsWith("Floor_")||t.name.StartsWith("Ceiling_")||t.name=="Walnut wainscot"||t.name=="Brass chair rail")GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);
        }
        foreach(var p in props.GetComponentsInChildren<mission3Interact_Prop_ShengChiehLee>(true).Where(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.LostProperty))DetailItem(p);
    }
    static void Sign(Transform parent,Vector3 position,string value,float yaw)
    {
        var board=GameObject.CreatePrimitive(PrimitiveType.Cube);board.name="Hotel wayfinding / "+value;board.transform.SetParent(parent);board.transform.position=position;board.transform.rotation=Quaternion.Euler(0,yaw,0);board.transform.localScale=new Vector3(2.75f,.38f,.08f);UnityEngine.Object.DestroyImmediate(board.GetComponent<Collider>());board.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(mission3Paths_Editor_ShengChiehLee.Material("mission3_Ink"));
        var label=new GameObject("Direction text").AddComponent<TextMeshPro>();label.transform.SetParent(parent);label.transform.position=position+Vector3.back*.055f;label.font=font;label.text=value.Replace("<","").Replace(">","");label.fontSize=1.15f;label.alignment=TextAlignmentOptions.Center;label.color=Gold;label.rectTransform.sizeDelta=new Vector2(2.6f,.35f);
        var back=UnityEngine.Object.Instantiate(label.gameObject,parent);back.name="Direction text reverse";back.transform.position=position+Vector3.forward*.055f;back.transform.rotation=Quaternion.Euler(0,180,0);
    }
    static GameObject Detail(Transform parent,string name,PrimitiveType shape,Vector3 pos,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    static void DetailItem(mission3Interact_Prop_ShengChiehLee item)
    {
        var brass=AssetDatabase.LoadAssetAtPath<Material>(mission3Paths_Editor_ShengChiehLee.Material("mission3_Brass"));var ink=AssetDatabase.LoadAssetAtPath<Material>(mission3Paths_Editor_ShengChiehLee.Material("mission3_Ink"));var linen=AssetDatabase.LoadAssetAtPath<Material>(mission3Paths_Editor_ShengChiehLee.Material("mission3_Linen"));var leaf=AssetDatabase.LoadAssetAtPath<Material>(mission3Paths_Editor_ShengChiehLee.Material("mission3_Leaf"));
        if(item.index==0)
        {
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var tick=Detail(item.transform,"Compass index",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*.255f,.045f,Mathf.Cos(a)*.255f),new Vector3(.012f,.01f,i%3==0?.065f:.035f),ink);tick.transform.localRotation=Quaternion.Euler(0,i*30,0);}
            Detail(item.transform,"Compass crown",PrimitiveType.Sphere,new Vector3(0,0,.36f),new Vector3(.12f,.1f,.1f),brass);
        }
        if(item.index==1){for(int i=0;i<4;i++)Detail(item.transform,"Postcard address line",PrimitiveType.Cube,new Vector3(.13f,.04f,-.10f+i*.055f),new Vector3(.2f,.01f,.008f),brass);Detail(item.transform,"Postage stamp",PrimitiveType.Cube,new Vector3(.22f,.047f,.13f),new Vector3(.08f,.01f,.08f),leaf);}
        if(item.index==2){Detail(item.transform,"Tempo scale",PrimitiveType.Cube,new Vector3(0,.04f,-.16f),new Vector3(.19f,.48f,.008f),linen);for(int i=0;i<7;i++)Detail(item.transform,"Tempo tick",PrimitiveType.Cube,new Vector3(0,-.16f+i*.055f,-.17f),new Vector3(.15f,.01f,.008f),ink);Detail(item.transform,"Pendulum weight",PrimitiveType.Cube,new Vector3(0,.13f,-.19f),new Vector3(.12f,.10f,.05f),brass);}
        if(item.index==3){Detail(item.transform,"Record label",PrimitiveType.Cylinder,new Vector3(0,.045f,0),new Vector3(.22f,.008f,.22f),linen);Detail(item.transform,"Spindle",PrimitiveType.Cylinder,new Vector3(0,.06f,0),new Vector3(.028f,.008f,.028f),ink);VinylGrooves(item.transform,brass);}
        if(item.index==4){var flap=Detail(item.transform,"Envelope seam",PrimitiveType.Cube,new Vector3(0,.045f,.06f),new Vector3(.3f,.01f,.008f),brass);flap.transform.localRotation=Quaternion.Euler(0,28,0);Detail(item.transform,"Seed seal",PrimitiveType.Cylinder,new Vector3(.14f,.05f,-.08f),new Vector3(.09f,.01f,.09f),leaf);}
        if(item.index==5){Detail(item.transform,"Fern stem",PrimitiveType.Cube,new Vector3(0,.06f,0),new Vector3(.01f,.01f,.34f),brass);for(int i=0;i<6;i++)for(int side=-1;side<=1;side+=2){var blade=Detail(item.transform,"Preserved leaflet",PrimitiveType.Sphere,new Vector3(side*(.04f+i*.006f),.065f,-.12f+i*.045f),new Vector3(.12f-i*.008f,.009f,.035f),leaf);blade.transform.localRotation=Quaternion.Euler(0,side*32,0);}}
    }

    static void VinylGrooves(Transform parent,Material material)
    {
        var group=new GameObject("Record grooves");group.transform.SetParent(parent,false);
        for(int i=0;i<5;i++)
        {
            var go=new GameObject("Groove "+i);go.transform.SetParent(group.transform,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.sharedMaterial=material;line.widthMultiplier=.0025f;line.positionCount=64;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            float radius=.16f+i*.027f;for(int j=0;j<64;j++){float angle=j*Mathf.PI/32;line.SetPosition(j,new Vector3(Mathf.Sin(angle)*radius,.044f,Mathf.Cos(angle)*radius));}
        }
    }

    [MenuItem("Mission3/Validate Mission1 Presentation Upgrade")]
    public static void Validate()
    {
        var manager=Find<mission3Hotel_Manager_ShengChiehLee>();if(manager==null||manager.canvasUI==null)throw new Exception("Mission3 Canvas is not assigned.");
        var ui=manager.canvasUI;if(ui.pages.Length!=9||ui.ownerButtons.Length!=18||ui.itemMarkers.Length!=6||ui.noteMarkers.Length!=3)throw new Exception("Incomplete UI references.");
        if(manager.headBob==null||manager.playerBody.GetComponentInChildren<mission3Watch_Player_ShengChiehLee>(true)==null)throw new Exception("Mission1 player rig missing.");
        var scene=manager.gameObject.scene;var all=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
        if(all.Count(t=>t.GetComponent<Camera>()!=null&&t.GetComponent<Camera>().enabled)!=1)throw new Exception("Expected exactly one enabled player camera.");
        if(all.Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new Exception("Missing script detected.");
        if(all.Any(t=>t.GetComponent<PocketWatch>()!=null||t.GetComponent<ExitPuzzle>()!=null))throw new Exception("Mission1 game rules leaked into Mission3.");
        if(ui.itemPositions.Any(x=>x==null)||ui.notePositions.Any(x=>x==null))throw new Exception("Missing map position reference.");
        if(ui.mapRect.GetComponentsInChildren<mission3Interact_Prop_ShengChiehLee>(true).Length!=0)throw new Exception("Map includes world interactions.");
        // Walkability must include every objective with the secret door closed.
        var walk=new HashSet<Vector2Int>();foreach(var t in all){var p=t.name.Split('_');if(p.Length==3&&p[0]=="Floor"&&int.TryParse(p[1],out int x)&&int.TryParse(p[2],out int y))walk.Add(new Vector2Int(x,y));}
        var seen=new HashSet<Vector2Int>();var q=new Queue<Vector2Int>();q.Enqueue(new Vector2Int(3,4));seen.Add(new Vector2Int(3,4));
        var dirs=new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down};while(q.Count>0){var p=q.Dequeue();foreach(var d in dirs){var n=p+d;if(walk.Contains(n)&&seen.Add(n))q.Enqueue(n);}}
        if(seen.Count!=walk.Count)throw new Exception("Maze is disconnected.");
        foreach(var p in ui.itemPositions.Concat(ui.notePositions))if(!seen.Contains(new Vector2Int(Mathf.RoundToInt(p.position.x/3),Mathf.RoundToInt(-p.position.z/3))))throw new Exception("Unreachable objective: "+p.name);
        mission3Build_Editor_ShengChiehLee.Validate();mission3NextLevel_Editor_ShengChiehLee.Validate(manager);
        mission3Session_Editor_ShengChiehLee.Write("VALIDATION_PASS\nOne camera; Mission1 player/hand/watch/head bob; Canvas pages and 18 owner buttons; six item and three note markers; no hidden-door marker; connected maze; all objectives reachable; scoring; NEXT LEVEL binding; no missing scripts.");
    }
    public static void Capture() { mission3Session_Editor_ShengChiehLee.Write("CAPTURE_PENDING"); mission3Playtest_Editor_ShengChiehLee.Begin(false); }
    public static void Smoke() { mission3Playtest_Editor_ShengChiehLee.Begin(true); }
}
