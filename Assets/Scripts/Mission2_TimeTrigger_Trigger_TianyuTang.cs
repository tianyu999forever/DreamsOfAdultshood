using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Mission2_TimeTrigger_Trigger_TianyuTang : MonoBehaviour
{
    public enum TriggerAction { StartEscape, FinishEscape }

    [SerializeField] private TriggerAction action;
    [SerializeField] private Mission2_TimeScore_GameManager_TianyuTang manager;

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Start()
    {
        if (manager == null)
            manager = FindFirstObjectByType<Mission2_TimeScore_GameManager_TianyuTang>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform candidate = other.transform;
        while (candidate != null && !candidate.CompareTag("Player"))
            candidate = candidate.parent;

        if (candidate == null || manager == null)
            return;

        if (action == TriggerAction.StartEscape)
            manager.BeginEscape();
        else
            manager.FinishEscape();
    }
}
