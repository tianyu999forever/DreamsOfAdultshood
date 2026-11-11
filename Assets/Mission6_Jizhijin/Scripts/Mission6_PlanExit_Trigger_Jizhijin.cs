using UnityEngine;

public class Mission6_PlanExit_Trigger_Jizhijin : MonoBehaviour
{
    public Mission6_Grid_Level_Jizhijin level;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
            level.BeginExploration();
    }
}
