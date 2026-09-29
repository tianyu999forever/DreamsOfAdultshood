using UnityEngine;

public class StartTimerTrigger : MonoBehaviour
{
    public PocketWatch pocketWatch;
    public PlanningTimer planningTimer;

    private bool activated;

    private void Start()
    {
        if (pocketWatch == null)
        {
            pocketWatch =
                FindFirstObjectByType<PocketWatch>();
        }

        if (planningTimer == null)
        {
            planningTimer =
                FindFirstObjectByType<PlanningTimer>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        bool isPlayer =
            other.CompareTag("Player") ||
            other.transform.root.CompareTag("Player");

        if (!isPlayer || activated)
            return;

        if (pocketWatch == null)
        {
            Debug.LogError("not found PocketWatch");
            return;
        }

        activated = true;

        // 停止出生房间的思考倒计时
        if (planningTimer != null)
        {
            planningTimer.PlayerLeftStartRoom();
        }

        // 开始原来的迷宫倒计时
        pocketWatch.StartCountdown();

        Debug.Log("Player left the start room");

        Collider triggerCollider =
            GetComponent<Collider>();

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
    }
}