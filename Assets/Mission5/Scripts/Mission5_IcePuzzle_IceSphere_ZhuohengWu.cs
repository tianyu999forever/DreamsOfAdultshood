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
    [SerializeField] private Collectible number3;
    [SerializeField] private Collider landingFloor;

    [Header("Melt and Number Drop")]
    [SerializeField, Min(0.01f)] private float meltDuration = 0.6f;
    [SerializeField, Min(0.01f)] private float dropDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float dropHeight = 0.6f;
    [SerializeField, Min(0.01f)] private float numberFloorOffset = 0.35f;
    [SerializeField, Min(0.1f)] private float floorProbeHeight = 2f;
    [SerializeField, Min(0.1f)] private float floorProbeDistance = 10f;

    private Rigidbody body;
    private SphereCollider iceCollider;
    private Collider numberTrigger;
    private Vector3 direction;
    private bool ready;
    private bool melting;

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        iceCollider = GetComponent<SphereCollider>();
        numberTrigger = number3 != null ? number3.GetComponent<Collider>() : null;

        if (iceVisual == null || iceVisual == transform || !iceVisual.IsChildOf(transform) ||
            number3 == null || numberTrigger == null || !numberTrigger.isTrigger ||
            number3.itemType != Collectible.ItemType.Number || number3.numberValue != 3 ||
            !number3.enabled || landingFloor == null || landingFloor.isTrigger ||
            !landingFloor.enabled || !landingFloor.gameObject.activeInHierarchy || iceCollider.isTrigger)
        {
            Debug.LogError("Mission 5 ice: assign a child Ice Visual, the existing Number_3 " +
                "with an enabled Collectible and trigger Collider, and the active solid Ground Collider.", this);
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
        number3.gameObject.SetActive(false);
        FloatingItem floating = number3.GetComponent<FloatingItem>();
        if (floating != null) floating.enabled = false;
        numberTrigger.enabled = false;

        ready = true;
        body.linearVelocity = direction * moveSpeed;
    }

    private void FixedUpdate()
    {
        if (!ready || melting) return;
        // Restore the configured horizontal speed each physics step, without damping.
        body.linearVelocity = direction * Mathf.Max(0.1f, moveSpeed);
    }

    private void OnCollisionEnter(Collision collision) => ReflectFromWalls(collision);
    private void OnCollisionStay(Collision collision) => ReflectFromWalls(collision);

    private void ReflectFromWalls(Collision collision)
    {
        if (!ready || melting) return;

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
        if (!ready || melting || !isActiveAndEnabled) return;

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

        Vector3 landingPosition = floorHit.point + Vector3.up * numberFloorOffset;
        StartCoroutine(MeltAndRelease(landingPosition));
    }

    private IEnumerator MeltAndRelease(Vector3 landingPosition)
    {
        Vector3 fullScale = iceVisual.localScale;
        float elapsed = 0f;
        while (elapsed < meltDuration)
        {
            elapsed += Time.deltaTime;
            iceVisual.localScale = Vector3.Lerp(fullScale, Vector3.zero, elapsed / meltDuration);
            yield return null;
        }
        iceVisual.gameObject.SetActive(false);

        // Detach only this instance at runtime; never activate the entire waiting group.
        number3.transform.SetParent(null, true);
        Vector3 dropStart = landingPosition + Vector3.up * dropHeight;
        number3.transform.position = dropStart;
        Rigidbody numberBody = number3.GetComponent<Rigidbody>();
        if (numberBody != null)
        {
            if (!numberBody.isKinematic)
            {
                numberBody.linearVelocity = Vector3.zero;
                numberBody.angularVelocity = Vector3.zero;
            }
            numberBody.useGravity = false;
            numberBody.isKinematic = true;
        }
        number3.gameObject.SetActive(true);

        elapsed = 0f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropDuration);
            number3.transform.position = Vector3.Lerp(dropStart, landingPosition, t * t);
            yield return null;
        }

        number3.transform.position = landingPosition;
        Physics.SyncTransforms();
        numberTrigger.enabled = true; // Existing Collectible owns pickup and Bag updates.
    }
}
