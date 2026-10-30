using UnityEngine;
public class Mission6_Grid_Level_Jizhijin : MonoBehaviour
{
    public int stageNumber = 1;
    public Mission6_Campaign_Jizhijin campaign;
    public GameObject planPanel, photoPanel;
    public TMPro.TMP_Text photoTitle;
    public Mission6_StageDefinition_Jizhijin Data => stageNumber == 1 ? Mission6_StageDefinition_Jizhijin.First : Mission6_StageDefinition_Jizhijin.Second;
    public bool IsPlanning => campaign != null && campaign.CurrentLevel == this && campaign.Phase == Mission6_Phase_Jizhijin.Planning;
    public void BeginExploration() { if (campaign != null && campaign.CurrentLevel == this) campaign.BeginExploration(); }
}
