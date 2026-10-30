using System.Linq;
using TMPro;
using UnityEngine;

// Event-driven introduction. Looking at the map never reveals item ownership or the hidden door.
public class mission3Guide_UI_ShengChiehLee : MonoBehaviour
{
    public mission3Hotel_Manager_ShengChiehLee manager;
    public GameObject objectiveCard;
    public TMP_Text objective,controls;
    public bool MapViewed { get; private set; }
    public bool JournalViewed { get; private set; }
    bool firstPickup,letterReminder;
    public string ObjectiveText
    {
        get
        {
            if(manager.IsCompleted)return "SHIFT COMPLETE\nYour belongings have been returned.";
            if(!MapViewed)return "01 / GET YOUR BEARINGS\nInspect the lobby map with E, or press M.";
            if(!manager.notesRead.Any(x=>x))return "02 / MEET THE GUESTS\nRead a guest note. Start with 301 / west wing.";
            if(!JournalViewed)return "03 / KEEP THE CLUES\nPress TAB to open your saved guest journal.";
            if(manager.CollectedCount==0)return "04 / FIND A BELONGING\nFind a cyan number on the map. E picks it up.";
            if(manager.CollectedCount<3)return "05 / SEARCH THE HOTEL\nFind at least 3 belongings: "+manager.CollectedCount+" / 3.";
            return "06 / RETURN TO RECEPTION\nReturn your belongings, or find all 6 for more points.";
        }
    }
    public void PageOpened(mission3Hotel_Manager_ShengChiehLee.Page page)
    {
        if(page==mission3Hotel_Manager_ShengChiehLee.Page.Map)MapViewed=true;
        if(page==mission3Hotel_Manager_ShengChiehLee.Page.Journal&&manager.notesRead.Any(x=>x))JournalViewed=true;
        if(page==mission3Hotel_Manager_ShengChiehLee.Page.Note)letterReminder=true;
        if(page==mission3Hotel_Manager_ShengChiehLee.Page.None&&letterReminder)
        {
            letterReminder=false;manager.ShowHint("Guest note saved. TAB opens your journal. A belonging's location does not tell you its owner.");
        }
        Refresh();
    }
    public void ItemCollected()
    {
        if(firstPickup)return;firstPickup=true;
        manager.ShowHint("In your bag! Match it to a guest's clues. Where you found it is not necessarily who owns it.");
    }
    void Update(){Refresh();}
    public void Refresh()
    {
        if(manager==null)return;
        if(objectiveCard!=null)objectiveCard.SetActive(manager.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.None);
        if(objective!=null)objective.text=ObjectiveText;
        if(controls!=null)controls.text=manager.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.None?
            (!MapViewed?"WASD  MOVE     M  FLOOR PLAN     E  INTERACT     ESC  PAUSE":"E  INTERACT     M  MAP     TAB  JOURNAL     Q  WATCH     ESC  PAUSE"):
            "WASD  MOVE     SHIFT  RUN     E  INTERACT     M  MAP     TAB  JOURNAL     Q  WATCH     ESC  PAUSE";
    }
}
