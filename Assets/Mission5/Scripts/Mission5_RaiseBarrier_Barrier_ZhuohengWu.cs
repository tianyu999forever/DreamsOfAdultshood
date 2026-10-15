using UnityEngine;

/// <summary>Holds the physical barrier up while Card 3 is selected; releases it downward.</summary>
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class Mission5_RaiseBarrier_Barrier_ZhuohengWu : MonoBehaviour
{
    [SerializeField] private Mission5_CardHotbar_CardHotbar_ZhuohengWu hotbar;
    [SerializeField] private Vector3 raiseOffset = new Vector3(0f, 3.5f, 0f);
    [SerializeField, Min(0.1f)] private float raiseSpeed = 12f;

    public bool Activated { get; private set; }
    public Collider SolidCollider { get; private set; }

    private Rigidbody body;
    private Vector3 loweredPosition;
    private Vector3 raisedPosition;
    private bool ready;

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        SolidCollider = GetComponent<BoxCollider>();
        if (hotbar == null || SolidCollider.isTrigger || raiseOffset.sqrMagnitude < 0.0001f)
        {
            Debug.LogError("Mission 5 barrier: assign Hotbar, use a solid BoxCollider and a nonzero Raise Offset.", this);
            enabled = false;
            return;
        }

        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        loweredPosition = body.position;
        raisedPosition = loweredPosition + raiseOffset; // World-space offset from its lowered scene position.
        ready = true;
    }

    private void LateUpdate()
    {
        Activated = ready && Time.timeScale > 0f && hotbar.isActiveAndEnabled &&
            hotbar.SelectedSlotIndex == 2;
    }

    private void FixedUpdate()
    {
        if (!ready) return;
        Vector3 targetPosition = Activated ? raisedPosition : loweredPosition;
        body.MovePosition(Vector3.MoveTowards(body.position, targetPosition,
            Mathf.Max(0.1f, raiseSpeed) * Time.fixedDeltaTime));
    }
}
