using UnityEngine;
using UnityEngine.AI;
// Adapted from Mission2_MonsterChase_Monster_TianyuTang.cs on main at 7205f39.
// Retains NavMesh patrol, last known position, pursuit, and lost-sight recovery.
// Mission 6 uses line-of-sight detection instead of Mission 2's flashlight dependency.
public class Mission6_Enemy_Jizhijin : MonoBehaviour
{
    public Mission6_Campaign_Jizhijin campaign;
    public Transform player;
    public Transform[] patrolPoints;
    public float patrolSpeed = 2, chaseSpeed = 3.6f, visionDistance = 9, loseSightTime = 2.5f, catchDistance = 1.15f;
    public bool Chasing { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    int patrolIndex; float lostSightTimer;
    Vector3 lastKnownPlayerPosition;
    public bool CanSeePlayer()
    {
        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 target = player.position + Vector3.up * .5f;
        Vector3 direction = target - origin;
        if (direction.magnitude > visionDistance) return false;
        if (Physics.Raycast(origin, direction.normalized, out var hit, direction.magnitude, ~0, QueryTriggerInteraction.Ignore))
            return hit.transform == player || hit.transform.IsChildOf(player);
        return false;
    }
    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        // The campaign adds its baked navigation during Awake, before agents start.
        Agent.enabled = true;
        if (NavMesh.SamplePosition(transform.position, out var hit, 3, NavMesh.AllAreas)) Agent.Warp(hit.position);
        Agent.speed = patrolSpeed;
    }
    void Update()
    {
        if (Agent == null || !Agent.isOnNavMesh || player == null) return;
        if (!campaign.HazardsActive) { Agent.isStopped = true; return; }
        Agent.isStopped = false;
        bool visible = CanSeePlayer();
        if (visible) { Chasing = true; lastKnownPlayerPosition = player.position; lostSightTimer = 0; }
        else if (Chasing) { lostSightTimer += Time.deltaTime; if (lostSightTimer >= loseSightTime) Chasing = false; }
        if (Chasing) { Agent.speed = chaseSpeed; Agent.acceleration = 22; Agent.SetDestination(lastKnownPlayerPosition); }
        else if (patrolPoints.Length > 0)
        {
            Agent.speed = patrolSpeed; Agent.acceleration = 10; Agent.SetDestination(patrolPoints[patrolIndex].position);
            if (!Agent.pathPending && Agent.remainingDistance <= .5f) patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }
        Vector3 a = transform.position, b = player.position; a.y = b.y = 0;
        if (visible && Vector3.Distance(a, b) <= catchDistance) campaign.Fail("A sentinel caught you. Break its line of sight around walls and use the map to plan another route.");
    }
}
