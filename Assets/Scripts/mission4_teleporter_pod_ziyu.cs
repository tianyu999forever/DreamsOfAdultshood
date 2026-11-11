using UnityEngine;
using TMPro;

public class Teleporter : MonoBehaviour
{
    [Header("Destination")]
    // 传送目标点（场景里的一个空物体）
    public Transform destination;

    [Header("Timing")]
    public float stayTime = 2f;

    [Header("Landing")]
    // 自动把玩家放在目标点下方的地面上，防止卡进地板
    public bool snapToGround = true;

    [Header("Feedback (optional)")]
    public TMP_Text promptText;
    public string promptLabel = "TELEPORTING IN ";
    public ParticleSystem teleportParticlePrefab;
    public AudioSource teleportSound;

    // 刚传送完的时间，用来防止在目标传送器上来回传送
    static float lastTeleportTime = -10f;

    int playerCollidersInside;
    bool blockedUntilExit;
    float stayTimer;

    Transform player;
    Rigidbody playerBody;

    void Start()
    {
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerCollidersInside++;

        if (playerCollidersInside > 1)
            return;

        playerBody = other.attachedRigidbody;
        player = playerBody != null
            ? playerBody.transform
            : other.transform.root;

        stayTimer = 0f;

        // 玩家是被传送到这里的，必须先离开才能再次触发
        blockedUntilExit =
            Time.time - lastTeleportTime < 0.5f;

        UpdatePrompt();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerCollidersInside =
            Mathf.Max(0, playerCollidersInside - 1);

        if (playerCollidersInside > 0)
            return;

        ResetState();
    }

    void Update()
    {
        if (playerCollidersInside == 0 ||
            blockedUntilExit ||
            destination == null)
            return;

        // 暂停时 deltaTime 为 0，计时也会停止
        stayTimer += Time.deltaTime;

        if (stayTimer >= stayTime)
        {
            Teleport();
            return;
        }

        UpdatePrompt();
    }

    void Teleport()
    {
        Vector3 target = GetLandingPosition();

        PlayParticles(player.position);

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.position = target;
        }

        player.position = target;
        Physics.SyncTransforms();

        lastTeleportTime = Time.time;

        PlayParticles(target);

        if (teleportSound != null)
            teleportSound.Play();

        ResetState();
    }

    Vector3 GetLandingPosition()
    {
        Vector3 target = destination.position;

        if (!snapToGround)
            return target;

        Collider playerCollider = player.GetComponent<Collider>();

        if (playerCollider == null)
            return target;

        // 玩家中心点到碰撞体底部的距离
        float pivotToBottom =
            player.position.y - playerCollider.bounds.min.y;

        // 从目标点上方往下找地面（忽略 Trigger）
        Vector3 rayStart = target + Vector3.up * 1f;

        if (Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                10f,
                ~0,
                QueryTriggerInteraction.Ignore))
        {
            target.y = hit.point.y + pivotToBottom + 0.05f;
        }
        else
        {
            Debug.LogWarning(
                name + ": no ground found below destination");
        }

        return target;
    }

    void ResetState()
    {
        playerCollidersInside = 0;
        blockedUntilExit = false;
        stayTimer = 0f;

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void UpdatePrompt()
    {
        if (promptText == null)
            return;

        if (blockedUntilExit)
        {
            promptText.gameObject.SetActive(false);
            return;
        }

        float timeLeft = Mathf.Max(0f, stayTime - stayTimer);

        promptText.text =
            promptLabel + timeLeft.ToString("0.0");

        promptText.gameObject.SetActive(true);
    }

    void PlayParticles(Vector3 position)
    {
        if (teleportParticlePrefab == null)
            return;

        ParticleSystem particles = Instantiate(
            teleportParticlePrefab,
            position,
            Quaternion.identity
        );

        particles.Play();

        Destroy(particles.gameObject, 3f);
    }

    bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") ||
               other.transform.root.CompareTag("Player");
    }

    // 在 Scene 视图里画出传送线，方便摆放
    void OnDrawGizmos()
    {
        if (destination == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, destination.position);
        Gizmos.DrawWireSphere(destination.position, 0.3f);
    }
}
