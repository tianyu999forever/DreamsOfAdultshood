using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>One optional bonus star, unlocked by three puzzle stars and a return to the poster room.</summary>
[RequireComponent(typeof(BoxCollider))]
public class Mission5_FinalMultiplyPoster_ZhuohengWu : MonoBehaviour
{
    public enum SequenceState { Locked, AwaitingReturn, Falling, ReadyForPickup, Collected }
    public SequenceState State { get; private set; } = SequenceState.Locked;

    [Header("Existing Scene References")]
    [SerializeField] private Mission5_StarInventory_ZhuohengWu inventory;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private Mission5_StarCollectible_ZhuohengWu bonusStar;
    [SerializeField] private Collider pickupTrigger;
    [SerializeField] private SphereCollider physicalCollider;
    [SerializeField] private Rigidbody pickupBody;
    [SerializeField] private Collider landingFloor;
    [SerializeField] private Transform ejectPoint;


    [Header("Existing Area Message Style")]
    [SerializeField] private TMP_Text returnMessage;
    [SerializeField] private PocketWatch messageTimingSource;

    [Header("Pop Out (Eject Point Local Axes; Units Per Second)")]
    [SerializeField] private Vector3 popVelocity = new Vector3(0f, 1.8f, 2.2f);
    [SerializeField, Min(0.01f)] private float settledSpeed = 0.35f;
    [SerializeField, Min(0.01f)] private float settleDuration = 0.25f;

    private BoxCollider roomTrigger;
    private bool ready;
    private bool hasBeenOutside;
    private bool messagePlaying;
    private Color messageColor;
    private float settledTime;

    private void Start()
    {
        roomTrigger = GetComponent<BoxCollider>();
        if (returnMessage != null)
        {
            messageColor = returnMessage.color;
            returnMessage.gameObject.SetActive(false);
        }
        if (bonusStar != null) bonusStar.gameObject.SetActive(false);
        if (inventory == null || playerCollider == null || bonusStar == null || pickupTrigger == null ||
            physicalCollider == null || pickupBody == null || landingFloor == null ||
            ejectPoint == null || returnMessage == null || messageTimingSource == null ||
            !roomTrigger.isTrigger || !pickupTrigger.isTrigger || physicalCollider.isTrigger ||
            bonusStar.GetComponent<Collider>() != pickupTrigger ||
            pickupBody.gameObject != bonusStar.gameObject ||
            physicalCollider.gameObject != bonusStar.gameObject || landingFloor.isTrigger)
        {
            Debug.LogError("Mission 5 bonus star: missing or inconsistent scene references.", this);
            enabled = false;
            return;
        }

        pickupTrigger.enabled = false;
        physicalCollider.enabled = false;
        pickupBody.isKinematic = true;
        pickupBody.useGravity = false;
        ready = true;
    }

    private bool PlayerInsideRoom()
    {
        Vector3 center = playerCollider.bounds.center;
        return (roomTrigger.ClosestPoint(center) - center).sqrMagnitude < 0.0001f;
    }

    private void Update()
    {
        if (!ready) return;
        if (State == SequenceState.Locked)
        {
            if (!inventory.ExitUnlocked) return;

            // This transition is one-way, independent of future UI changes.
            State = SequenceState.AwaitingReturn;
            hasBeenOutside = !PlayerInsideRoom();
            StartCoroutine(ShowReturnMessage());
            // Even an activation inside the room cannot eject immediately: leave
            // and re-enter to count as a return. Update also covers missed trigger events.
            return;
        }

        if (State == SequenceState.AwaitingReturn)
        {
            if (!PlayerInsideRoom()) hasBeenOutside = true;
            TryRelease();
        }
        else if (State == SequenceState.ReadyForPickup &&
            (bonusStar == null || bonusStar.Collected || !pickupTrigger.enabled))
        {
            State = SequenceState.Collected;
            // Collectible already added the star and owns its animation/destruction.
        }
    }

    private void OnTriggerEnter(Collider other) => CheckPlayerReturn(other);
    private void OnTriggerStay(Collider other) => CheckPlayerReturn(other);

    private void CheckPlayerReturn(Collider other)
    {
        if (ready && (other == playerCollider || other.transform.root.CompareTag("Player"))) TryRelease();
    }

    private void TryRelease()
    {
        if (State != SequenceState.AwaitingReturn || !hasBeenOutside || !PlayerInsideRoom()) return;
        if (bonusStar == null || bonusStar.Collected)
        {
            State = SequenceState.Collected;
            return;
        }

        State = SequenceState.Falling; // Set before activation/physics callbacks: never release twice.
        bonusStar.transform.SetPositionAndRotation(ejectPoint.position, Quaternion.identity);
        bonusStar.gameObject.SetActive(true);
        pickupBody.position = ejectPoint.position;
        pickupBody.rotation = Quaternion.identity;
        physicalCollider.enabled = true;
        Physics.IgnoreCollision(physicalCollider, playerCollider, true);
        pickupBody.isKinematic = false;
        pickupBody.useGravity = true;
        pickupBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        pickupBody.angularVelocity = Vector3.zero;
        pickupBody.linearVelocity = ejectPoint.TransformDirection(popVelocity);
        pickupBody.WakeUp();
    }

    private void FixedUpdate()
    {
        if (!ready || State != SequenceState.Falling || pickupBody == null || Mission5_TimeFreeze_Environment_ZhuohengWu.IsFrozenIn(this)) return;
        Vector3 position = pickupBody.position;
        bool grounded = landingFloor.Raycast(new Ray(position + Vector3.up * 0.1f, Vector3.down),
            out RaycastHit hit, 2f) && Vector3.Dot(hit.normal, Vector3.up) > 0.9f &&
            Mathf.Abs(position.y - hit.point.y - physicalCollider.radius) < 0.1f;
        bool slow = pickupBody.linearVelocity.sqrMagnitude <= settledSpeed * settledSpeed;
        settledTime = grounded && slow ? settledTime + Time.fixedDeltaTime : 0f;
        if (settledTime < settleDuration) return;

        pickupBody.linearVelocity = Vector3.zero;
        pickupBody.angularVelocity = Vector3.zero;
        pickupBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        pickupBody.isKinematic = true;
        pickupBody.useGravity = false;
        physicalCollider.enabled = false;
        State = SequenceState.ReadyForPickup;
        pickupTrigger.enabled = true; // Star Collectible now owns the one-time star addition.
    }

    private IEnumerator ShowReturnMessage()
    {
        messagePlaying = true;
        returnMessage.text = "RETURN TO THE STARTING AREA";
        returnMessage.gameObject.SetActive(true);
        float fade = Mathf.Max(0.01f, messageTimingSource.hintFadeDuration);
        yield return FadeMessage(0f, messageColor.a, fade);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, messageTimingSource.hintDuration));
        yield return FadeMessage(messageColor.a, 0f, fade);
        returnMessage.gameObject.SetActive(false);
        returnMessage.color = messageColor;
        messagePlaying = false;
    }

    private IEnumerator FadeMessage(float from, float to, float duration)
    {
        float elapsed = 0f;
        do
        {
            Color color = messageColor;
            color.a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            returnMessage.color = color;
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        } while (elapsed < duration);
        Color endColor = messageColor;
        endColor.a = to;
        returnMessage.color = endColor;
    }

    private void OnDisable()
    {
        if (messagePlaying && returnMessage != null)
        {
            StopAllCoroutines();
            returnMessage.gameObject.SetActive(false);
            returnMessage.color = messageColor;
            messagePlaying = false;
        }
    }
}
