using UnityEngine;

public class Mission2_EscapeStart_Trigger_TianyuTang : MonoBehaviour
{
    public Mission2_EscapeScore_GameManager_TianyuTang scoreManager;

    void OnTriggerEnter(Collider other)
    {
        bool isPlayer =
            other.CompareTag("Player") ||
            other.transform.root.CompareTag("Player");

        if (isPlayer && scoreManager != null)
            scoreManager.StartEscape();
    }
}