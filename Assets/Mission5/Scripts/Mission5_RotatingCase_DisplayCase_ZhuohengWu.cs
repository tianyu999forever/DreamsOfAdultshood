using UnityEngine;

/// <summary>A compact shutter safe with a visible, physical freeze-and-alignment interlock.</summary>
public class Mission5_RotatingCase_DisplayCase_ZhuohengWu : MonoBehaviour
{
    [SerializeField] private Mission5_CardHotbar_CardHotbar_ZhuohengWu hotbar;
    [SerializeField] private Rigidbody rotatingCase;
    [SerializeField] private Mission5_StarCollectible_ZhuohengWu rewardStar;
    [SerializeField] private Collider pickupTrigger;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private Transform leftSafetyGate;
    [SerializeField] private Transform rightSafetyGate;
    [SerializeField] private Renderer statusLamp;
    [SerializeField, Min(0.1f)] private float rotationSpeed = 72f;
    [SerializeField, Range(1f, 40f)] private float alignmentTolerance = 24f;
    [SerializeField, Min(0.1f)] private float gateSpeed = 8f;
    [SerializeField, Min(0.1f)] private float gateTravel = 0.55f;

    public bool IsFrozen => hotbar != null && hotbar.isActiveAndEnabled &&
        hotbar.SelectedSlotIndex == 4 && Time.timeScale > 0f;
    public bool IsAligned => rotatingCase != null && Mathf.Abs(Vector3.SignedAngle(transform.forward,
        rotatingCase.rotation * Vector3.forward, Vector3.up)) <= alignmentTolerance;
    // A lifted housing must never expose the stationary reward as a gravity shortcut.
    private bool AtRest => gravity == null || !gravity.IsMoving;
    public bool AccessOpen => AtRest && IsFrozen && IsAligned && gateOpening >= 1f;

    private Mission5_ReverseGravity_MovableObject_ZhuohengWu gravity;
    private Quaternion rotation;
    private Vector3 leftClosed, rightClosed;
    private float gateOpening;
    private MaterialPropertyBlock lampBlock;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        gravity = GetComponent<Mission5_ReverseGravity_MovableObject_ZhuohengWu>();
        if (pickupTrigger != null) pickupTrigger.enabled = false;
        if (hotbar == null || rotatingCase == null || rewardStar == null || pickupTrigger == null ||
            playerCollider == null || leftSafetyGate == null || rightSafetyGate == null || statusLamp == null ||
            !rotatingCase.isKinematic || !pickupTrigger.isTrigger || pickupTrigger.gameObject != rewardStar.gameObject ||
            rewardStar.transform.IsChildOf(rotatingCase.transform))
        {
            Debug.LogError("Mission 5 shutter safe: assign Hotbar, kinematic Shutter Rotor, stationary Star, " +
                "Pickup Trigger, Player Collider, both safety gates and Status Lamp.", this);
            enabled = false;
            return;
        }
        rotation = rotatingCase.rotation;
        leftClosed = leftSafetyGate.localPosition;
        rightClosed = rightSafetyGate.localPosition;
        lampBlock = new MaterialPropertyBlock();
        UpdateStatus();
    }

    private void OnEnable()
    {
        if (hotbar != null) hotbar.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(int slot)
    {
        if (slot == 4 && rotatingCase != null)
        {
            // Capture the actual physics angle, not a previous interpolated render pose.
            rotation = rotatingCase.rotation;
            rotatingCase.transform.rotation = rotation;
        }
        else if (pickupTrigger != null) pickupTrigger.enabled = false;
    }

    private void FixedUpdate()
    {
        if (Time.timeScale <= 0f) return;
        bool unlock = AtRest && IsFrozen && IsAligned;
        gateOpening = Mathf.MoveTowards(gateOpening, unlock ? 1f : 0f, gateSpeed * Time.fixedDeltaTime);
        leftSafetyGate.localPosition = leftClosed + Vector3.left * (gateOpening * gateTravel);
        rightSafetyGate.localPosition = rightClosed + Vector3.right * (gateOpening * gateTravel);
        Physics.SyncTransforms();

        // This visible gate closes before rotation resumes. There is never an open moving entrance.
        if (!IsFrozen && gateOpening == 0f)
        {
            rotation = Quaternion.AngleAxis(rotationSpeed * Time.fixedDeltaTime, Vector3.up) * rotation;
            rotatingCase.MoveRotation(rotation);
        }
        UpdateStatus();
        if (rewardStar == null || rewardStar.Collected || pickupTrigger == null) return;

        Vector3 local = transform.InverseTransformPoint(playerCollider.bounds.center);
        bool atPort = local.z > 0.85f && local.z < 2f && Mathf.Abs(local.x) < 0.9f &&
            playerCollider.bounds.min.y <= transform.position.y + 0.22f;
        bool clear = atPort && !Physics.Linecast(playerCollider.bounds.center, rewardStar.transform.position,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        pickupTrigger.enabled = AccessOpen && clear;
        // Also handle a player already overlapping as the gate opens: no missed trigger-enter event.
        if (pickupTrigger.enabled && Physics.ComputePenetration(pickupTrigger,
            pickupTrigger.transform.position, pickupTrigger.transform.rotation, playerCollider,
            playerCollider.transform.position, playerCollider.transform.rotation, out _, out _))
            rewardStar.TryCollect(playerCollider);
    }

    private void UpdateStatus()
    {
        if (statusLamp == null || lampBlock == null) return;
        statusLamp.GetPropertyBlock(lampBlock);
        lampBlock.SetColor(BaseColor, AccessOpen ? new Color(0.15f, 1f, 0.35f, 1f) :
            (AtRest && IsFrozen && IsAligned ? new Color(1f, 0.65f, 0.08f, 1f) : new Color(1f, 0.14f, 0.06f, 1f)));
        statusLamp.SetPropertyBlock(lampBlock);
    }

    private void OnDisable()
    {
        if (hotbar != null) hotbar.SelectionChanged -= OnSelectionChanged;
        if (pickupTrigger != null) pickupTrigger.enabled = false;
    }
}
