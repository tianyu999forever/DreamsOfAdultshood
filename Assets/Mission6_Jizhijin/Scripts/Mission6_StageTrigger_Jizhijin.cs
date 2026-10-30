using UnityEngine;
using UnityEngine.InputSystem;
public class Mission6_StageTrigger_Jizhijin : MonoBehaviour
{
    public Mission6_Grid_Level_Jizhijin level;
    public int action; // 0: leave PLAN, 1: exit puzzle, 2: enter level two
    bool inside;
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player")) return;
        inside = true;
        if (action == 0) level.BeginExploration();
        else if (action == 1) level.campaign.OpenPuzzle(level);
        else level.campaign.EnterSecondLevel();
    }
    void OnTriggerExit(Collider other) { if (other.CompareTag("Player") || other.transform.root.CompareTag("Player")) inside = false; }
    void Update() { if (inside && action == 1 && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) level.campaign.OpenPuzzle(level); }
}
