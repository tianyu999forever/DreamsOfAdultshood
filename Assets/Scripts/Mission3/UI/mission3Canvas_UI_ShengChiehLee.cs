using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Scene-authored Canvas: Mission1's hierarchy, wired only to Mission3's state.
public class mission3Canvas_UI_ShengChiehLee : MonoBehaviour
{
    public mission3Hotel_Manager_ShengChiehLee manager;
    public GameObject backdrop;
    public GameObject[] pages;
    public TMP_Text status, bag, prompt, notice, clock, note, journal, summary, result;
    public Button reviewButton;
    public Button[] ownerButtons;
    public RectTransform mapRect, playerMarker;
    public Image[] itemMarkers, noteMarkers;
    public Transform[] itemPositions, notePositions;
    public TMP_Text[] itemRows;
    public Vector2 mapWorldMin = new Vector2(-1.5f,-61.5f);
    public Vector2 mapWorldMax = new Vector2(61.5f,1.5f);
    public Vector2 mapPixelMin = new Vector2(72,72);
    public Vector2 mapPixelMax = new Vector2(952,952);
    int lastPage = -1;
    static readonly Color Cyan = new Color(.03f,.72f,.87f);
    static readonly Color Gold = new Color(.79f,.60f,.29f);

    void Awake()
    {
        for(int n=0;n<ownerButtons.Length;n++)
        {
            int item=n/3, owner=n%3;
            ownerButtons[n].onClick.AddListener(()=>Assign(item,owner));
        }
    }
    void LateUpdate()
    {
        if(manager == null) return;
        Refresh();
    }
    public void Refresh()
    {
        int page=(int)manager.CurrentPage;
        if(page!=lastPage)
        {
            backdrop.SetActive(page!=0);
            for(int i=0;i<pages.Length;i++) if(pages[i]!=null) pages[i].SetActive(i==page);
            lastPage=page;
        }
        status.text="FLOOR 03  /  "+manager.CollectedCount+" OF 6 BELONGINGS  /  "+manager.notesRead.Count(x=>x)+" OF 3 NOTES";
        bag.text="BAG   "+(manager.CollectedCount==0?"Find the six lost belongings":string.Join("   /   ",Enumerable.Range(0,6).Where(i=>manager.collected[i]).Select(i=>mission3Rules_Manager_ShengChiehLee.Items[i])));
        prompt.text=page==0?manager.FocusPrompt:"";
        notice.text=manager.Notice;
        clock.text=System.TimeSpan.FromSeconds(manager.ElapsedSeconds).ToString(@"mm\:ss");
        note.text=manager.CurrentNote;
        journal.text=string.Join("\n\n",Enumerable.Range(0,3).Select(g=>manager.notesRead[g]?mission3Rules_Manager_ShengChiehLee.Notes[g]:(301+g)+" / NOTE NOT READ\nFind the guest's letter in their room."));
        summary.text="RETURN LOST PROPERTY  /  "+manager.CollectedCount+" OF 6\nChoose an owner for each belonging. Final confirmation locks your score.";
        for(int i=0;i<6;i++)
        {
            itemRows[i].text=(i+1).ToString("00")+"  "+mission3Rules_Manager_ShengChiehLee.Items[i]+(manager.collected[i]?"":"  / NOT FOUND");
            itemRows[i].color=manager.collected[i]?new Color(.91f,.88f,.78f):new Color(.48f,.51f,.53f);
            for(int g=0;g<3;g++)
            {
                var button=ownerButtons[i*3+g];
                button.interactable=manager.collected[i]&&!manager.IsCompleted;
                button.GetComponent<Image>().color=manager.assignment[i]==g?Cyan:new Color(.16f,.20f,.22f);
            }
        }
        reviewButton.interactable=manager.CollectedCount>=3 && Enumerable.Range(0,6).All(i=>!manager.collected[i]||manager.assignment[i]>=0);
        for(int i=0;i<itemMarkers.Length;i++)
        {
            if(itemPositions[i]!=null) Place(itemMarkers[i].rectTransform,itemPositions[i].position);
            itemMarkers[i].color=manager.collected[i]?new Color(.25f,.52f,.49f,.7f):Cyan;
            itemMarkers[i].GetComponentInChildren<TMP_Text>().text=manager.collected[i]?"+":(i+1).ToString();
        }
        for(int i=0;i<noteMarkers.Length;i++)
        {
            Place(noteMarkers[i].rectTransform,notePositions[i].position);
            noteMarkers[i].color=manager.notesRead[i]?new Color(.42f,.57f,.43f):Gold;
        }
        Place(playerMarker,manager.playerBody.position);
        playerMarker.localEulerAngles=new Vector3(0,0,-manager.playerBody.rotation.eulerAngles.y);
        if(manager.IsCompleted) result.text=manager.ResultDescription;
    }
    void Place(RectTransform marker,Vector3 world)
    {
        var uv=new Vector2(Mathf.InverseLerp(mapWorldMin.x,mapWorldMax.x,world.x),Mathf.InverseLerp(mapWorldMin.y,mapWorldMax.y,world.z));
        var pixel=mapPixelMin+Vector2.Scale(uv,mapPixelMax-mapPixelMin);
        marker.anchorMin=marker.anchorMax=pixel/1024f; marker.anchoredPosition=Vector2.zero;
    }
    void Assign(int item,int owner) { if(manager.collected[item]&&!manager.IsCompleted) manager.assignment[item]=owner; }
    public void Begin() { manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.None); }
    public void Map() { manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Map); }
    public void Journal() { manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Journal); }
    public void Pause() { manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Pause); }
    public void Review() { if(reviewButton.interactable) manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Confirm); }
    public void Edit() { manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Checkout); }
    public void Finish() { manager.CompleteCheckout(); }
    public void Restart() { manager.Restart(); }
    public void Continue() { manager.LoadNextLevel(); }
}
