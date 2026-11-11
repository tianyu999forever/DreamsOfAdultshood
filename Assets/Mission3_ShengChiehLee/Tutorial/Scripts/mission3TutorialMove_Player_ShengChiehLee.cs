using UnityEngine;
using UnityEngine.InputSystem;

// Tutorial-local copy of shared Moving physics; the shared script and player asset stay unchanged.
[DefaultExecutionOrder(-100)]
public class mission3TutorialMove_Player_ShengChiehLee : Moving
{
    Rigidbody body;
    Collider capsule;
    Vector2 move;
    bool running, jumpHeld, jumpRequested;
    float stepTimer;
    void Start() { body = GetComponent<Rigidbody>(); capsule = GetComponent<Collider>(); }
    void OnEnable() { jumpHeld = Keyboard.current != null && Keyboard.current.spaceKey.isPressed; jumpRequested = false; }
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
        if (body == null || body.isKinematic || cameraTransform == null) return;
        var forward = cameraTransform.forward; forward.y = 0; forward.Normalize();
        var right = cameraTransform.right; right.y = 0; right.Normalize();
        var velocity = (forward * move.y + right * move.x).normalized * (running ? runSpeed : walkSpeed);
        body.linearVelocity = new Vector3(velocity.x, body.linearVelocity.y, velocity.z);
        if (jumpRequested && Grounded()) body.linearVelocity = new Vector3(velocity.x, jumpSpeed, velocity.z);
        jumpRequested = false;
    }
    bool Grounded() { return capsule != null && Physics.Raycast(capsule.bounds.center, Vector3.down, capsule.bounds.extents.y + .12f, ~0, QueryTriggerInteraction.Ignore); }
    void OnDisable() { move = Vector2.zero; jumpRequested = false; }
}
