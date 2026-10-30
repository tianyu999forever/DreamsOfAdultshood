using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class Mission6_GridBuilder_Jizhijin
{
    const string Root = "Assets/Mission6_Jizhijin";
    const string ScenePath = Root + "/Scenes/Mission6_Jizhijin.unity";
    static Scene scene;
    static T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
    static Mission6_GridBuilder_Jizhijin() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists("Temp/Mission6_Jizhijin.grid") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Temp/Mission6_Jizhijin.grid"); Directory.CreateDirectory("docs/Mission6_Jizhijin");
        try { Rebuild(); File.WriteAllText("docs/Mission6_Jizhijin/grid-build.txt", "Built connected 20x20 and 28x28 levels, graded crosswords, gate, and two NavMesh sentinels."); }
        catch (Exception e) { File.WriteAllText("docs/Mission6_Jizhijin/grid-build-error.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Mission6 Jizhijin/Rebuild two connected levels")]
    public static void Rebuild()
    {
        scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SceneManager.SetActiveScene(scene);
        var player = All<Moving>().First(x => x.gameObject.activeInHierarchy);
        var canvas = All<Canvas>().First(x => x.renderMode == RenderMode.ScreenSpaceOverlay);
        var inventory = All<Mission6_Letters_GameManager_Jizhijin>().Single();
        var watch = player.GetComponentInChildren<PocketWatch>(true);
        string nextScene = All<Mission6_Spelling_ExitPanel_Jizhijin>().FirstOrDefault()?.nextSceneName;
        var template = canvas.transform.Find("DropdownTemplate_Jizhijin");
        for (int i = canvas.transform.childCount - 1; i >= 0; i--) if (canvas.transform.GetChild(i) != template) Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject);
        foreach (var c in All<Mission6_Campaign_Jizhijin>()) Object.DestroyImmediate(c.gameObject);
        var oldGrid = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Grid20x20_Jizhijin"); if (oldGrid != null) Object.DestroyImmediate(oldGrid);
        var legacy = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "LegacyEnvironment_Jizhijin");
        if (legacy == null) legacy = new GameObject("LegacyEnvironment_Jizhijin");
        foreach (var root in scene.GetRootGameObjects())
            if (root != player.gameObject && root != canvas.gameObject && root != inventory.gameObject && root != legacy && root.GetComponent<UnityEngine.EventSystems.EventSystem>() == null) root.transform.SetParent(legacy.transform, true);
        legacy.SetActive(false);
        foreach (var flare in All<LensFlareComponentSRP>()) flare.enabled = false;
        foreach (var v in All<Volume>()) v.enabled = false;
        RenderSettings.fog = false; RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.69f,.75f,.79f);
        var camera = player.GetComponentInChildren<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.63f,.77f,.84f); camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        player.transform.position = Mission6_StageDefinition_Jizhijin.First.Center(1,1,1.4f); player.transform.rotation = Quaternion.identity; camera.transform.localRotation = Quaternion.Euler(-8,0,0);
        player.GetComponent<Rigidbody>().isKinematic = false;
        var rootGo = new GameObject("Mission6_TwoLevels_Jizhijin"); var campaign = rootGo.AddComponent<Mission6_Campaign_Jizhijin>();
        campaign.moving = player; campaign.mouseLook = player.GetComponentInChildren<MouseLook>(); campaign.headBob = player.GetComponentInChildren<HeadBob>(); campaign.watch = watch; campaign.inventory = inventory; campaign.nextSceneName = nextScene;
        campaign.photo = player.GetComponent<Mission6_Photo_Player_Jizhijin>(); campaign.photo.moving = player;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280,720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        campaign.objective = Label("Objective_Jizhijin", canvas.transform, "", 18, new Vector2(0,310),new Vector2(1200,32));
        campaign.timer = Label("StageTimer_Jizhijin", canvas.transform, "", 20, new Vector2(0,340),new Vector2(1000,30));
        var status = Label("PhotoStatus_Jizhijin", canvas.transform,"",17,new Vector2(0,280),new Vector2(1000,26)); campaign.photo.statusText=status; campaign.photoHint=status.gameObject;
        campaign.stageResult=Label("LastGrade_Jizhijin",canvas.transform,"",18,new Vector2(0,-275),new Vector2(1100,55));
        var bag=Panel("LetterBag_Jizhijin",canvas.transform,new Color(.035f,.07f,.09f,.94f),false); bag.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-320);bag.GetComponent<RectTransform>().sizeDelta=new Vector2(1000,60);
        inventory.inventoryText=Label("InventoryText",bag.transform,"",18,Vector2.zero,new Vector2(980,58));campaign.bagPanel=bag;
        campaign.levels = new Mission6_Grid_Level_Jizhijin[2]; campaign.puzzles = new Mission6_Spelling_ExitPanel_Jizhijin[2];
        for(int n=1;n<=2;n++)
        {
            var data=n==1?Mission6_StageDefinition_Jizhijin.First:Mission6_StageDefinition_Jizhijin.Second;
            var stage=BuildTerrain(rootGo.transform,data);stage.campaign=campaign;campaign.levels[n-1]=stage;
            var map=Mission6_GridMap_Jizhijin.Create(data);
            var photo=MapPanel("Photo_Level"+n,canvas.transform,map,"LEVEL "+n+" MAP",out var photoTitle);photo.SetActive(false);stage.photoPanel=photo;stage.photoTitle=photoTitle;
            var board=new GameObject("PLAN_Board_Level"+n,typeof(RectTransform),typeof(Canvas));board.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;board.transform.SetParent(stage.transform,false);board.transform.position=data.origin+new Vector3(4.8f,3.35f,9.53f);board.transform.localScale=Vector3.one*.01f;board.GetComponent<RectTransform>().sizeDelta=new Vector2(800,800);
            var poster=MapPanel("Poster",board.transform,map,"LEVEL "+n+" / PLAN",out var boardTitle);boardTitle.gameObject.SetActive(false);
            Label("Legend",poster.transform,"GOLD LETTERS / BLUE PLATFORMS / RED SENTINELS",15,new Vector2(0,-287),new Vector2(790,25));
            Label("Instruction",poster.transform,"WASD MOVE / LEAVE THROUGH THE RIGHT OPENING",16,new Vector2(0,-318),new Vector2(790,26));
            stage.planPanel=board;if(n==2)board.SetActive(false);
            Trigger("Leave_PLAN",stage,0,data.origin+new Vector3(10.5f,2,3.6f),new Vector3(.4f,5,2.2f));
            Trigger("Crossword_Exit",stage,1,data.Center(data.Exit.x,data.Exit.y,1.6f),new Vector3(2.2f,3.4f,2.2f));
            var exitLabel=WorldText("ExitSign",stage.transform,data.Center(data.Exit.x,data.Exit.y,3),n==1?"EXIT / LEVEL 2":"FINAL EXIT",6);exitLabel.gameObject.AddComponent<FaceCamera>();
            var leave=WorldText("LeaveStartRoom",stage.transform,data.origin+new Vector3(9.25f,3.2f,3.6f),"LEAVE TO START",4);leave.rectTransform.sizeDelta=new Vector2(6,1);leave.gameObject.AddComponent<FaceCamera>();
            for(int i=0;i<data.pickups.Length;i++)
            {
                var cell=data.pickups[i];var item=new GameObject("Pickup_"+data.letters[i]+"_"+i,typeof(BoxCollider),typeof(Mission6_LetterPickup_Collectible_Jizhijin));item.transform.SetParent(stage.transform);item.transform.position=data.Center(cell.x,cell.y,1.2f);
                var collider=item.GetComponent<BoxCollider>();collider.isTrigger=true;collider.size=new Vector3(1.15f,2.3f,1.15f);
                var pickup=item.GetComponent<Mission6_LetterPickup_Collectible_Jizhijin>();pickup.inventory=inventory;pickup.letter=data.letters[i].ToString();
                var label=WorldText("LetterVisual",item.transform,item.transform.position,pickup.letter,12);label.color=new Color(1,.8f,.2f);label.gameObject.AddComponent<FaceCamera>();pickup.visual=label.transform;item.AddComponent<FloatingItem>().height=.12f;
            }
            campaign.puzzles[n-1]=BuildPuzzle(canvas.transform,template.GetComponent<TMP_Dropdown>(),stage,inventory);campaign.puzzles[n-1].gameObject.SetActive(false);
        }
        var wall=Material("GridWall",new Color(.23f,.35f,.4f));var floor=Material("GridFloorA",new Color(.44f,.56f,.51f));
        Cube("Connecting_Corridor",rootGo.transform,new Vector3(52.8f,-.1f,44.4f),new Vector3(9.6f,.2f,2.4f),floor);
        Cube("Corridor_North",rootGo.transform,new Vector3(52.8f,1.8f,45.8f),new Vector3(9.6f,3.6f,.4f),wall);Cube("Corridor_South",rootGo.transform,new Vector3(52.8f,1.8f,43),new Vector3(9.6f,3.6f,.4f),wall);
        var gate=Cube("Level1_Gate_To_Level2",rootGo.transform,new Vector3(48,1.8f,44.4f),new Vector3(.45f,3.6f,2.4f),Material("Gate",new Color(.65f,.48f,.18f)));campaign.firstGate=gate.transform;campaign.gateCollider=gate.GetComponent<Collider>();
        WorldText("GateHint",rootGo.transform,new Vector3(47.7f,3.9f,44.4f),"7 / 10 WORDS TO OPEN",3).gameObject.AddComponent<FaceCamera>();
        Trigger("Level2_Entrance",campaign.levels[1],2,new Vector3(58.2f,2,44.4f),new Vector3(.4f,5,2.2f));
        var sunlight=new GameObject("Sunlight",typeof(Light));sunlight.transform.SetParent(rootGo.transform);sunlight.transform.rotation=Quaternion.Euler(55,-25,0);var light=sunlight.GetComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.94f,.84f);light.intensity=1.1f;light.shadows=LightShadows.Soft;
        BuildNavigation(campaign);
        BuildEnemies(campaign);
        campaign.deathPanel=Panel("DeathPanel_Jizhijin",canvas.transform,new Color(.02f,.04f,.06f,1),true);Label("Title",campaign.deathPanel.transform,"TRY ANOTHER ROUTE",36,new Vector2(0,100),new Vector2(1100,60));campaign.deathReason=Label("Reason",campaign.deathPanel.transform,"",22,new Vector2(0,20),new Vector2(1050,100));UnityEventTools.AddPersistentListener(Button("Retry",campaign.deathPanel.transform,"RETRY LEVEL",new Vector2(0,-100)).onClick,campaign.Replay);campaign.deathPanel.SetActive(false);watch.deathPanel=campaign.deathPanel;
        campaign.finishPanel=Panel("Campaign_Results",canvas.transform,new Color(.02f,.04f,.06f,1),true);campaign.finishText=Label("Results",campaign.finishPanel.transform,"",30,new Vector2(0,50),new Vector2(1100,460));UnityEventTools.AddPersistentListener(Button("Replay",campaign.finishPanel.transform,"PLAY AGAIN",new Vector2(-145,-250)).onClick,campaign.Replay);UnityEventTools.AddPersistentListener(Button("NextMission",campaign.finishPanel.transform,"NEXT MISSION",new Vector2(145,-250)).onClick,campaign.NextMission);campaign.finishPanel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Selection.activeGameObject=rootGo;
    }
    static Mission6_Grid_Level_Jizhijin BuildTerrain(Transform parent,Mission6_StageDefinition_Jizhijin d)
    {
        var root=new GameObject("Level"+d.number+"_"+d.size+"x"+d.size);root.transform.SetParent(parent);root.transform.position=d.origin;var stage=root.AddComponent<Mission6_Grid_Level_Jizhijin>();stage.stageNumber=d.number;
        var a=Material("GridFloorA",new Color(.44f,.56f,.51f));var b=Material("GridFloorB",new Color(.5f,.62f,.56f));var wall=Material("GridWall",new Color(.23f,.35f,.4f));var raised=Material("GridRaised",new Color(.24f,.5f,.59f));var rampMat=Material("GridRamp",new Color(.65f,.51f,.33f));var wordMat=Material("GridWord",new Color(.86f,.82f,.64f));
        for(int row=0;row<d.size;row++)for(int x=0;x<d.size;x++)
        {
            var tile=Cube("Cell_"+x.ToString("00")+"_"+row.ToString("00"),root.transform,d.Center(x,row,-.1f),new Vector3(2.37f,.2f,2.37f),d.Height(x,row)>0?raised:((x+row)%2==0?a:b));
            int ramp=d.Ramp(x,row);if(ramp!=0){tile.transform.position=d.Center(x,row,.08f);tile.transform.rotation=Quaternion.Euler(0,0,ramp*Mathf.Atan(.36f/2.4f)*Mathf.Rad2Deg);tile.transform.localScale=new Vector3(2.43f,.2f,2.37f);tile.GetComponent<Renderer>().sharedMaterial=rampMat;}
            if(d.IsWall(x,row)){float h=row==4&&x<4?7.5f:3.6f;Cube("Wall_"+x+"_"+row,root.transform,d.Center(x,row,h/2),new Vector3(2.4f,h,2.4f),wall);}
            if(d.crossword.TryGetValue(new Vector2Int(x,row),out var c))
            {tile.GetComponent<Renderer>().sharedMaterial=wordMat;var glyph=WorldText("Word_"+x+"_"+row,root.transform,d.Center(x,row,.025f),c.missing?"_":c.answer.ToString(),11);glyph.transform.rotation=Quaternion.Euler(90,0,0);glyph.color=new Color(.08f,.18f,.2f);}
        }
        float width=d.size*2.4f;
        Cube("Boundary_North",root.transform,d.origin+new Vector3(width/2,1.8f,width+.2f),new Vector3(width,3.6f,.4f),wall);Cube("Boundary_South",root.transform,d.origin+new Vector3(width/2,1.8f,-.2f),new Vector3(width,3.6f,.4f),wall);
        for(int r=0;r<d.size;r++)
        {
            if(!(d.number==2&&r==1))Cube("Boundary_West_"+r,root.transform,d.origin+new Vector3(-.2f,1.8f,(r+.5f)*2.4f),new Vector3(.4f,3.6f,2.4f),wall);
            if(!(d.number==1&&r==18))Cube("Boundary_East_"+r,root.transform,d.origin+new Vector3(width+.2f,1.8f,(r+.5f)*2.4f),new Vector3(.4f,3.6f,2.4f),wall);
        }
        return stage;
    }
    static void Trigger(string name,Mission6_Grid_Level_Jizhijin level,int action,Vector3 pos,Vector3 size)
    {var go=new GameObject(name,typeof(BoxCollider),typeof(Mission6_StageTrigger_Jizhijin));go.transform.SetParent(level.transform);go.transform.position=pos;go.GetComponent<BoxCollider>().isTrigger=true;go.GetComponent<BoxCollider>().size=size;go.GetComponent<Mission6_StageTrigger_Jizhijin>().level=level;go.GetComponent<Mission6_StageTrigger_Jizhijin>().action=action;}
    static void BuildNavigation(Mission6_Campaign_Jizhijin campaign)
    {
        var sources=new List<NavMeshBuildSource>();foreach(var box in campaign.levels[1].GetComponentsInChildren<BoxCollider>())
        {if(box.isTrigger)continue;sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=box.transform.localToWorldMatrix*Matrix4x4.Translate(box.center),size=box.size,area=0});}
        var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.35f;settings.agentHeight=1.8f;settings.agentClimb=.5f;settings.overrideVoxelSize=true;settings.voxelSize=.12f;
        var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(campaign.levels[1].Data.origin+new Vector3(33,3,33),new Vector3(75,16,75)),Vector3.zero,Quaternion.identity);
        const string path=Root+"/Materials/Mission6_Level2_NavMesh.asset";var old=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
        if(old==null){AssetDatabase.CreateAsset(data,path);campaign.navigation=data;}else{EditorUtility.CopySerialized(data,old);Object.DestroyImmediate(data);campaign.navigation=old;EditorUtility.SetDirty(old);}
    }
    static void BuildEnemies(Mission6_Campaign_Jizhijin campaign)
    {
        var d=campaign.levels[1].Data;var list=new List<Mission6_Enemy_Jizhijin>();var mat=Material("Sentinel",new Color(.55f,.08f,.06f));var eye=Material("SentinelEye",new Color(1,.8f,.15f));
        for(int i=0;i<d.enemies.Length;i++)
        {
            var cell=d.enemies[i];var go=new GameObject("Sentinel_"+(i+1),typeof(NavMeshAgent),typeof(Mission6_Enemy_Jizhijin));go.transform.SetParent(campaign.levels[1].transform);go.transform.position=d.Center(cell.x,cell.y,.05f);
            var agent=go.GetComponent<NavMeshAgent>();agent.enabled=false;agent.radius=.35f;agent.height=1.8f;agent.baseOffset=0;agent.stoppingDistance=.6f;
            var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="Body";body.transform.SetParent(go.transform,false);body.transform.localPosition=new Vector3(0,1,0);body.transform.localScale=new Vector3(.85f,1,.85f);body.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(body.GetComponent<Collider>());
            Cube("Eyes",go.transform,go.transform.position+new Vector3(0,1.5f,.4f),new Vector3(.5f,.16f,.12f),eye);
            var enemy=go.GetComponent<Mission6_Enemy_Jizhijin>();enemy.player=campaign.moving.transform;enemy.campaign=campaign;
            var points=i==0?new[]{new Vector2Int(12,6),new Vector2Int(23,6),new Vector2Int(23,8),new Vector2Int(12,8)}:new[]{new Vector2Int(14,12),new Vector2Int(20,12),new Vector2Int(20,14),new Vector2Int(14,14)};
            enemy.patrolPoints=points.Select((p,index)=>{var t=new GameObject("Patrol_"+i+"_"+index).transform;t.SetParent(campaign.levels[1].transform);t.position=d.Center(p.x,p.y);return t;}).ToArray();list.Add(enemy);
        }
        campaign.enemies=list.ToArray();
    }
    static GameObject MapPanel(string name,Transform parent,Texture2D map,string heading,out TMP_Text title)
    {
        var panel=Panel(name,parent,new Color(.025f,.05f,.065f,1),true);var image=new GameObject("GridMapImage",typeof(RectTransform),typeof(RawImage));image.transform.SetParent(panel.transform,false);image.GetComponent<RectTransform>().sizeDelta=new Vector2(555,555);image.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,4);image.GetComponent<RawImage>().texture=map;image.GetComponent<RawImage>().raycastTarget=false;title=Label("MapTitle",panel.transform,heading,27,new Vector2(0,318),new Vector2(1100,40));return panel;
    }
    static Mission6_Spelling_ExitPanel_Jizhijin BuildPuzzle(Transform parent,TMP_Dropdown template,Mission6_Grid_Level_Jizhijin level,Mission6_Letters_GameManager_Jizhijin inventory)
    {
        var data=level.Data;var panel=Panel("CrosswordPanel_Level"+data.number,parent,new Color(.025f,.05f,.065f,1),true);var p=panel.AddComponent<Mission6_Spelling_ExitPanel_Jizhijin>();p.inventory=inventory;p.level=level;
        Label("Title",panel.transform,"LEVEL "+data.number+" CROSSWORD",32,new Vector2(0,310),new Vector2(1200,48));Label("Hint",panel.transform,"PASS "+data.minimumWords+" / "+data.words.Length+" WORDS   |   MORE WORDS + LESS TIME = BETTER GRADE",18,new Vector2(0,267),new Vector2(1200,30));
        p.bagText=Label("CollectedLetters",panel.transform,"",16,new Vector2(0,232),new Vector2(1200,28));p.bagText.color=new Color(1,.86f,.5f);
        p.timeText=Label("AnswerTimer",panel.transform,"",16,new Vector2(0,204),new Vector2(1200,25));
        int minX=data.crossword.Keys.Min(v=>v.x),maxX=data.crossword.Keys.Max(v=>v.x),minY=data.crossword.Keys.Min(v=>v.y),maxY=data.crossword.Keys.Max(v=>v.y);
        float size=Mathf.Min(44,350f/Mathf.Max(maxX-minX+1,maxY-minY+1));var slots=new List<TMP_Dropdown>();var cells=new List<Vector2Int>();
        foreach(var pair in data.crossword.OrderByDescending(v=>v.Key.y).ThenBy(v=>v.Key.x))
        {
            var cell=pair.Value;var pos=pair.Key;var square=Panel("Square_"+pos.x+"_"+pos.y,panel.transform,Color.white,false);square.GetComponent<RectTransform>().anchoredPosition=new Vector2(-290+(pos.x-(minX+maxX)/2f)*size,20+(pos.y-(minY+maxY)/2f)*size);square.GetComponent<RectTransform>().sizeDelta=Vector2.one*(size-1);
            if(cell.missing)
            {
                var slot=Object.Instantiate(template,square.transform);slot.name="Blank_"+pos.x+"_"+pos.y;slot.gameObject.SetActive(true);slot.onValueChanged=new TMP_Dropdown.DropdownEvent();slot.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-2);slot.GetComponent<RectTransform>().sizeDelta=new Vector2(size-3,size-8);slot.captionText.fontSize=size*.56f;slot.captionText.alignment=TextAlignmentOptions.Center;slot.captionText.rectTransform.anchorMin=Vector2.zero;slot.captionText.rectTransform.anchorMax=Vector2.one;slot.captionText.rectTransform.offsetMin=new Vector2(1,0);slot.captionText.rectTransform.offsetMax=new Vector2(-10,0);slot.GetComponent<Image>().color=new Color(1,.9f,.65f);ConfigureLetterMenu(slot);slots.Add(slot);cells.Add(pos);
            }
            else{var fixedLetter=Label("Given",square.transform,cell.answer.ToString(),size*.6f,new Vector2(0,-2),Vector2.one*(size-2));fixedLetter.color=new Color(.06f,.16f,.2f);}
            if(cell.number>0){var number=Label("Number",square.transform,cell.number.ToString(),Mathf.Max(8,size*.22f),new Vector2(-size*.32f,size*.32f),new Vector2(size*.4f,size*.25f));number.color=Color.black;}
        }
        var labels=new TMP_Text[data.words.Length];var ordered=data.words.Select((w,i)=>new{w,i}).OrderBy(v=>v.w.down).ThenBy(v=>v.w.number).ToArray();int row=0;
        foreach(var item in ordered){var w=item.w;labels[item.i]=Label("Clue_"+item.i,panel.transform,(w.down?"D ":"A ")+w.number+". "+w.hint,data.number==1?19:16,new Vector2(280,168-24*row++),new Vector2(500,25));labels[item.i].alignment=TextAlignmentOptions.Left;}
        p.letterSlots=slots.ToArray();p.slotCells=cells.ToArray();p.wordLabels=labels;p.resultText=Label("Result",panel.transform,"",18,new Vector2(0,-220),new Vector2(1200,60));
        UnityEventTools.AddPersistentListener(Button("SubmitButton",panel.transform,"CHECK WORDS",new Vector2(0,-270)).onClick,p.CheckAnswer);
        var replay=Button("Replay",panel.transform,"PLAY AGAIN",new Vector2(0,-320));UnityEventTools.AddPersistentListener(replay.onClick,p.PlayAgain);p.playAgainButton=replay.gameObject;
        var next=Button("NextLevel_Jizhijin",panel.transform,data.number==1?"NEXT LEVEL":"FINISH LEVEL",new Vector2(290,-320));UnityEventTools.AddPersistentListener(next.onClick,p.NextLevel);p.nextLevelButton=next.gameObject;
        UnityEventTools.AddPersistentListener(Button("BackToGrid",panel.transform,"BACK TO MAZE",new Vector2(-290,-320)).onClick,level.campaign.ClosePuzzle);return p;
    }
    static Material Material(string name, Color color)
    {
        string path = Root + "/Materials/Mission6_" + name + "_Jizhijin.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.color = color; mat.SetFloat("_Smoothness", 0.05f); EditorUtility.SetDirty(mat); return mat;
    }
    static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name;
        cube.transform.SetParent(parent); cube.transform.position = position; cube.transform.localScale = size;
        cube.GetComponent<Renderer>().sharedMaterial = material; return cube;
    }
    static TextMeshPro WorldText(string name, Transform parent, Vector3 position, string text, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro)); go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshPro>(); label.rectTransform.sizeDelta = new Vector2(2.2f, 2.2f);
        label.transform.position = position; label.rectTransform.anchoredPosition = new Vector2(position.x - parent.position.x, position.y - parent.position.y);
        label.text = text; label.fontSize = size; label.alignment = TextAlignmentOptions.Center; label.color = Color.white;
        label.margin = Vector4.zero; label.raycastTarget = false; return label;
    }
    static GameObject Panel(string name, Transform parent, Color color, bool full)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        if (full) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero; }
        go.GetComponent<Image>().color = color; return go;
    }
    static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Vector2 position, Vector2 dimensions)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>(); label.text = text; label.fontSize = size;
        label.color = Color.white; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.rectTransform.anchoredPosition = position; label.rectTransform.sizeDelta = dimensions; return label;
    }
    static Button Button(string name, Transform parent, string text, Vector2 position)
    {
        var go = Panel(name, parent, new Color(0.1f, 0.37f, 0.43f, 1), false);
        var rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(260, 44);
        var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
        Label("Label", go.transform, text, 21, Vector2.zero, new Vector2(250, 42)); return button;
    }
    static void ConfigureLetterMenu(TMP_Dropdown slot)
    {
        // The small crossword button and its expanded menu need independent widths.
        // Keep letter labels wide; the second level bag scrolls beyond ten rows.
        if (slot.template != null) Object.DestroyImmediate(slot.template.gameObject);
        var popup = Panel("LetterMenu", slot.transform, new Color(0.96f, 0.97f, 0.94f, 1), false);
        var rect = popup.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(150, 270);
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(popup.transform, false);
        var viewRect = viewport.GetComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero; viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = new Vector2(4, 0); viewRect.offsetMax = new Vector2(-20, 0);
        var content = new GameObject("Content", typeof(RectTransform)); content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 1); contentRect.sizeDelta = new Vector2(0, 26);
        var item = Panel("Item", content.transform, Color.white, false);
        var itemRect = item.GetComponent<RectTransform>();
        itemRect.anchorMin = Vector2.zero; itemRect.anchorMax = Vector2.right;
        itemRect.anchoredPosition = new Vector2(0, 13); itemRect.sizeDelta = new Vector2(0, 26);
        var toggle = item.AddComponent<Toggle>(); toggle.targetGraphic = item.GetComponent<Image>();
        var colors = toggle.colors; colors.highlightedColor = new Color(1, 0.89f, 0.6f); colors.selectedColor = colors.highlightedColor; toggle.colors = colors;
        var check = Label("Selected", item.transform, ">", 18, Vector2.zero, new Vector2(20, 26));
        check.color = new Color(0.1f, 0.25f, 0.28f);
        check.rectTransform.anchorMin = check.rectTransform.anchorMax = new Vector2(0, 0.5f); check.rectTransform.anchoredPosition = new Vector2(12, 0);
        toggle.graphic = check;
        var text = Label("Letter", item.transform, "-", 21, Vector2.zero, new Vector2(80, 26));
        text.color = new Color(0.06f, 0.16f, 0.2f); text.alignment = TextAlignmentOptions.Left;
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(28, 0); text.rectTransform.offsetMax = new Vector2(-8, 0);
        var scroll = popup.AddComponent<ScrollRect>(); scroll.content = contentRect; scroll.viewport = viewRect;
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        var barGo = Panel("Scrollbar", popup.transform, new Color(.7f,.75f,.72f,1), false);
        var barRect = barGo.GetComponent<RectTransform>(); barRect.anchorMin = new Vector2(1,0); barRect.anchorMax = Vector2.one; barRect.offsetMin = new Vector2(-16,2); barRect.offsetMax = new Vector2(-2,-2);
        var handle = Panel("Handle", barGo.transform, new Color(.25f,.4f,.4f,1), true);
        var bar = barGo.AddComponent<Scrollbar>(); bar.handleRect = handle.GetComponent<RectTransform>(); bar.targetGraphic = handle.GetComponent<Image>(); bar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        slot.template = rect; slot.itemText = text; slot.itemImage = null; slot.alphaFadeSpeed = 0;
        popup.SetActive(false);
    }
}
