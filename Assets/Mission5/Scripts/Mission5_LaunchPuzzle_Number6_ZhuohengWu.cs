using System.Collections;
using UnityEngine;

/// <summary>Launches one existing collectible; retries require an actual FailTrigger entry.</summary>
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class Mission5_LaunchPuzzle_Number6_ZhuohengWu : MonoBehaviour
{
    public enum PuzzleState { Ready, Flying, Reset, Complete }
    public PuzzleState State { get; private set; } = PuzzleState.Ready;

    [Header("Scene References")]
    [SerializeField] private Mission5_CardHotbar_CardHotbar_ZhuohengWu hotbar;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private Collider failTrigger;
    [SerializeField] private Mission5_RaiseBarrier_Barrier_ZhuohengWu barrier;
    [SerializeField] private BoxCollider landingArea;
    [SerializeField] private Collider landingFloor;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private Collider pickupTrigger;

    [Header("Launch (Launch Point Local Axes; Units per Second)")]
    [SerializeField] private Vector3 launchVelocity = new Vector3(0f, 4.5f, 8f);

    [Header("Successful Landing")]
    [SerializeField, Min(0.01f)] private float settledSpeed = 0.5f;
    [SerializeField, Min(0.01f)] private float settleDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float floorTolerance = 0.12f;

    private Rigidbody body;
    private SphereCollider flightCollider;
    private GameObject numberVisual;
    private bool initialized;
    private bool card2WasSelected;
    private bool intercepted;
    private float settledTime;

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        flightCollider = GetComponent<SphereCollider>();
        Collectible collectible = GetComponent<Collectible>();

        if (hotbar == null || launchPoint == null || failTrigger == null || !failTrigger.isTrigger ||
            barrier == null || landingArea == null || !landingArea.isTrigger ||
            landingFloor == null || landingFloor.isTrigger || playerCollider == null ||
            pickupTrigger == null || !pickupTrigger.isTrigger || pickupTrigger == flightCollider ||
            pickupTrigger.gameObject != gameObject || GetComponent<Collider>() != pickupTrigger ||
            flightCollider.isTrigger || collectible == null || !collectible.enabled ||
            collectible.itemType != Collectible.ItemType.Number || collectible.numberValue != 6 ||
            collectible.visual == null || collectible.visual == transform ||
            !collectible.visual.IsChildOf(transform) ||
            launchPoint.IsChildOf(transform))
        {
            Debug.LogError("Mission 5 launch puzzle: assign every scene reference, keep the original " +
                "root pickup BoxCollider first, add a solid SphereCollider, and use the existing Number_6 " +
                "with its child Visual assigned on Collectible.", this);
            enabled = false;
            return;
        }

        numberVisual = collectible.visual.gameObject;
        FloatingItem floating = GetComponent<FloatingItem>();
        if (floating != null) floating.enabled = false;
        pickupTrigger.enabled = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.linearDamping = 0f;
        body.angularDamping = 0f;
        ParkAtLaunchPoint();
        initialized = true;
        Debug.Log("Mission 5 Number_6: Ready", this);
    }

    private void LateUpdate()
    {
        if (!initialized) return;
        bool selected = Time.timeScale > 0f && hotbar.isActiveAndEnabled && hotbar.SelectedSlotIndex == 1;
        bool activation = selected && !card2WasSelected;
        card2WasSelected = selected;

        if (activation && State == PuzzleState.Ready)
            Launch();
    }

    private void Launch()
    {
        State = PuzzleState.Flying; // Lock immediately; this is not a cooldown.
        numberVisual.SetActive(true);
        intercepted = false;
        settledTime = 0f;
        body.isKinematic = false;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        flightCollider.enabled = true;
        // Reapply after enabling: ignore-pair state can be lost when a collider is disabled.
        Physics.IgnoreCollision(flightCollider, playerCollider, true);
        body.angularVelocity = Vector3.zero;
        body.linearVelocity = launchPoint.TransformDirection(launchVelocity);
        body.WakeUp();
        Debug.Log("Mission 5 Number_6: Flying", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized || State != PuzzleState.Flying || other != failTrigger) return;
        State = PuzzleState.Reset;
        StartCoroutine(ResetAfterFailure());
    }

    private IEnumerator ResetAfterFailure()
    {
        Debug.Log("Mission 5 Number_6: Reset (entered FailTrigger)", this);
        ParkAtLaunchPoint();
        // Wait for a physics boundary after teleporting; Ready is not time-unlocked.
        yield return new WaitForFixedUpdate();
        State = PuzzleState.Ready;
        Debug.Log("Mission 5 Number_6: Ready (release and select Card 2 to retry)", this);
    }

    private void ParkAtLaunchPoint()
    {
        // Hide only the child visual; the same collectible root stays alive for every retry.
        numberVisual.SetActive(false);
        pickupTrigger.enabled = false;
        flightCollider.enabled = false;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.isKinematic = true;
        body.useGravity = false;
        body.position = launchPoint.position;
        body.rotation = launchPoint.rotation;
        transform.SetPositionAndRotation(launchPoint.position, launchPoint.rotation);
        intercepted = false;
        settledTime = 0f;
        Physics.SyncTransforms();
    }

    private void OnCollisionEnter(Collision collision) => CheckInterception(collision);
    private void OnCollisionStay(Collision collision) => CheckInterception(collision);

    private void CheckInterception(Collision collision)
    {
        if (!initialized || State != PuzzleState.Flying || intercepted) return;
        // A real physical contact stays valid even if Card 3 is released during/after impact.
        // The fully lowered collider is underground and cannot contact the airborne number.
        if (collision.collider == barrier.SolidCollider)
        {
            intercepted = true;
            Debug.Log("Mission 5 Number_6: barrier contact; waiting for a reachable landing", this);
        }
    }

    private void FixedUpdate()
    {
        if (!initialized || State != PuzzleState.Flying || !intercepted) return;

        float radius = flightCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
        Vector3 center = transform.TransformPoint(flightCollider.center);
        bool inReachableArea = landingArea.bounds.Contains(center);
        bool onFloor = landingFloor.Raycast(new Ray(center, Vector3.down), out RaycastHit hit,
            radius + floorTolerance) && Vector3.Dot(hit.normal, Vector3.up) > 0.8f;

        if (!inReachableArea || !onFloor || body.linearVelocity.sqrMagnitude > settledSpeed * settledSpeed)
        {
            settledTime = 0f;
            return;
        }

        settledTime += Time.fixedDeltaTime;
        if (settledTime < settleDuration) return;

        State = PuzzleState.Complete;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.isKinematic = true;
        body.useGravity = false;
        flightCollider.enabled = false;
        // Keep the shared Collectible enabled; only its trigger was gated until success.
        pickupTrigger.enabled = true;
        Debug.Log("Mission 5 Number_6: Complete (pickup enabled)", this);
    }
}
