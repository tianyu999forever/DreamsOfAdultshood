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
    static Mission6_Campaign_Jizhijin c; static Keyboard keyboard; static int stage,pickup; static float next,deadline,before;static Vector3 position;static bool background;
    static Mission6_Verify_Jizhijin()
    {
        EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;
        Application.logMessageReceived+=(m,t,type)=>{if(SessionState.GetBool(Flag,false)&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))errors.Add(m+"\n"+t);};
    }
    [MenuItem("Mission6 Jizhijin/Verify two level campaign")]
    public static void Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        SessionState.SetBool(Flag,true);SessionState.SetString(Flag+".previous",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(Mission6_Setup_Jizhijin.ScenePath);EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Flag,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){stage=0;next=Time.realtimeSinceStartup+1;deadline=Time.realtimeSinceStartup+120;background=Application.runInBackground;Application.runInBackground=true;}
        if(state==PlayModeStateChange.EnteredEditMode){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Flag+".previous",""));SessionState.SetBool(Flag,false);Write();}
    }
    static void Write(){Directory.CreateDirectory(Out);File.WriteAllLines(Out+"campaign-verification.txt",results.Concat(errors.Select(e=>"RUNTIME ERROR: "+e)));}
    static void Check(bool ok,string name){results.Add((ok?"PASS: ":"FAIL: ")+name);Write();if(!ok)throw new Exception(name);}
    static void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.GetFullPath(Out+name+".png"));
    static void Key(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
    static void Pose(Vector3 p,float yaw=0,float pitch=0){var rb=c.moving.GetComponent<Rigidbody>();rb.position=p;rb.rotation=Quaternion.Euler(0,yaw,0);rb.linearVelocity=Vector3.zero;c.mouseLook.enabled=c.headBob.enabled=false;Camera.main.transform.localRotation=Quaternion.Euler(pitch,0,0);Physics.SyncTransforms();}
    static void Advance(int value,float delay=.25f){stage=value;next=Time.realtimeSinceStartup+delay;}
    static void Finish(){if(keyboard!=null)InputSystem.RemoveDevice(keyboard);Time.timeScale=1;Application.runInBackground=background;results.Add(errors.Count==0?"PASS: no runtime errors":"FAIL: runtime errors");Write();stage=100;EditorApplication.ExitPlaymode();}
    static Mission6_Spelling_ExitPanel_Jizhijin Puzzle=>c.puzzles[c.CurrentLevel.stageNumber-1];
    static void Fill(Mission6_Spelling_ExitPanel_Jizhijin p,int mask)
    {
        var used=new HashSet<int>();foreach(var slot in p.letterSlots)slot.SetValueWithoutNotify(0);
        for(int i=0;i<p.slotCells.Length;i++)if((mask&(1<<i))!=0)
        {string letter=p.level.Data.crossword[p.slotCells[i]].answer.ToString();int index=Enumerable.Range(0,c.inventory.Letters.Count).First(j=>!used.Contains(j)&&c.inventory.Letters[j]==letter);used.Add(index);p.letterSlots[i].SetValueWithoutNotify(index+1);p.letterSlots[i].RefreshShownValue();}
        p.CheckAnswer();
    }
    static bool FillExactly(Mission6_Spelling_ExitPanel_Jizhijin p,int target)
    {for(int mask=0;mask<(1<<p.slotCells.Length);mask++){Fill(p,mask);if(p.CorrectWords==target)return true;}return false;}
    static void Layout(Mission6_Grid_Level_Jizhijin level)
    {
        var d=level.Data;Check(d.words.Length==(d.number==1?10:15)&&d.minimumWords==(d.number==1?7:11),"level "+d.number+" word total and 70 percent pass requirement");
        Check(level.transform.Cast<Transform>().Count(t=>t.name.StartsWith("Cell_"))==d.size*d.size,"level "+d.number+" has "+d.size+" x "+d.size+" tiles");
        var seen=new HashSet<Vector2Int>{d.Start};var queue=new Queue<Vector2Int>();queue.Enqueue(d.Start);while(queue.Count>0){var p=queue.Dequeue();foreach(var dir in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}){var n=p+dir;if(!d.IsWall(n.x,n.y)&&seen.Add(n))queue.Enqueue(n);}}
        Check(d.pickups.All(seen.Contains)&&seen.Contains(d.Exit)&&d.crossword.Keys.All(seen.Contains),"level "+d.number+" pickups, word cells and exit are reachable");
        Check(d.blanks.GroupBy(p=>d.crossword[p].answer).All(g=>d.letters.Count(ch=>ch==g.Key)>=g.Count()),"level "+d.number+" has enough physical tiles for every missing letter");
        Check(d.words.All(w=>Enumerable.Range(0,w.word.Length).Any(i=>d.crossword[w.CellAt(i)].missing)),"every level "+d.number+" word requires at least one player answer");
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mission6_Jizhijin/Materials/"+(d.number==1?"Mission6_GridMap_Jizhijin.png":"Mission6_GridMap_Level2_Jizhijin.png"));bool pixels=true;
        for(int row=0;row<d.size;row++)for(int x=0;x<d.size;x++){var sample=texture.GetPixel(60+x*44+1,60+row*44+1);var expected=d.crossword.ContainsKey(new Vector2Int(x,row))?Mission6_GridMap_Jizhijin.WallColor:Mission6_GridMap_Jizhijin.TerrainColor(d,x,row);pixels&=Mathf.Abs(sample.r-expected.r)<.015f&&Mathf.Abs(sample.g-expected.g)<.015f;}
        Check(pixels,"level "+d.number+" map agrees with every terrain and crossword cell");
        Check(level.GetComponentsInChildren<Mission6_LetterPickup_Collectible_Jizhijin>().Length==d.pickups.Length,"level "+d.number+" pickup count matches its map");
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
                    Check(!RenderSettings.fog&&!c.GateOpen&&c.gateCollider.enabled,"clear view and closed physical gate at start");
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
                    Check(!c.photo.IsVisible&&c.photo.Used&&c.Elapsed>before+5,"photo closes after five seconds while clear time keeps running");pickup=0;Advance(5,0);break;
                case 5:
                    if(pickup==c.CurrentLevel.Data.pickups.Length){Pose(c.CurrentLevel.Data.Center(18,18,1.4f));Advance(7);break;}
                    var item=c.CurrentLevel.GetComponentsInChildren<Mission6_LetterPickup_Collectible_Jizhijin>().Single(t=>t.name.EndsWith("_"+pickup)&&t.GetComponent<Collider>().enabled);Pose(item.transform.position);Advance(6,.15f);break;
                case 6:Check(c.inventory.Letters.Count==pickup+1,"physical level one pickup "+(pickup+1));pickup++;Advance(5,0);break;
                case 7:
                    Check(c.PuzzleOpen&&Time.timeScale==0,"physical level one exit opens the crossword and pauses hazards");before=c.Elapsed;Puzzle.CheckAnswer();Check(Puzzle.CorrectWords==0&&!c.CompleteCurrentLevel()&&!c.GateOpen,"empty answers do not pass and cannot open the gate");Advance(8,.4f);break;
                case 8:
                    Check(c.Elapsed>before+.3f,"time spent answering counts toward the score");Check(FillExactly(Puzzle,7)&&Puzzle.Solved,"seven correct words pass with some blanks still empty");Capture("campaign-03-level1-partial-grade");Advance(9,.3f);break;
                case 9:
                    int handle=SceneManager.GetActiveScene().handle;Check(c.CompleteCurrentLevel()&&c.FirstScore.correctWords==7&&c.GateOpen&&!c.gateCollider.enabled&&SceneManager.GetActiveScene().handle==handle,"passing level one records a grade and opens the door without changing scenes");Pose(new Vector3(46.5f,1.4f,44.4f),90);Key(UnityEngine.InputSystem.Key.W);Advance(10,2.9f);break;
                case 10:
                    Key();Check(c.CurrentLevel==c.levels[1]&&c.Phase==Mission6_Phase_Jizhijin.Planning,"player walks through the physical doorway and corridor into level two");Check(c.inventory.Letters.Count==0&&!c.photo.Used&&!c.HazardsActive,"level two resets the bag and photo and keeps PLAN safe");Pose(c.CurrentLevel.Data.Center(1,1,1.4f),0,-8);Advance(11);break;
                case 11:Capture("campaign-04-level2-plan");Advance(110,.2f);break;
                case 110:c.BeginExploration();c.mouseLook.enabled=c.headBob.enabled=false;position=c.enemies[0].transform.position;Advance(12,1);break;
                case 12:
                    Check(Vector3.Distance(position,c.enemies[0].transform.position)>.3f,"sentinel follows its patrol route when level two starts");Check(c.photo.TryUsePhoto(),"level two restores one photo use");Advance(13,.2f);break;
                case 13:
                    Capture("campaign-05-level2-photo");Advance(130,.2f);break;
                case 130:c.photo.Close();Pose(c.enemies[0].transform.position+new Vector3(0,1.4f,4),180,8);Advance(14,.35f);break;
                case 14:
                    Check(c.enemies[0].CanSeePlayer()&&c.enemies[0].Chasing,"sentinel sees an exposed player and starts chasing");
                    Capture("campaign-09-sentinel-chase");Advance(140,.2f);break;
                case 140:
                    foreach(var enemy in c.enemies){enemy.Agent.isStopped=true;enemy.Agent.ResetPath();enemy.enabled=false;}
                    c.enemies[0].Agent.Warp(c.CurrentLevel.Data.Center(20,8));Pose(c.CurrentLevel.Data.Center(20,12,1.4f));Physics.SyncTransforms();Check(!c.enemies[0].CanSeePlayer(),"a solid maze wall blocks enemy line of sight");
                    Pose(c.enemies[0].transform.position+new Vector3(0,1.4f,.7f));c.enemies[0].enabled=true;Advance(15,.25f);break;
                case 15:
                    Check(c.Phase==Mission6_Phase_Jizhijin.Failed&&c.deathPanel.activeSelf&&!c.moving.enabled&&Time.timeScale==0,"catching the player opens failure UI and stops control");Capture("campaign-06-sentinel-capture");Advance(16,.25f);break;
                case 16:c.Replay();Advance(17,1.3f);break;
                case 17:
                    c=UnityEngine.Object.FindFirstObjectByType<Mission6_Campaign_Jizhijin>();Check(c.CurrentLevel.stageNumber==2&&c.FirstScore.correctWords==7&&c.Phase==Mission6_Phase_Jizhijin.Planning&&!c.photo.Used,"retry restores the level two checkpoint and retains the level one grade");
                    c.BeginExploration();foreach(var enemy in c.enemies){enemy.Agent.isStopped=true;enemy.enabled=false;}c.mouseLook.enabled=c.headBob.enabled=false;pickup=0;Advance(18,0);break;
                case 18:
                    if(pickup==c.CurrentLevel.Data.pickups.Length){Pose(c.CurrentLevel.Data.Center(26,26,1.4f));Advance(20);break;}
                    var tile=c.CurrentLevel.GetComponentsInChildren<Mission6_LetterPickup_Collectible_Jizhijin>().Single(t=>t.name.EndsWith("_"+pickup)&&t.GetComponent<Collider>().enabled);Pose(tile.transform.position);Advance(19,.12f);break;
                case 19:Check(c.inventory.Letters.Count==pickup+1,"physical level two pickup "+(pickup+1));pickup++;Advance(18,0);break;
                case 20:
                    Check(c.PuzzleOpen&&Puzzle.letterSlots.Length==12,"level two exit displays its larger crossword with twelve missing squares");Check(FillExactly(Puzzle,10)&&!c.CompleteCurrentLevel(),"ten of fifteen words cannot finish level two");Check(FillExactly(Puzzle,11)&&Puzzle.Solved,"eleven of fifteen words meets the second level threshold");Capture("campaign-07-level2-partial-grade");Advance(21,.3f);break;
                case 21: Puzzle.letterSlots[0].Show();Advance(210,.2f);break;
                case 210:
                    var menu=Puzzle.letterSlots[0].transform.Find("Dropdown List");Check(menu!=null,"full bag letter menu opens");var scroll=menu.GetComponent<ScrollRect>();Canvas.ForceUpdateCanvases();Check(scroll.verticalScrollbar!=null&&scroll.verticalScrollbar.gameObject.activeInHierarchy&&scroll.content.rect.height>scroll.viewport.rect.height,"full bag menu has a working visible scrollbar");scroll.verticalNormalizedPosition=0;Advance(211,.2f);break;
                case 211:
                    var list=Puzzle.letterSlots[0].transform.Find("Dropdown List").GetComponent<ScrollRect>();var toggles=list.content.GetComponentsInChildren<Toggle>();var last=toggles.Last(t=>t.gameObject.activeInHierarchy);var lastText=last.GetComponentsInChildren<TMP_Text>().First(t=>t.name=="Letter");var local=list.viewport.InverseTransformPoint(lastText.rectTransform.TransformPoint(lastText.rectTransform.rect.center));Check(lastText.text==c.inventory.Letters.Last()&&list.viewport.rect.Contains(new Vector2(local.x,local.y))&&lastText.rectTransform.rect.width>50,"last collected letter remains readable after scrolling");Capture("campaign-10-full-bag-menu");Advance(212,.2f);break;
                case 212:
                    var choice=Puzzle.letterSlots[0].transform.Find("Dropdown List").GetComponent<ScrollRect>().content.GetComponentsInChildren<Toggle>().Last(t=>t.gameObject.activeInHierarchy);Puzzle.letterSlots[0].SetValueWithoutNotify(0);choice.isOn=true;Check(Puzzle.letterSlots[0].value==c.inventory.Letters.Count,"last collected letter can be selected from the menu");Puzzle.letterSlots[0].Hide();Fill(Puzzle,(1<<Puzzle.slotCells.Length)-1);Check(Puzzle.CorrectWords==15,"all fifteen crossing words can be completed with the collected physical tiles");Advance(22,.3f);break;
                case 22:
                    Check(c.CompleteCurrentLevel()&&c.Phase==Mission6_Phase_Jizhijin.Finished&&c.finishPanel.activeSelf,"finishing level two displays campaign results");Check(c.FirstScore.correctWords==7&&c.SecondScore.correctWords==15&&c.finishText.text.Contains("TOTAL"),"results show each level's time, word count, and combined grade");Capture("campaign-08-final-results");Advance(23,.3f);break;
                case 23:Finish();break;
            }
        }
        catch(Exception e){results.Add("FAIL: "+e);Finish();}
    }
}
