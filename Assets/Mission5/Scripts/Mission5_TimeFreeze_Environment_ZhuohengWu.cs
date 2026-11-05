using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>Scene-local motion pause; gameplay clocks, input and player presentation keep running.</summary>
[DefaultExecutionOrder(-1500)]
public class Mission5_TimeFreeze_Environment_ZhuohengWu : MonoBehaviour
{
    [SerializeField] private Mission5_CardHotbar_CardHotbar_ZhuohengWu hotbar;

    private static readonly List<Mission5_TimeFreeze_Environment_ZhuohengWu> instances = new();
    private readonly Dictionary<Rigidbody, BodyState> bodies = new();
    private readonly Dictionary<Animator, float> animators = new();
    private readonly Dictionary<AnimationState, float> legacyAnimations = new();
    private readonly List<PlayableDirector> directors = new();
    private readonly List<ParticleSystem> particles = new();

    public bool IsFrozen => isActiveAndEnabled && hotbar != null && hotbar.isActiveAndEnabled &&
        hotbar.SelectedSlotIndex == 4 && Time.timeScale > 0f;

    // Movement scripts cooperate with the same scene-local clock, including coroutine progress.
    public static bool IsFrozenIn(Component target)
    {
        if (target == null) return false;
        foreach (var instance in instances)
            if (instance != null && instance.gameObject.scene == target.gameObject.scene && instance.IsFrozen)
                return true;
        return false;
    }

    private sealed class BodyState
    {
        public Vector3 Position, Velocity, AngularVelocity;
        public Quaternion Rotation;
        public bool Kinematic, Gravity, Sleeping;
        public RigidbodyInterpolation Interpolation;
        public CollisionDetectionMode Detection;

        public BodyState(Rigidbody body)
        {
            Position = body.position;
            Rotation = body.rotation;
            Kinematic = body.isKinematic;
            Gravity = body.useGravity;
            Sleeping = body.IsSleeping();
            Velocity = body.linearVelocity;
            AngularVelocity = body.angularVelocity;
            Interpolation = body.interpolation;
            Detection = body.collisionDetectionMode;
            if (!Kinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
            Hold(body);
        }

        public void Hold(Rigidbody body)
        {
            body.position = Position;
            body.rotation = Rotation;
            body.transform.SetPositionAndRotation(Position, Rotation);
        }

        public void Restore(Rigidbody body)
        {
            Hold(body);
            body.isKinematic = Kinematic;
            body.useGravity = Gravity;
            body.collisionDetectionMode = Detection;
            body.interpolation = Interpolation;
            if (!Kinematic)
            {
                body.linearVelocity = Velocity;
                body.angularVelocity = AngularVelocity;
                if (Sleeping) body.Sleep(); else body.WakeUp();
            }
        }
    }

    private void Awake()
    {
        if (hotbar != null) return;
        Debug.LogError("Mission 5 Time Freeze: assign the scene Card Hotbar.", this);
        enabled = false;
    }

    private void OnEnable()
    {
        instances.Add(this);
        if (hotbar != null) hotbar.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(int slot)
    {
        if (IsFrozen) CaptureMotion(); else RestoreMotion();
    }

    private bool Eligible(Component target)
    {
        // The watch and held fire effects are inside the player hierarchy, as is the camera.
        return target != null && target.gameObject.scene == gameObject.scene &&
            target.gameObject.activeInHierarchy && target.GetComponentInParent<Moving>() == null &&
            target.GetComponentInParent<Camera>() == null && target.GetComponentInParent<Canvas>() == null &&
            target.GetComponentInParent<PocketWatch>() == null;
    }

    private bool OwnsBody(Rigidbody body)
    {
        if (!Eligible(body)) return false;
        // These controllers retain their own kinematic targets/return anchors. The safe also
        // operates its visible access interlock while the rotor is frozen.
        if (body.GetComponentInParent<Mission5_RotatingCase_DisplayCase_ZhuohengWu>() != null) return false;
        var gravity = body.GetComponent<Mission5_ReverseGravity_MovableObject_ZhuohengWu>();
        return gravity == null || !gravity.IsMoving;
    }

    private void Update() => Synchronize();
    private void FixedUpdate() => Synchronize();
    private void LateUpdate() => Synchronize();

    private void Synchronize()
    {
        if (!IsFrozen) { RestoreMotion(); return; }
        CaptureMotion();
        foreach (var pair in bodies)
            if (pair.Key != null) pair.Value.Hold(pair.Key);
    }

    private void CaptureMotion()
    {
        // Re-scan while held: a newly activated/ejected star must freeze before its next
        // physics step. LateUpdate also captures objects created by later Update callbacks.
        foreach (var body in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            if (OwnsBody(body) && !bodies.ContainsKey(body)) bodies.Add(body, new BodyState(body));
        foreach (var animator in FindObjectsByType<Animator>(FindObjectsSortMode.None))
            if (Eligible(animator) && animator.enabled && !animators.ContainsKey(animator))
            {
                animators.Add(animator, animator.speed);
                animator.speed = 0f;
            }
        foreach (var animation in FindObjectsByType<Animation>(FindObjectsSortMode.None))
            if (Eligible(animation) && animation.enabled)
                foreach (AnimationState state in animation)
                    if (!legacyAnimations.ContainsKey(state))
                    {
                        legacyAnimations.Add(state, state.speed);
                        state.speed = 0f;
                    }
        foreach (var particle in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            if (Eligible(particle) && particle.isPlaying && !particles.Contains(particle))
            {
                particles.Add(particle);
                particle.Pause(false);
            }
        foreach (var director in FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None))
            if (Eligible(director) && director.state == PlayState.Playing && !directors.Contains(director))
            {
                directors.Add(director);
                director.Pause();
            }
    }

    private void RestoreMotion()
    {
        foreach (var pair in bodies)
            if (pair.Key != null) pair.Value.Restore(pair.Key);
        bodies.Clear();
        foreach (var pair in animators)
            if (pair.Key != null) pair.Key.speed = pair.Value;
        animators.Clear();
        foreach (var pair in legacyAnimations)
            if (pair.Key != null) pair.Key.speed = pair.Value;
        legacyAnimations.Clear();
        foreach (var director in directors)
            if (director != null && director.gameObject.activeInHierarchy) director.Resume();
        directors.Clear();
        foreach (var particle in particles)
            if (particle != null && particle.gameObject.activeInHierarchy && particle.isPaused) particle.Play(false);
        particles.Clear();
    }

    private void OnDisable()
    {
        if (hotbar != null) hotbar.SelectionChanged -= OnSelectionChanged;
        instances.Remove(this);
        RestoreMotion();
    }
}
