using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
[InitializeOnLoad]
public static class Mission6_Verify_Jizhijin
{
    const string Flag="Mission6.Campaign.Verify",Out="docs/Mission6_Jizhijin/";
    static readonly List<string> results=new List<string>(),errors=new List<string>();
    static Mission6_Campaign_Jizhijin c; static Keyboard keyboard; static int stage,pickup; static float next,deadline,before;static Vector3 position;static bool background, asyncShaders;
    static Mission6_Verify_Jizhijin()
    {
        EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;
        Application.logMessageReceived+=(m,t,type)=>{if(SessionState.GetBool(Flag,false)&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))errors.Add(m+"\n"+t);};
    }
    [MenuItem("Mission6 Jizhijin/Verify two level campaign")]
    public static void Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        results.Clear();errors.Clear();SessionState.SetBool(Flag,true);SessionState.SetString(Flag+".previous",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(Mission6_Setup_Jizhijin.ScenePath);EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Flag,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){stage=0;next=Time.realtimeSinceStartup+1;deadline=Time.realtimeSinceStartup+220;background=Application.runInBackground;Application.runInBackground=true;asyncShaders=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;}
        if(state==PlayModeStateChange.EnteredEditMode){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Flag+".previous",""));SessionState.SetBool(Flag,false);Write();}
    }
    static void Write(){Directory.CreateDirectory(Out);File.WriteAllLines(Out+"campaign-verification.txt",results.Concat(errors.Select(e=>"RUNTIME ERROR: "+e)));}
    static void Check(bool ok,string name){results.Add((ok?"PASS: ":"FAIL: ")+name);Write();if(!ok)throw new Exception(name);}
    static void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.GetFullPath(Out+name+".png"));
    static void Key(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
    static void Pose(Vector3 p,float yaw=0,float pitch=0){var rb=c.moving.GetComponent<Rigidbody>();rb.position=p;rb.rotation=Quaternion.Euler(0,yaw,0);rb.linearVelocity=Vector3.zero;c.mouseLook.enabled=c.headBob.enabled=false;Camera.main.transform.localRotation=Quaternion.Euler(pitch,0,0);Physics.SyncTransforms();}
    static void Advance(int value,float delay=.25f){stage=value;next=Time.realtimeSinceStartup+delay;}
    static void Finish(){if(keyboard!=null)InputSystem.RemoveDevice(keyboard);Time.timeScale=1;Application.runInBackground=background;ShaderUtil.allowAsyncCompilation=asyncShaders;results.Add(errors.Count==0?"PASS: no runtime errors":"FAIL: runtime errors");Write();stage=100;EditorApplication.ExitPlaymode();}
    static Mission6_Spelling_ExitPanel_Jizhijin Puzzle=>c.puzzles[c.CurrentLevel.stageNumber-1];
    static void Fill(Mission6_Spelling_ExitPanel_Jizhijin p,int mask)
    {
        p.ClearAnswers();
        for(int i=0;i<p.slotCells.Length;i++)if((mask&(1<<i))!=0)
            Check(p.SelectLetter(i,p.level.Data.crossword[p.slotCells[i]].answer),"available tile fills crossword square "+i);
        p.CheckAnswer();
    }
    static bool FillExactly(Mission6_Spelling_ExitPanel_Jizhijin p,int target)
    {
        for(int mask=0;mask<(1<<p.slotCells.Length);mask++)
        {
            int count=p.level.Data.words.Count(w=>Enumerable.Range(0,w.word.Length).All(i=>!p.level.Data.crossword[w.CellAt(i)].missing || (mask&(1<<Array.IndexOf(p.slotCells,w.CellAt(i))))!=0));
            if(count==target){Fill(p,mask);return p.CorrectWords==target;}
        }
        return false;
    }
    static void StopEnemies(){foreach(var e in c.enemies){e.Agent.isStopped=true;e.Agent.ResetPath();e.enabled=false;}}
    static void FacePickup(int index)
    {
        var d=c.CurrentLevel.Data;var cell=d.pickups[index];
        var dir=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left}.First(v=>!d.IsWall(cell.x+v.x,cell.y+v.y));
        var look=Quaternion.LookRotation(new Vector3(-dir.x,0,-dir.y));Pose(d.Center(cell.x+dir.x,cell.y+dir.y,1.4f),look.eulerAngles.y,18);
    }
    static void FaceEnemy()
    {
        var d=c.CurrentLevel.Data;var cell=d.enemies[0];
        var dir=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left}.First(v=>!d.IsWall(cell.x+v.x,cell.y+v.y)&&!d.IsWall(cell.x+v.x*2,cell.y+v.y*2));
        c.enemies[0].Agent.Warp(d.Center(cell.x,cell.y));
        var look=Quaternion.LookRotation(new Vector3(-dir.x,0,-dir.y));Pose(d.Center(cell.x+dir.x*2,cell.y+dir.y*2,1.4f),look.eulerAngles.y,8);
    }
    static void Occlusion()
    {
        var d=c.CurrentLevel.Data;
        for(int y=2;y<26;y++)for(int x=2;x<26;x++)foreach(var dir in new[]{Vector2Int.up,Vector2Int.right})
            if(d.IsWall(x,y)&&!d.IsWall(x+dir.x,y+dir.y)&&!d.IsWall(x-dir.x,y-dir.y))
            {c.enemies[0].Agent.Warp(d.Center(x+dir.x,y+dir.y));Pose(d.Center(x-dir.x,y-dir.y,1.4f));Physics.SyncTransforms();Check(!c.enemies[0].CanSeePlayer(),"a maze wall blocks enemy line of sight");return;}
        throw new Exception("No occlusion test wall found");
    }
    static int MinimumTurns(Mission6_StageDefinition_Jizhijin d)
    {
        var distance=new int[d.size,d.size,4];var queue=new Queue<Vector3Int>();
        for(int y=0;y<d.size;y++)for(int x=0;x<d.size;x++)for(int dir=0;dir<4;dir++)distance[x,y,dir]=int.MaxValue;
        for(int dir=0;dir<4;dir++){distance[d.Start.x,d.Start.y,dir]=0;queue.Enqueue(new Vector3Int(d.Start.x,d.Start.y,dir));}
        var directions=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
        while(queue.Count>0)
        {
            var p=queue.Dequeue();
            for(int dir=0;dir<4;dir++)
            {
                var n=new Vector2Int(p.x,p.y)+directions[dir];if(d.IsWall(n.x,n.y))continue;
                int turns=distance[p.x,p.y,p.z]+(p.z==dir?0:1);
                if(turns>=distance[n.x,n.y,dir])continue;
                distance[n.x,n.y,dir]=turns;queue.Enqueue(new Vector3Int(n.x,n.y,dir));
            }
        }
        return Enumerable.Range(0,4).Min(dir=>distance[d.Exit.x,d.Exit.y,dir]);
    }
    static void Layout(Mission6_Grid_Level_Jizhijin level)
    {
        var d=level.Data;Check(d.words.Length==(d.number==1?10:15)&&d.minimumWords==(d.number==1?7:11),"level "+d.number+" word total and 70 percent pass requirement");
        Check(level.transform.Cast<Transform>().Count(t=>t.name.StartsWith("Cell_"))==d.size*d.size,"level "+d.number+" has "+d.size+" x "+d.size+" tiles");
        var seen=new HashSet<Vector2Int>{d.Start};var queue=new Queue<Vector2Int>();queue.Enqueue(d.Start);while(queue.Count>0){var p=queue.Dequeue();foreach(var dir in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}){var n=p+dir;if(!d.IsWall(n.x,n.y)&&seen.Add(n))queue.Enqueue(n);}}
        Check(d.pickups.All(seen.Contains)&&seen.Contains(d.Exit)&&d.crossword.Keys.All(seen.Contains),"level "+d.number+" pickups, word cells and exit are reachable");
        Check(d.pickups.Distinct().Count()==d.pickups.Length&&d.pickups.All(p=>!d.crossword.ContainsKey(p)),"level "+d.number+" pickup markers do not cover crossword letters");
        Check(d.blanks.GroupBy(p=>d.crossword[p].answer).All(g=>d.letters.Count(ch=>ch==g.Key)>=g.Count()),"level "+d.number+" has enough physical tiles for every missing letter");
        Check(d.words.All(w=>Enumerable.Range(0,w.word.Length).Any(i=>d.crossword[w.CellAt(i)].missing)),"every level "+d.number+" word requires at least one player answer");
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mission6_Jizhijin/Materials/"+(d.number==1?"Mission6_GridMap_Jizhijin.png":"Mission6_GridMap_Level2_Jizhijin.png"));bool pixels=true;
        for(int row=0;row<d.size;row++)for(int x=0;x<d.size;x++){var sample=texture.GetPixel(60+x*44+1,60+row*44+1);var expected=d.crossword.ContainsKey(new Vector2Int(x,row))?Mission6_GridMap_Jizhijin.WallColor:Mission6_GridMap_Jizhijin.TerrainColor(d,x,row);pixels&=Mathf.Abs(sample.r-expected.r)<.015f&&Mathf.Abs(sample.g-expected.g)<.015f;}
        Check(pixels,"level "+d.number+" map agrees with every terrain and crossword cell");
        Check(level.GetComponentsInChildren<Mission6_LetterPickup_Collectible_Jizhijin>().Length==d.pickups.Length,"level "+d.number+" pickup count matches its map");
        if(d.number==2)
        {
            int walls=0,deadEnds=0,junctions=0;
            foreach(var pos in Enumerable.Range(0,d.size).SelectMany(y=>Enumerable.Range(0,d.size).Select(x=>new Vector2Int(x,y))))
            {if(d.IsWall(pos.x,pos.y)){walls++;continue;}int degree=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}.Count(v=>!d.IsWall(pos.x+v.x,pos.y+v.y));if(degree==1)deadEnds++;if(degree>=3)junctions++;}
            Check(walls==175&&deadEnds<5,"approved model B retains its broad courts and limited walls: "+walls+" / "+deadEnds);
            Check(MinimumTurns(d)==7,"every start-to-exit route requires at least seven turns, including crossword tiles");
            Check(d.Start==new Vector2Int(3,3)&&d.Exit==new Vector2Int(3,24)&&d.Landmarks.Length==4,"model B places its start, exit and four court landmarks correctly");
            foreach(var enemy in level.campaign.enemies)foreach(var patrol in enemy.patrolPoints)
            {var path=new UnityEngine.AI.NavMeshPath();Check(UnityEngine.AI.NavMesh.CalculatePath(enemy.transform.position,patrol.position,UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete,"model B enemy patrol reaches "+patrol.name);}
            foreach(var pos in d.pickups.Concat(new[]{d.Exit}))
            {var path=new UnityEngine.AI.NavMeshPath();Check(UnityEngine.AI.NavMesh.CalculatePath(d.Center(d.Start.x,d.Start.y),d.Center(pos.x,pos.y),UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete,"baked maze navigation reaches "+pos);}
        }
    }
    static void Tick()
    {
        if(File.Exists("Temp/Mission6_Jizhijin.verify")&&!File.Exists("Temp/Mission6_Jizhijin.grid")&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode){File.Delete("Temp/Mission6_Jizhijin.verify");Start();return;}
        if(!SessionState.GetBool(Flag,false)||!EditorApplication.isPlaying||stage==100||Time.realtimeSinceStartup<next)return;
        try
        {
            if(Time.realtimeSinceStartup>deadline)throw new TimeoutException();
            switch(stage)
            {
                case 0:
                    c=UnityEngine.Object.FindFirstObjectByType<Mission6_Campaign_Jizhijin>();keyboard=InputSystem.AddDevice<Keyboard>();c.mouseLook.enabled=c.headBob.enabled=false;
                    Check(c.CurrentLevel==c.levels[0]&&c.Phase==Mission6_Phase_Jizhijin.Planning&&c.moving.enabled,"level one begins with a movable PLAN room");
                    Check(!RenderSettings.fog&&c.GateOpen&&!c.gateCollider.enabled,"clear view and unblocked physical passage at start");
                    Check(c.enterSecondButton.activeSelf&&c.enterSecondButton.GetComponent<Button>().interactable,"ENTER LEVEL 2 is immediately visible and clickable during PLAN");
                    c.enterSecondButton.GetComponent<Button>().onClick.Invoke();Check(c.CurrentLevel.stageNumber==2&&c.FirstSkipped&&c.FirstScore.points==0&&c.Phase==Mission6_Phase_Jizhijin.Planning,"HUD button enters level two without collecting or solving and gives no first-level grade");
                    c.StartLevel(1,true);c.mouseLook.enabled=c.headBob.enabled=false;
                    Check(!c.photo.TryUsePhoto()&&!c.photo.Used,"PLAN cannot consume the photo");Layout(c.levels[0]);Layout(c.levels[1]);
                    Check(c.enemies.Length==2&&c.enemies.All(e=>e.Agent.isOnNavMesh),"both sentinels are bound to the generated level two NavMesh");
                    Check(Mission6_Score_Jizhijin.Calculate(1,10,10,7,30,180).rank=="S"&&Mission6_Score_Jizhijin.Calculate(1,10,10,7,90,180).rank=="A"&&Mission6_Score_Jizhijin.Calculate(1,7,10,7,0,180).rank=="B"&&Mission6_Score_Jizhijin.Calculate(1,7,10,7,120,180).rank=="C","S A B C thresholds combine time and word count");
                    Check(!Mission6_Score_Jizhijin.Calculate(1,6,10,7,1,180).passed&&Mission6_Score_Jizhijin.Calculate(1,7,10,7,1,180).passed&&!Mission6_Score_Jizhijin.Calculate(2,10,15,11,1,300).passed&&Mission6_Score_Jizhijin.Calculate(2,11,15,11,1,300).passed,"pass boundaries are exactly 7 of 10 and 11 of 15");
                    Check(!Mission6_Score_Jizhijin.Calculate(1,10,10,7,180,180).passed,"even full word completion cannot pass after the time limit");
                    Capture("campaign-01-level1-plan");position=c.moving.transform.position;Key(UnityEngine.InputSystem.Key.W);Advance(1,.5f);break;
                case 1:
                    Key();Check(c.moving.transform.position.z>position.z+.5f&&c.Phase==Mission6_Phase_Jizhijin.Planning,"WASD moves during PLAN");Pose(new Vector3(8.3f,1.4f,3.6f));Key(UnityEngine.InputSystem.Key.D);Advance(2,.8f);break;
                case 2:
                    Key();Check(c.Phase==Mission6_Phase_Jizhijin.Exploring&&!c.CurrentLevel.planPanel.activeSelf,"walking through the opening starts level one early");Check(c.photo.TryUsePhoto()&&!c.photo.TryUsePhoto(),"photo opens once and repeated use cannot extend it");before=c.Elapsed;Advance(3,.2f);break;
                case 3:Capture("campaign-02-level1-photo");Advance(4,5.1f);break;
                case 4:
                    Check(!c.photo.IsVisible&&c.photo.Used&&c.Elapsed>before+5,"photo closes after five seconds while clear time keeps running");Check(!c.night.IsNight&&c.night.CycleTime==0&&!c.night.countdown.gameObject.activeSelf,"first level never activates the night mechanic");Pose(new Vector3(46.5f,1.4f,44.4f),90);Key(UnityEngine.InputSystem.Key.W);Advance(40,2.9f);break;
                case 40:
                    Key();Check(c.CurrentLevel.stageNumber==2&&c.FirstSkipped&&c.Phase==Mission6_Phase_Jizhijin.Planning,"walking through the physical passage enters level two with zero solved words");Capture("campaign-11-direct-entry");Advance(41,.2f);break;
                case 41:
                    c.StartLevel(1,true);c.BeginExploration();c.mouseLook.enabled=c.headBob.enabled=false;pickup=0;Advance(5,0);break;
                case 5:
                    if(pickup==c.CurrentLevel.Data.pickups.Length){Pose(c.CurrentLevel.Data.Center(18,18,1.4f));Advance(7);break;}
                    var item=c.CurrentLevel.GetComponentsInChildren<Mission6_LetterPickup_Collectible_Jizhijin>().Single(t=>t.name.EndsWith("_"+pickup)&&t.GetComponent<Collider>().enabled);Pose(item.transform.position);Advance(6,.15f);break;
                case 6:Check(c.inventory.Letters.Count==pickup+1,"physical level one pickup "+(pickup+1));pickup++;Advance(5,0);break;
                case 7:
                    Check(c.PuzzleOpen&&Time.timeScale==0,"physical level one exit opens the crossword and pauses hazards");before=c.Elapsed;Puzzle.CheckAnswer();Check(Puzzle.CorrectWords==0&&Puzzle.nextLevelButton.GetComponent<Button>().interactable,"empty first-level answers still leave NEXT LEVEL clickable");
                    Check(Puzzle.SelectLetter(0,'K')&&Puzzle.letterSlots.Skip(1).All(slot=>slot.options.All(o=>o.text!="K")),"a selected unique letter disappears from every other dropdown");Puzzle.letterSlots[0].value=0;Check(Puzzle.letterSlots.Skip(1).All(slot=>slot.options.Any(o=>o.text=="K")),"clearing a square returns its letter to other dropdowns");Advance(8,.4f);break;
                case 8:
                    Check(c.Elapsed>before+.3f,"time spent answering counts toward the score");Check(FillExactly(Puzzle,7)&&Puzzle.Solved,"seven correct words pass with some blanks still empty");Capture("campaign-03-level1-partial-grade");Advance(9,.3f);break;
                case 9:
                    int handle=SceneManager.GetActiveScene().handle;Check(c.CompleteCurrentLevel()&&c.FirstScore.correctWords==7&&!c.FirstSkipped&&c.CurrentLevel.stageNumber==2&&SceneManager.GetActiveScene().handle==handle,"optional first-level solve retains its grade and directly enters level two in the same scene");Advance(10,.2f);break;
                case 10:
                    Key();Check(c.CurrentLevel==c.levels[1]&&c.Phase==Mission6_Phase_Jizhijin.Planning,"second level starts its own PLAN after direct entry");Check(c.inventory.Letters.Count==0&&!c.photo.Used&&!c.HazardsActive,"level two resets the bag and photo and keeps PLAN safe");var start=c.CurrentLevel.Data.Start;Pose(c.CurrentLevel.Data.Center(start.x,start.y,1.4f),0,-8);Advance(11);break;
                case 11:Capture("campaign-04-level2-plan");Advance(110,.2f);break;
                case 110:Pose(c.CurrentLevel.Data.Center(3,5,1.4f));Key(UnityEngine.InputSystem.Key.W);Advance(111,.8f);break;
                case 111:Key();Check(c.Phase==Mission6_Phase_Jizhijin.Exploring,"walking past model B PLAN board starts level two early");c.mouseLook.enabled=c.headBob.enabled=false;position=c.enemies[0].transform.position;Advance(12,1);break;
                case 12:
                    Check(Vector3.Distance(position,c.enemies[0].transform.position)>.3f,"sentinel follows its patrol route when level two starts");Check(c.photo.TryUsePhoto(),"level two restores one photo use");Advance(13,.2f);break;
                case 13:
                    Capture("campaign-05-level2-photo");Advance(130,.2f);break;
                case 130:c.photo.Close();FaceEnemy();Advance(14,.35f);break;
                case 14:
                    Check(c.enemies[0].CanSeePlayer()&&c.enemies[0].Chasing,"sentinel sees an exposed player and starts chasing");
                    Capture("campaign-09-sentinel-chase");Advance(140,.2f);break;
                case 140:
                    StopEnemies();Occlusion();
                    Pose(c.enemies[0].transform.position+new Vector3(0,1.4f,.7f));c.enemies[0].enabled=true;Advance(15,.25f);break;
                case 15:
                    Check(c.Phase==Mission6_Phase_Jizhijin.Failed&&c.deathPanel.activeSelf&&!c.moving.enabled&&Time.timeScale==0,"catching the player opens failure UI and stops control");Capture("campaign-06-sentinel-capture");Advance(16,.25f);break;
                case 16:c.Replay();Advance(17,1.3f);break;
                case 17:
                    c=UnityEngine.Object.FindFirstObjectByType<Mission6_Campaign_Jizhijin>();Check(c.CurrentLevel.stageNumber==2&&c.FirstScore.correctWords==7&&c.Phase==Mission6_Phase_Jizhijin.Planning&&!c.photo.Used,"retry restores the level two checkpoint and retains the level one grade");
                    c.BeginExploration();StopEnemies();c.mouseLook.enabled=c.headBob.enabled=false;Check(!c.night.IsNight&&c.night.CycleTime<.1f,"retry starts a fresh thirty-second day");FacePickup(0);Advance(170,29.5f);break;
                case 170:
                    Check(!c.night.IsNight&&c.night.SecondsLeft<1,"second level remains day until thirty seconds");Capture("campaign-12-day-before-night");Advance(171,.7f);break;
                case 171:
                    Check(c.night.IsNight&&c.night.SecondsLeft>9&&RenderSettings.fog&&RenderSettings.fogEndDistance==11,"thirty seconds triggers a ten-second night with reduced visibility");
                    Check(c.night.letterRenderers.Where(r=>r!=null).All(r=>!r.enabled)&&c.night.countdown.gameObject.activeSelf&&c.night.countdown.text.Contains("NIGHT ENDS"),"night hides all scene letters and shows its upper countdown");
                    c.enemies[0].enabled=true;Advance(172,.15f);break;
                case 172:
                    Check(Mathf.Abs(c.enemies[0].Agent.speed-(c.enemies[0].Chasing?c.enemies[0].chaseSpeed:c.enemies[0].patrolSpeed)*1.6f)<.01f,"enemy movement is sixty percent faster at night");StopEnemies();Capture("campaign-13-night-maze");Advance(1721,.2f);break;
                case 1721:Pose(c.CurrentLevel.Data.Center(c.CurrentLevel.Data.pickups[0].x,c.CurrentLevel.Data.pickups[0].y,1.4f));Advance(173,.15f);break;
                case 173:
                    Check(c.inventory.Letters.Count==0,"invisible night letters cannot be accidentally collected");Check(c.photo.TryUsePhoto()&&c.night.photoMap.texture==c.night.nightMap,"photo also conceals letter positions and letters at night");Advance(174,.2f);break;
                case 174:
                    Capture("campaign-14-night-photo");Advance(1741,.2f);break;
                case 1741:c.OpenPuzzle(c.CurrentLevel);before=c.night.CycleTime;Advance(175,.6f);break;
                case 175:
                    Check(Mathf.Abs(c.night.CycleTime-before)<.03f&&c.PuzzleOpen,"answer panel pauses the night cycle together with enemies");c.ClosePuzzle();Advance(176,c.night.SecondsLeft+.2f);break;
                case 176:
                    Check(!c.night.IsNight&&!RenderSettings.fog&&c.night.photoMap.texture==c.night.dayMap&&c.inventory.Letters.Count==1,"ten-second night restores daylight, photo letters and standing-on-letter collection");
                    Check(c.night.letterRenderers.Where(r=>r!=null).All(r=>r.enabled),"remaining world letters reappear at dawn");c.enemies[0].enabled=true;Advance(177,.2f);break;
                case 177:
                    Check(Mathf.Abs(c.enemies[0].Agent.speed-(c.enemies[0].Chasing?c.enemies[0].chaseSpeed:c.enemies[0].patrolSpeed))<.01f,"enemy returns to normal speed after night");StopEnemies();Pose(c.CurrentLevel.Data.Center(1,1,1.4f));Advance(178,c.night.SecondsLeft+.2f);break;
                case 178:
                    Check(c.night.IsNight,"a second night recurs after another thirty-second day");c.night.ResetCycle();Check(!c.night.IsNight&&!RenderSettings.fog,"resetting the level cycle restores day");pickup=1;Advance(18,0);break;
                case 18:
                    if(pickup==c.CurrentLevel.Data.pickups.Length){var exit=c.CurrentLevel.Data.Exit;Pose(c.CurrentLevel.Data.Center(exit.x,exit.y,1.4f));Advance(20);break;}
                    var tile=c.CurrentLevel.GetComponentsInChildren<Mission6_LetterPickup_Collectible_Jizhijin>().Single(t=>t.name.EndsWith("_"+pickup)&&t.GetComponent<Collider>().enabled);Pose(tile.transform.position);Advance(19,.12f);break;
                case 19:Check(c.inventory.Letters.Count==pickup+1,"physical level two pickup "+(pickup+1));pickup++;Advance(18,0);break;
                case 20:
                    Check(c.PuzzleOpen&&Puzzle.letterSlots.Length==12,"level two exit displays its larger crossword with twelve missing squares");Check(FillExactly(Puzzle,10)&&!c.CompleteCurrentLevel(),"ten of fifteen words cannot finish level two");Check(FillExactly(Puzzle,11)&&Puzzle.Solved,"eleven of fifteen words meets the second level threshold");Capture("campaign-07-level2-partial-grade");Advance(21,.3f);break;
                case 21:
                    Puzzle.ClearAnswers();var repeated=c.inventory.Letters.GroupBy(v=>v).First(g=>g.Count()>1);int copies=repeated.Count();
                    for(int i=0;i<copies;i++)Check(Puzzle.SelectLetter(i,repeated.Key[0]),"remaining duplicate letter copy "+i+" stays selectable");
                    Check(Puzzle.letterSlots[copies].options.All(o=>!o.text.StartsWith(repeated.Key))&&Puzzle.SelectedTiles.Where(i=>i>=0).Distinct().Count()==copies,"using every copy removes that letter from all other slots");
                    Puzzle.letterSlots[0].value=0;Check(Puzzle.letterSlots[copies].options.Any(o=>o.text==repeated.Key),"clearing a duplicate restores exactly one available copy");
                    Puzzle.ClearAnswers();Puzzle.letterSlots[0].Show();Advance(210,.2f);break;
                case 210:
                    var menu=Puzzle.letterSlots[0].transform.Find("Dropdown List");Check(menu!=null,"full bag letter menu opens");var scroll=menu.GetComponent<ScrollRect>();Canvas.ForceUpdateCanvases();Check(scroll.verticalScrollbar!=null&&scroll.verticalScrollbar.gameObject.activeInHierarchy&&scroll.content.rect.height>scroll.viewport.rect.height,"full bag menu has a working visible scrollbar");scroll.verticalNormalizedPosition=0;Advance(211,.2f);break;
                case 211:
                    var list=Puzzle.letterSlots[0].transform.Find("Dropdown List").GetComponent<ScrollRect>();var toggles=list.content.GetComponentsInChildren<Toggle>();var last=toggles.Last(t=>t.gameObject.activeInHierarchy);var lastText=last.GetComponentsInChildren<TMP_Text>().First(t=>t.name=="Letter");var local=list.viewport.InverseTransformPoint(lastText.rectTransform.TransformPoint(lastText.rectTransform.rect.center));Check(lastText.text==Puzzle.letterSlots[0].options.Last().text&&list.viewport.rect.Contains(new Vector2(local.x,local.y))&&lastText.rectTransform.rect.width>50,"last collected letter remains readable after scrolling");Capture("campaign-10-full-bag-menu");Advance(212,.2f);break;
                case 212:
                    var choice=Puzzle.letterSlots[0].transform.Find("Dropdown List").GetComponent<ScrollRect>().content.GetComponentsInChildren<Toggle>().Last(t=>t.gameObject.activeInHierarchy);string lastOption=Puzzle.letterSlots[0].options.Last().text;choice.isOn=true;Check(Puzzle.SelectedTiles[0]>=0&&c.inventory.Letters[Puzzle.SelectedTiles[0]][0]==lastOption[0],"last collected letter can be selected from the menu");Puzzle.letterSlots[0].Hide();Fill(Puzzle,(1<<Puzzle.slotCells.Length)-1);Check(Puzzle.CorrectWords==15,"all fifteen crossing words can be completed with the collected physical tiles");Advance(22,.3f);break;
                case 22:
                    Check(c.CompleteCurrentLevel()&&c.Phase==Mission6_Phase_Jizhijin.Finished&&c.finishPanel.activeSelf,"finishing level two displays campaign results");Check(c.FirstScore.correctWords==7&&c.SecondScore.correctWords==15&&c.finishText.text.Contains("TOTAL"),"results show each level's time, word count, and combined grade");Capture("campaign-08-final-results");Advance(23,.3f);break;
                case 23:Finish();break;
            }
        }
        catch(Exception e){results.Add("FAIL: "+e);Finish();}
    }
}
