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
    private GameObject starVisual;
    private Mission5_StarCollectible_ZhuohengWu collectible;
    private bool initialized;
    private bool card2WasSelected;
    private bool intercepted;
    private float settledTime;

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        flightCollider = GetComponent<SphereCollider>();
        collectible = GetComponent<Mission5_StarCollectible_ZhuohengWu>();

        if (hotbar == null || launchPoint == null || failTrigger == null || !failTrigger.isTrigger ||
            landingFloor == null || landingFloor.isTrigger || playerCollider == null || playerCollider.isTrigger ||
            pickupTrigger == null || !pickupTrigger.isTrigger || pickupTrigger == flightCollider ||
            pickupTrigger.gameObject != gameObject || GetComponent<Collider>() != pickupTrigger ||
            flightCollider.isTrigger || collectible == null || !collectible.enabled ||
            collectible.Visual == null || collectible.Visual == transform ||
            !collectible.Visual.IsChildOf(transform) ||
            launchPoint.IsChildOf(transform))
        {
            Debug.LogError("Mission 5 launch puzzle: assign every scene reference, keep the original " +
                "root pickup BoxCollider first, add a solid SphereCollider, and use the existing LauncherStar " +
                "with its child Visual assigned on Star Collectible.", this);
            enabled = false;
            return;
        }

        starVisual = collectible.Visual.gameObject;
        FloatingItem floating = GetComponent<FloatingItem>();
        if (floating != null) floating.enabled = false;
        pickupTrigger.enabled = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.linearDamping = 0f;
        body.angularDamping = 0f;
        ParkAtLaunchPoint();
        initialized = true;
        hotbar.SelectionChanged += OnSelectionChanged;
        collectible.CollectedSuccessfully += OnCollected;
        Debug.Log("Mission 5 LauncherStar: Ready", this);
    }

    private void LateUpdate()
    {
        if (!initialized) return;
        bool selected = Time.timeScale > 0f && hotbar.isActiveAndEnabled && hotbar.SelectedSlotIndex == 1;
        bool activation = selected && !card2WasSelected;
        card2WasSelected = selected;

        if (activation && State == PuzzleState.Ready)
            Launch();
        UpdatePickupAvailability();
    }

    private void OnSelectionChanged(int slot) => UpdatePickupAvailability();

    private void UpdatePickupAvailability()
    {
        if (initialized && State == PuzzleState.Flying)
            pickupTrigger.enabled = Mission5_TimeFreeze_Environment_ZhuohengWu.IsFrozenIn(this);
    }

    private void OnCollected()
    {
        // The existing collectible has already registered this star and awarded its score.
        // Retire the entire physics object immediately, including while its snapshot is frozen.
        State = PuzzleState.Complete;
        pickupTrigger.enabled = false;
        flightCollider.enabled = false;
        starVisual.SetActive(false);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (hotbar != null) hotbar.SelectionChanged -= OnSelectionChanged;
        if (collectible != null) collectible.CollectedSuccessfully -= OnCollected;
    }

    private void Launch()
    {
        State = PuzzleState.Flying; // Lock immediately; this is not a cooldown.
        starVisual.SetActive(true);
        intercepted = false;
        settledTime = 0f;
        body.isKinematic = false;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        flightCollider.enabled = true;
        // Swept body contact below handles blocking without a solver impulse on the player.
        // Reapply after enabling: ignore-pair state can be lost when a collider is disabled.
        Physics.IgnoreCollision(flightCollider, playerCollider, true);
        body.angularVelocity = Vector3.zero;
        body.linearVelocity = launchPoint.TransformDirection(launchVelocity);
        body.WakeUp();
        Debug.Log("Mission 5 LauncherStar: Flying", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Mission5_TimeFreeze_Environment_ZhuohengWu.IsFrozenIn(this) || !initialized || State != PuzzleState.Flying || intercepted || other != failTrigger) return;
        State = PuzzleState.Reset;
        StartCoroutine(ResetAfterFailure());
    }

    // A freeze may begin on the same step as entry. Resume an overlapping failure zone too.
    private void OnTriggerStay(Collider other) => OnTriggerEnter(other);

    private IEnumerator ResetAfterFailure()
    {
        Debug.Log("Mission 5 LauncherStar: Reset (entered FailTrigger)", this);
        ParkAtLaunchPoint();
        // Wait for a physics boundary after teleporting; Ready is not time-unlocked.
        yield return new WaitForFixedUpdate();
        while (Mission5_TimeFreeze_Environment_ZhuohengWu.IsFrozenIn(this)) yield return null;
        State = PuzzleState.Ready;
        Debug.Log("Mission 5 LauncherStar: Ready (release and select Card 2 to retry)", this);
    }

    private void ParkAtLaunchPoint()
    {
        // Hide only the child visual; the same collectible root stays alive for every retry.
        starVisual.SetActive(false);
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

    private void CheckPlayerBlock()
    {
        if (playerCollider == null || !playerCollider.enabled || !playerCollider.gameObject.activeInHierarchy) return;

        // Check the real body shape, including walking into a star between physics steps.
        if (Physics.ComputePenetration(flightCollider, body.position, body.rotation,
            playerCollider, playerCollider.transform.position, playerCollider.transform.rotation,
            out _, out _))
        {
            BlockAt(body.position);
            return;
        }

        // Sweep one physics step to avoid skipping the player at launch speed. Solid room
        // geometry must be the first hit, so a player behind a wall cannot block the star.
        float radius = WorldRadius();
        Vector3 centerOffset = transform.TransformVector(flightCollider.center);
        Vector3 travel = (body.linearVelocity + Physics.gravity * Time.fixedDeltaTime) * Time.fixedDeltaTime;
        float distance = travel.magnitude;
        if (distance < 0.0001f) return;
        RaycastHit[] hits = Physics.SphereCastAll(body.position + centerOffset, radius,
            travel / distance, distance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        Collider first = null;
        float nearest = float.PositiveInfinity;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.attachedRigidbody == body || hit.distance >= nearest) continue;
            first = hit.collider;
            nearest = hit.distance;
        }
        if (first == playerCollider)
            BlockAt(body.position + travel.normalized * Mathf.Max(0f, nearest - 0.01f));
    }

    private void BlockAt(Vector3 position)
    {
        intercepted = true;
        settledTime = 0f;
        body.position = position;
        // Cancel forward/upward momentum on the star only. Gravity drops it at contact;
        // keeping the solver pair ignored prevents knockback, jumping, or body jitter.
        body.linearVelocity = new Vector3(0f, Mathf.Min(0f, body.linearVelocity.y), 0f);
        body.angularVelocity = Vector3.zero;
        Debug.Log("Mission 5 LauncherStar: player body blocked star; waiting for landing", this);
    }

    private float WorldRadius() => flightCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x),
        Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));

    private void FixedUpdate()
    {
        if (!initialized || State != PuzzleState.Flying) return;
        UpdatePickupAvailability();
        if (Mission5_TimeFreeze_Environment_ZhuohengWu.IsFrozenIn(this))
        {
            // Enabling a trigger around an overlapping player must also work without a new
            // trigger-enter callback. Use the same shape and single-use collection method.
            if (playerCollider != null && playerCollider.enabled && playerCollider.gameObject.activeInHierarchy &&
                Physics.ComputePenetration(pickupTrigger, pickupTrigger.transform.position,
                    pickupTrigger.transform.rotation, playerCollider, playerCollider.transform.position,
                    playerCollider.transform.rotation, out _, out _))
                collectible.TryCollect(playerCollider);
            return;
        }
        if (!intercepted) CheckPlayerBlock();
        if (!intercepted) return;

        float radius = WorldRadius();
        Vector3 center = transform.TransformPoint(flightCollider.center);
        bool onFloor = landingFloor.Raycast(new Ray(center, Vector3.down), out RaycastHit hit,
            radius + floorTolerance) && Vector3.Dot(hit.normal, Vector3.up) > 0.8f;

        if (!onFloor || body.linearVelocity.sqrMagnitude > settledSpeed * settledSpeed)
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
        // Without Time Freeze, pickup still requires a body block and successful landing.
        pickupTrigger.enabled = true;
        Debug.Log("Mission 5 LauncherStar: Complete (pickup enabled)", this);
    }
}
