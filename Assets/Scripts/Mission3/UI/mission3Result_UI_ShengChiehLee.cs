using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A scene-independent prefab; assign this mission's manager on the scene instance.
public class mission3Result_UI_ShengChiehLee : MonoBehaviour
{
    public mission3Hotel_Manager_ShengChiehLee manager;
    public TMP_Text totalScore, summary, itemPoints, pairPoints, explorationPoints;
    public TMP_Text itemDetail, pairDetail, explorationDetail, nextLevelHint;
    public TMP_Text[] ownerLabels, itemStates;
    public Button continueButton, replayButton;
    static readonly Color Success=new Color(.28f,.82f,.67f),Warning=new Color(1,.69f,.38f),Muted=new Color(.57f,.64f,.64f);

    void OnEnable(){Refresh();}
    public void Refresh()
    {
        if(continueButton==null||replayButton==null)return;
        bool settled=manager!=null&&manager.IsCompleted;
        continueButton.interactable=settled&&manager.CanEnterNextLevel;
        replayButton.interactable=settled;
        if(!settled)return;
        var result=manager.Settlement;
        totalScore.text=result.score.ToString();
        summary.text="BELONGINGS FOUND   "+result.collected+" / 6\nCORRECT RETURNS      "+result.correct+" / 6\nGUEST PAIRS                 "+result.sets+" / 3";
        itemPoints.text="+"+result.itemPoints;pairPoints.text="+"+result.setPoints;explorationPoints.text="+"+result.explorationPoints;
        itemDetail.text=result.correct+" correct returns x 10";pairDetail.text=result.sets+" complete pairs x 10";
        explorationDetail.text=manager.ShortcutOpened?"Service shortcut discovered":"Service shortcut not discovered";
        for(int i=0;i<6;i++)
        {
            ownerLabels[i].text=(301+mission3Rules_Manager_ShengChiehLee.Owner(i)).ToString();
            bool found=manager.collected[i],correct=found&&manager.assignment[i]==mission3Rules_Manager_ShengChiehLee.Owner(i);
            itemStates[i].text=!found?"NOT FOUND  /  +0":correct?"RETURNED  /  +10":"WRONG OWNER  /  +0";
            itemStates[i].color=!found?Muted:correct?Success:Warning;
        }
        nextLevelHint.text=manager.NextLevelHint;
    }
    public void Continue(){if(manager!=null)manager.LoadNextLevel();}
    public void Replay(){if(manager!=null&&manager.IsCompleted)manager.Restart();}
}
