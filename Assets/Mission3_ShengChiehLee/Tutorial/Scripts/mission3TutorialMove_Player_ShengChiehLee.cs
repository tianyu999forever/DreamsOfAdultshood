using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

// Tutorial-local copy of shared Moving physics; the shared script and player asset stay unchanged.
[DefaultExecutionOrder(-100)]
public class mission3TutorialMove_Player_ShengChiehLee : Moving
{
    Rigidbody body;
    Collider capsule;
    Vector2 move;
    bool running, jumpHeld, jumpRequested;
    float stepTimer;
    readonly Vector3[] wallNormals = new Vector3[8];
    int wallCount;
    PhysicsMaterial originalMaterial, movementMaterial;
    MeshRenderer bodyRenderer;
    ShadowCastingMode originalBodyShadows;
    void Awake()
    {
        // Hide only the root capsule mesh; camera-mounted hands and watch keep rendering.
        bodyRenderer = GetComponent<MeshRenderer>();
        if (bodyRenderer == null) return;
        originalBodyShadows = bodyRenderer.shadowCastingMode;
        bodyRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }
    void Start()
    {
        body = GetComponent<Rigidbody>(); capsule = GetComponent<Collider>();
        if (capsule == null) return;
        // Only this runtime collider changes; never edit the shared player or material asset.
        originalMaterial = capsule.sharedMaterial;
        movementMaterial = new PhysicsMaterial("Mission3 player slide")
        {
            hideFlags = HideFlags.DontSave,
            staticFriction = 0, dynamicFriction = 0, bounciness = 0,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        capsule.sharedMaterial = movementMaterial;
    }
    void OnEnable() { jumpHeld = Keyboard.current != null && Keyboard.current.spaceKey.isPressed; jumpRequested = false; wallCount = 0; }
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || Time.timeScale <= 0) { move = Vector2.zero; jumpRequested = false; return; }
        move = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0), (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
        running = keyboard.leftShiftKey.isPressed;
        bool space = keyboard.spaceKey.isPressed;
        if (space && !jumpHeld) jumpRequested = true;
        jumpHeld = space;
        if (move.sqrMagnitude < .01f || !Grounded()) { stepTimer = 0; return; }
        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0)
        {
            stepTimer = running ? runStepInterval : walkStepInterval;
            if (footstepSource != null && footstepClips != null && footstepClips.Length > 0) footstepSource.PlayOneShot(footstepClips[Random.Range(0, footstepClips.Length)], running ? runVolume : walkVolume);
        }
    }
    void FixedUpdate()
    {
        if (body == null || body.isKinematic || cameraTransform == null) { wallCount = 0; return; }
        var forward = cameraTransform.forward; forward.y = 0; forward.Normalize();
        var right = cameraTransform.right; right.y = 0; right.Normalize();
        var velocity = (forward * move.y + right * move.x).normalized * (running ? runSpeed : walkSpeed);
        // Keep the component along each wall, without accelerating diagonal movement.
        // Two passes also constrain corners; motion away from a wall remains unrestricted.
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < wallCount; i++)
            {
                float intoWall = Vector3.Dot(velocity, wallNormals[i]);
                if (intoWall < 0) velocity -= wallNormals[i] * intoWall;
            }
        wallCount = 0; // Collision callbacks refill contacts after the next physics step.
        body.linearVelocity = new Vector3(velocity.x, body.linearVelocity.y, velocity.z);
        if (jumpRequested && Grounded()) body.linearVelocity = new Vector3(velocity.x, jumpSpeed, velocity.z);
        jumpRequested = false;
    }
    bool Grounded() { return capsule != null && Physics.Raycast(capsule.bounds.center, Vector3.down, capsule.bounds.extents.y + .12f, ~0, QueryTriggerInteraction.Ignore); }
    void OnCollisionEnter(Collision collision) { RecordWalls(collision); }
    void OnCollisionStay(Collision collision) { RecordWalls(collision); }
    void RecordWalls(Collision collision)
    {
        if (!enabled) return;
        for (int i = 0; i < collision.contactCount && wallCount < wallNormals.Length; i++)
        {
            var normal = collision.GetContact(i).normal;
            if (Mathf.Abs(normal.y) > .6f) continue; // Floor and ceiling are not walls.
            normal.y = 0; normal.Normalize();
            bool duplicate = false;
            for (int j = 0; j < wallCount; j++)
                if (Vector3.Dot(normal, wallNormals[j]) > .98f) { duplicate = true; break; }
            if (!duplicate) wallNormals[wallCount++] = normal;
        }
    }
    void OnDisable() { move = Vector2.zero; jumpRequested = false; wallCount = 0; }
    void OnDestroy()
    {
        if (bodyRenderer != null && bodyRenderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
            bodyRenderer.shadowCastingMode = originalBodyShadows;
        if (capsule != null && capsule.sharedMaterial == movementMaterial) capsule.sharedMaterial = originalMaterial;
        if (movementMaterial != null) Destroy(movementMaterial);
    }
}
