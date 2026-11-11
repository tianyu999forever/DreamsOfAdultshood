using UnityEngine;
using UnityEngine.AI;

public class Mission2_MonsterChase_Monster_TianyuTang : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Mission2_Flashlight_MainCamera_TianyuTang flashlightController;
    public Light flashlight;

    [Header("Death UI")]
    public GameObject deathPanel;
    public Moving moving;
    public MouseLook mouseLook;
    public HeadBob headBob;

    [Header("Flashlight Detection")]
    public float detectionDistance = 12f;
    public float lightExposureNeeded = 0.4f;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float patrolAcceleration = 10f;
    public float waypointReachDistance = 0.5f;

    [Header("Chase")]
    public float chaseSpeed = 4.5f;
    public float chaseAcceleration = 22f;
    public float catchDistance = 1.5f;
    public float loseSightTime = 4f;
    public float visionDistance = 12f;
    public LayerMask blockingLayers = ~0;

    private NavMeshAgent agent;
    private float lightExposure;
    private float lostSightTimer;
    private int patrolIndex;
    private bool chasing;
    private bool caught;
    private Vector3 lastKnownPlayerPosition;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (moving == null) moving = FindFirstObjectByType<Moving>();
        if (mouseLook == null) mouseLook = FindFirstObjectByType<MouseLook>();
        if (headBob == null) headBob = FindFirstObjectByType<HeadBob>();

        if (agent != null)
        {
            agent.speed = patrolSpeed;
            agent.acceleration = patrolAcceleration;
        }
    }

    void Update()
    {
        if (caught || player == null || agent == null)
            return;

        CheckCatchDistance();

        if (caught)
            return;

        if (!chasing)
        {
            CheckFlashlightDetection();

            if (!chasing)
                Patrol();

            return;
        }

        ChasePlayer();
    }

    void CheckFlashlightDetection()
    {
        if (flashlightController == null ||
            flashlight == null ||
            !flashlightController.IsOn)
        {
            lightExposure = 0f;
            return;
        }

        Collider monsterBody = GetComponentInChildren<Collider>();

        Vector3 targetPoint = monsterBody != null
            ? monsterBody.bounds.center
            : transform.position + Vector3.up;

        Vector3 lightOrigin = flashlight.transform.position;
        Vector3 toMonster = targetPoint - lightOrigin;

        if (toMonster.magnitude > detectionDistance)
        {
            lightExposure = 0f;
            return;
        }

        float angle = Vector3.Angle(
            flashlight.transform.forward,
            toMonster.normalized);

        if (angle <= flashlight.spotAngle * 0.55f)
        {
            lightExposure += Time.deltaTime;

            if (lightExposure >= lightExposureNeeded)
            {
                chasing = true;
                lostSightTimer = 0f;
                lastKnownPlayerPosition = player.position;
            }
        }
        else
        {
            lightExposure = 0f;
        }
    }

    void Patrol()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0 ||
            !agent.isOnNavMesh)
        {
            return;
        }

        Transform target = patrolPoints[patrolIndex];

        if (target == null)
            return;

        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.acceleration = patrolAcceleration;
        agent.SetDestination(target.position);

        if (!agent.pathPending &&
            agent.remainingDistance <= waypointReachDistance)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }
    }

    void ChasePlayer()
    {
        if (!agent.isOnNavMesh)
            return;

        if (CanSeePlayer())
        {
            lastKnownPlayerPosition = player.position;
            lostSightTimer = 0f;
        }
        else
        {
            lostSightTimer += Time.deltaTime;
        }

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.acceleration = chaseAcceleration;
        agent.SetDestination(lastKnownPlayerPosition);

        if (lostSightTimer >= loseSightTime)
        {
            chasing = false;
            lightExposure = 0f;
            lostSightTimer = 0f;
        }
    }

    bool CanSeePlayer()
    {
        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 target = player.position + Vector3.up;

        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance > visionDistance)
            return false;

        if (Physics.Raycast(
            origin,
            direction.normalized,
            out RaycastHit hit,
            distance,
            blockingLayers,
            QueryTriggerInteraction.Ignore))
        {
            return hit.transform == player ||
                   hit.transform.IsChildOf(player);
        }

        return false;
    }

    void CheckCatchDistance()
    {
        Vector3 monsterFlat =
            new Vector3(transform.position.x, 0f, transform.position.z);

        Vector3 playerFlat =
            new Vector3(player.position.x, 0f, player.position.z);

        if (Vector3.Distance(monsterFlat, playerFlat) <= catchDistance)
        {
            caught = true;
            agent.isStopped = true;
            agent.ResetPath();

            if (deathPanel != null)
                deathPanel.SetActive(true);

            if (moving != null) moving.enabled = false;
            if (mouseLook != null) mouseLook.enabled = false;
            if (headBob != null) headBob.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }
    }
}