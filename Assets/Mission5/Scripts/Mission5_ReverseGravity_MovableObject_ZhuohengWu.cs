using System.Collections.Generic;
using UnityEngine;

/// <summary>Controlled vertical motion for eligible props, with an exact per-cycle return pose.</summary>
public class Mission5_ReverseGravity_MovableObject_ZhuohengWu : MonoBehaviour
{
    [SerializeField] private Mission5_CardHotbar_CardHotbar_ZhuohengWu hotbar;
    [SerializeField] private Collider ceiling;
    [SerializeField] private Transform player;
    [SerializeField] private Behaviour[] suspendWhileMoving = new Behaviour[0];
    [SerializeField] private Rigidbody[] carriedBodies = new Rigidbody[0];
    [SerializeField, Min(0.1f)] private float maximumSpeed = 3.5f;
    [SerializeField, Min(0.1f)] private float acceleration = 14f;
    [SerializeField, Min(0f)] private float ceilingClearance = 0.05f;

    public bool IsReversed { get; private set; }
    public bool IsMoving { get; private set; }
    public Vector3 OriginalPosition { get; private set; }
    public Quaternion OriginalRotation { get; private set; }
    public float LiftOffset => offset;
    public event System.Action Returned;

    private Rigidbody body;
    private Collider[] shapes;
    private Renderer[] visuals;
    private Mission5_IcePuzzle_IceSphere_ZhuohengWu ice;
    private bool[] previousEnabled;
    private bool wasKinematic, usedGravity;
    private RigidbodyConstraints previousConstraints;
    private CollisionDetectionMode previousDetection;
    private Vector3 previousVelocity, previousAngularVelocity;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private float offset, verticalSpeed, maximumLift;
    private Vector3[] carriedOffsets;
    private RigidbodyInterpolation[] carriedInterpolation;
    private readonly List<CollisionPair> pairs = new List<CollisionPair>();

    private struct CollisionPair
    {
        public Collider Own, Other;
        public bool OriginallyIgnored;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        shapes = GetComponentsInChildren<Collider>(true);
        visuals = GetComponentsInChildren<Renderer>(true);
        ice = GetComponent<Mission5_IcePuzzle_IceSphere_ZhuohengWu>();
        if (hotbar == null || ceiling == null || player == null || ceiling.isTrigger ||
            GetComponentInParent<Moving>() != null || GetComponentInChildren<Camera>(true) != null ||
            GetComponentInChildren<Canvas>(true) != null ||
            GetComponentInChildren<Mission5_StarCollectible_ZhuohengWu>(true) != null)
        {
            Debug.LogError("Mission 5 gravity: assign Hotbar, solid Ceiling and Player. " +
                "Keep players, cameras, UI, stars and essential triggers outside moving prop hierarchies.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        // Explicit scene targets keep structures and other missions out of this ability.
        var protectedShapes = new HashSet<Collider>(player.GetComponentsInChildren<Collider>(true));
        foreach (var star in FindObjectsByType<Mission5_StarCollectible_ZhuohengWu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (star.gameObject.scene == gameObject.scene)
                foreach (Collider shape in star.GetComponentsInChildren<Collider>(true)) protectedShapes.Add(shape);
        foreach (var prop in FindObjectsByType<Mission5_ReverseGravity_MovableObject_ZhuohengWu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (prop != this && prop.gameObject.scene == gameObject.scene)
                foreach (Collider shape in prop.GetComponentsInChildren<Collider>(true)) protectedShapes.Add(shape);
        foreach (Collider own in shapes)
        foreach (Collider other in protectedShapes)
            if (!own.isTrigger && other != null && !other.isTrigger && other != own)
                pairs.Add(new CollisionPair { Own = own, Other = other,
                    OriginallyIgnored = Physics.GetIgnoreCollision(own, other) });
    }

    private void FixedUpdate()
    {
        if (body != null) MoveVertically(Time.fixedDeltaTime);
        UpdateProtectedCollisions();
    }

    private void LateUpdate()
    {
        // Render-only and static-collider props do not need a Rigidbody added to use Card 3.
        if (body == null) MoveVertically(Time.deltaTime);
    }

    private void MoveVertically(float deltaTime)
    {
        if (Time.timeScale <= 0f) return;
        IsReversed = hotbar.isActiveAndEnabled && hotbar.SelectedSlotIndex == 2;
        // Time Freeze pauses a return in place without replacing its original anchor/state.
        if (hotbar.isActiveAndEnabled && hotbar.SelectedSlotIndex == 4) return;
        if (!IsMoving)
        {
            if (!IsReversed || (ice != null && ice.IsMelting)) return;
            if (!CapturePose()) return;
        }

        float target = IsReversed ? maximumLift : 0f;
        float direction = Mathf.Sign(target - offset);
        verticalSpeed = Mathf.MoveTowards(verticalSpeed, direction * maximumSpeed, acceleration * deltaTime);
        offset = Mathf.Clamp(offset + verticalSpeed * deltaTime, 0f, maximumLift);
        if (offset == target) verticalSpeed = 0f;
        Vector3 position = OriginalPosition + Vector3.up * offset;
        if (body != null)
        {
            body.MovePosition(position);
            body.MoveRotation(OriginalRotation);
        }
        else
        {
            transform.localPosition = originalLocalPosition + (transform.parent != null
                ? transform.parent.InverseTransformVector(Vector3.up * offset) : Vector3.up * offset);
            transform.localRotation = originalLocalRotation;
        }
        CarryBodies(position);
        if (body == null) Physics.SyncTransforms();
        if (!IsReversed && offset == 0f) RestorePoseAndState();
    }

    private bool CapturePose()
    {
        OriginalPosition = body != null ? body.position : transform.position;
        OriginalRotation = body != null ? body.rotation : transform.rotation;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        Bounds bounds = new Bounds(OriginalPosition, Vector3.zero);
        bool foundBounds = false;
        foreach (Renderer visual in visuals)
            if (visual != null && visual.enabled && visual.gameObject.activeInHierarchy)
            {
                if (!foundBounds) bounds = visual.bounds; else bounds.Encapsulate(visual.bounds);
                foundBounds = true;
            }
        foreach (Collider shape in shapes)
            if (shape != null && shape.enabled && !shape.isTrigger && shape.gameObject.activeInHierarchy)
            {
                if (!foundBounds) bounds = shape.bounds; else bounds.Encapsulate(shape.bounds);
                foundBounds = true;
            }
        if (!foundBounds) return false;
        // The assigned Mission 5 roof is flat and spans the entire playable room.
        maximumLift = Mathf.Max(0f, ceiling.bounds.min.y - ceilingClearance - bounds.max.y);
        if (maximumLift <= 0f) return false;
        offset = verticalSpeed = 0f;
        previousEnabled = new bool[suspendWhileMoving.Length];
        for (int i = 0; i < suspendWhileMoving.Length; i++)
            if (suspendWhileMoving[i] != null)
            {
                previousEnabled[i] = suspendWhileMoving[i].enabled;
                suspendWhileMoving[i].enabled = false;
            }
        if (body != null)
        {
            wasKinematic = body.isKinematic;
            usedGravity = body.useGravity;
            previousConstraints = body.constraints;
            previousDetection = body.collisionDetectionMode;
            previousVelocity = body.linearVelocity;
            previousAngularVelocity = body.angularVelocity;
            if (!wasKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }
        carriedOffsets = new Vector3[carriedBodies.Length];
        carriedInterpolation = new RigidbodyInterpolation[carriedBodies.Length];
        for (int i = 0; i < carriedBodies.Length; i++)
            if (carriedBodies[i] != null && carriedBodies[i].isKinematic &&
                carriedBodies[i].transform.IsChildOf(transform))
            {
                carriedOffsets[i] = transform.InverseTransformPoint(carriedBodies[i].position);
                carriedInterpolation[i] = carriedBodies[i].interpolation;
                // The prop moves each rendered frame. A second interpolation stream on its
                // child body would drag the rotor back toward the previous parent pose.
                carriedBodies[i].interpolation = RigidbodyInterpolation.None;
            }
        IsMoving = true;
        UpdateProtectedCollisions();
        return true;
    }

    private void CarryBodies(Vector3 rootPosition)
    {
        if (carriedOffsets == null) return;
        for (int i = 0; i < carriedBodies.Length; i++)
            if (carriedBodies[i] != null && carriedBodies[i].isKinematic &&
                carriedBodies[i].transform.IsChildOf(transform))
            {
                // Only translate the child. Its own controller retains rotation ownership.
                Vector3 position = rootPosition + transform.TransformVector(carriedOffsets[i]);
                carriedBodies[i].position = position;
                carriedBodies[i].transform.position = position;
            }
    }

    private void RestorePoseAndState()
    {
        // A new press during return retains this anchor. Only a completed return ends the cycle.
        transform.SetPositionAndRotation(OriginalPosition, OriginalRotation);
        if (body == null)
        {
            // Preserve the exact stored local coordinates under rotated parent groups.
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
        }
        if (body != null)
        {
            body.position = OriginalPosition;
            body.rotation = OriginalRotation;
            body.constraints = previousConstraints;
            body.isKinematic = wasKinematic;
            body.useGravity = usedGravity;
            body.collisionDetectionMode = previousDetection;
            if (!wasKinematic)
            {
                body.linearVelocity = previousVelocity;
                body.angularVelocity = previousAngularVelocity;
            }
        }
        CarryBodies(OriginalPosition);
        for (int i = 0; i < carriedBodies.Length; i++)
            if (carriedBodies[i] != null && carriedBodies[i].isKinematic &&
                carriedBodies[i].transform.IsChildOf(transform))
                carriedBodies[i].interpolation = carriedInterpolation[i];
        for (int i = 0; i < suspendWhileMoving.Length; i++)
            if (suspendWhileMoving[i] != null) suspendWhileMoving[i].enabled = previousEnabled[i];
        offset = verticalSpeed = 0f;
        IsMoving = false;
        Physics.SyncTransforms();
        UpdateProtectedCollisions();
        Returned?.Invoke();
    }

    private void UpdateProtectedCollisions()
    {
        foreach (CollisionPair pair in pairs)
        {
            Collider own = pair.Own, other = pair.Other;
            if (own == null || other == null || !own.enabled || !other.enabled ||
                !own.gameObject.activeInHierarchy || !other.gameObject.activeInHierarchy) continue;
            var otherProp = other.GetComponentInParent<Mission5_ReverseGravity_MovableObject_ZhuohengWu>();
            bool ignored = IsMoving || (otherProp != null && otherProp.IsMoving) || pair.OriginallyIgnored;
            bool currentlyIgnored = Physics.GetIgnoreCollision(own, other);
            // Delay restoring a previously suspended pair while it still overlaps. Ordinary
            // contact with an idle prop (including rotating shutters) must remain solid.
            if (!ignored && currentlyIgnored && Physics.ComputePenetration(own, own.transform.position, own.transform.rotation,
                other, other.transform.position, other.transform.rotation, out _, out _)) ignored = true;
            if (currentlyIgnored != ignored) Physics.IgnoreCollision(own, other, ignored);
        }
    }

    private void OnDisable()
    {
        IsReversed = false;
        if (IsMoving) RestorePoseAndState();
    }
}
