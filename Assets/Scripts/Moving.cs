using UnityEngine;
using UnityEngine.InputSystem;

public class Moving : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpSpeed = 5f;
    public Transform cameraTransform;

    [Header("Footsteps")]
    public AudioSource footstepSource;
    public AudioClip[] footstepClips;

    public float walkStepInterval = 0.48f;
    public float runStepInterval = 0.30f;

    [Range(0f, 1f)]
    public float walkVolume = 0.4f;

    [Range(0f, 1f)]
    public float runVolume = 0.6f;

    Rigidbody rb;
    Collider playerCollider;

    float x;
    float z;
    bool running;
    bool jumpPressed;
    float stepTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();

        if (footstepSource == null)
            footstepSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        x = 0;
        z = 0;

        if (Keyboard.current.wKey.isPressed) z += 1;
        if (Keyboard.current.sKey.isPressed) z -= 1;
        if (Keyboard.current.aKey.isPressed) x -= 1;
        if (Keyboard.current.dKey.isPressed) x += 1;

        running = Keyboard.current.leftShiftKey.isPressed;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpPressed = true;

        UpdateFootsteps();
    }

    void FixedUpdate()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        float speed = running ? runSpeed : walkSpeed;

        Vector3 move =
            (forward * z + right * x).normalized * speed;

        rb.linearVelocity =
            new Vector3(move.x, rb.linearVelocity.y, move.z);

        if (jumpPressed && IsGrounded())
        {
            rb.linearVelocity =
                new Vector3(
                    rb.linearVelocity.x,
                    jumpSpeed,
                    rb.linearVelocity.z
                );
        }

        jumpPressed = false;
    }

    void UpdateFootsteps()
    {
        bool isMoving =
            Mathf.Abs(x) > 0.01f ||
            Mathf.Abs(z) > 0.01f;

        if (!isMoving || !IsGrounded())
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            PlayFootstep();

            stepTimer = running
                ? runStepInterval
                : walkStepInterval;
        }
    }

    void PlayFootstep()
    {
        if (footstepSource == null ||
            footstepClips == null ||
            footstepClips.Length == 0)
        {
            return;
        }

        AudioClip clip =
            footstepClips[
                Random.Range(0, footstepClips.Length)
            ];

        float volume;

        if (running)
        {
            volume = runVolume;
            footstepSource.pitch =
                Random.Range(1.02f, 1.12f);
        }
        else
        {
            volume = walkVolume;
            footstepSource.pitch =
                Random.Range(0.92f, 1.02f);
        }

        footstepSource.PlayOneShot(clip, volume);
    }

    bool IsGrounded()
    {
        float distance =
            playerCollider.bounds.extents.y + 0.1f;

        return Physics.Raycast(
            transform.position,
            Vector3.down,
            distance
        );
    }
}