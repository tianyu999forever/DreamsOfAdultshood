using System.Collections;
using UnityEngine;

/// <summary>Moves the ice and releases one existing scene collectible after melting.</summary>
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class Mission5_IcePuzzle_IceSphere_ZhuohengWu : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
    [SerializeField] private Vector3 initialDirection = new Vector3(1f, 0f, 0.6f);

    [Header("Scene References")]
    [SerializeField] private Transform iceVisual;
    [SerializeField] private Mission5_StarCollectible_ZhuohengWu rewardStar;
    [SerializeField] private Collider landingFloor;

    [Header("Melt and Star Drop")]
    [SerializeField, Min(0.01f)] private float meltDuration = 0.6f;
    [SerializeField, Min(0.01f)] private float dropDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float dropHeight = 0.6f;
    [SerializeField, Min(0.01f)] private float starFloorOffset = 0.35f;
    [SerializeField, Min(0.1f)] private float floorProbeHeight = 2f;
    [SerializeField, Min(0.1f)] private float floorProbeDistance = 10f;

    private Rigidbody body;
    private SphereCollider iceCollider;
    private Collider rewardTrigger;
    private Vector3 direction;
    private bool ready;
    private bool melting;
    public bool IsMelting => melting;

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        iceCollider = GetComponent<SphereCollider>();
        rewardTrigger = rewardStar != null ? rewardStar.GetComponent<Collider>() : null;

        if (iceVisual == null || iceVisual == transform || !iceVisual.IsChildOf(transform) ||
            rewardStar == null || rewardTrigger == null || !rewardTrigger.isTrigger ||
            !rewardStar.enabled || landingFloor == null || landingFloor.isTrigger ||
            !landingFloor.enabled || !landingFloor.gameObject.activeInHierarchy || iceCollider.isTrigger)
        {
            Debug.LogError("Mission 5 ice: assign a child Ice Visual, the assigned IceStar " +
                "with an enabled Star Collectible and trigger Collider, and the active solid Ground Collider.", this);
            enabled = false;
            return;
        }

        // Runtime-only configuration; no prefab or scene asset is edited.
        body.isKinematic = false;
        body.useGravity = false;
        body.linearDamping = 0f;
        body.angularDamping = 0f;
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.angularVelocity = Vector3.zero;

        direction = Vector3.ProjectOnPlane(initialDirection, Vector3.up);
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;

        // Reserve this exact instance, even if its waiting parent is inactive.
        rewardStar.gameObject.SetActive(false);
        FloatingItem floating = rewardStar.GetComponent<FloatingItem>();
        if (floating != null) floating.enabled = false;
        rewardTrigger.enabled = false;

        ready = true;
        body.linearVelocity = direction * moveSpeed;
    }

    private void FixedUpdate()
    {
        if (!ready || melting || Mission5_TimeFreeze_Environment_ZhuohengWu.IsFrozenIn(this)) return;
        // Restore the configured horizontal speed each physics step, without damping.
        body.linearVelocity = direction * Mathf.Max(0.1f, moveSpeed);
    }

    private void OnCollisionEnter(Collision collision) => ReflectFromWalls(collision);
    private void OnCollisionStay(Collision collision) => ReflectFromWalls(collision);

    private void ReflectFromWalls(Collision collision)
    {
        if (!ready || melting || Mission5_TimeFreeze_Environment_ZhuohengWu.IsFrozenIn(this)) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = Vector3.ProjectOnPlane(collision.GetContact(i).normal, Vector3.up);
            if (normal.sqrMagnitude < 0.01f) continue; // Ignore horizontal floor surfaces.
            normal.Normalize();
            if (Vector3.Dot(direction, normal) >= -0.001f) continue;
            direction = Vector3.Reflect(direction, normal).normalized;
        }

        body.linearVelocity = direction * Mathf.Max(0.1f, moveSpeed);
    }

    public void TryMelt()
    {
        if (!ready || melting || !isActiveAndEnabled || Mission5_TimeFreeze_Environment_ZhuohengWu.IsFrozenIn(this)) return;

        Ray probe = new Ray(body.position + Vector3.up * floorProbeHeight, Vector3.down);
        if (!landingFloor.Raycast(probe, out RaycastHit floorHit, floorProbeDistance) ||
            Vector3.Dot(floorHit.normal, Vector3.up) < 0.9f)
        {
            Debug.LogError("Mission 5 ice: no flat landing point on the assigned floor below the ice.", this);
            return;
        }

        melting = true; // Set before the coroutine: repeated fire hits cannot restart it.
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.isKinematic = true;
        iceCollider.enabled = false;

        Vector3 landingPosition = floorHit.point + Vector3.up * starFloorOffset;
        StartCoroutine(MeltAndRelease(landingPosition));
    }

    private IEnumerator MeltAndRelease(Vector3 landingPosition)
    {
        Vector3 fullScale = iceVisual.localScale;
        float elapsed = 0f;
        while (elapsed < meltDuration)
        {
            if (Mission5_TimeFreeze_Environment_ZhuohengWu.IsFrozenIn(this)) { yield return null; continue; }
            elapsed += Time.deltaTime;
            iceVisual.localScale = Vector3.Lerp(fullScale, Vector3.zero, elapsed / meltDuration);
            yield return null;
        }
        iceVisual.gameObject.SetActive(false);

        // Detach only this instance at runtime; never activate the entire waiting group.
        rewardStar.transform.SetParent(null, true);
        Vector3 dropStart = landingPosition + Vector3.up * dropHeight;
        rewardStar.transform.position = dropStart;
        Rigidbody starBody = rewardStar.GetComponent<Rigidbody>();
        if (starBody != null)
        {
            if (!starBody.isKinematic)
            {
                starBody.linearVelocity = Vector3.zero;
                starBody.angularVelocity = Vector3.zero;
            }
            starBody.useGravity = false;
            starBody.isKinematic = true;
        }
        rewardStar.gameObject.SetActive(true);

        elapsed = 0f;
        while (elapsed < dropDuration)
        {
            if (Mission5_TimeFreeze_Environment_ZhuohengWu.IsFrozenIn(this)) { yield return null; continue; }
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropDuration);
            rewardStar.transform.position = Vector3.Lerp(dropStart, landingPosition, t * t);
            yield return null;
        }

        rewardStar.transform.position = landingPosition;
        Physics.SyncTransforms();
        rewardTrigger.enabled = true; // Star Collectible owns the single-use pickup and HUD update.
    }
}
