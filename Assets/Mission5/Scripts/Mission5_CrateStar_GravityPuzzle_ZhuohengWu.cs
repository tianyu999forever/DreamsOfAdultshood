using UnityEngine;

/// <summary>The crate physically conceals the stationary star; pickup requires safe headroom.</summary>
public class Mission5_CrateStar_GravityPuzzle_ZhuohengWu : MonoBehaviour
{
    [SerializeField] private Collider crateCollider;
    [SerializeField] private Mission5_StarCollectible_ZhuohengWu hiddenStar;
    [SerializeField] private Collider pickupTrigger;
    [SerializeField] private Collider playerCollider;
    [SerializeField, Min(0f)] private float headroomMargin = 0.15f;

    private float floorHeight;
    private bool collected;

    private void Awake()
    {
        if (pickupTrigger != null) pickupTrigger.enabled = false;
        if (crateCollider == null || hiddenStar == null || pickupTrigger == null || playerCollider == null ||
            !pickupTrigger.isTrigger || pickupTrigger.gameObject != hiddenStar.gameObject)
        {
            Debug.LogError("Mission 5 crate star: assign the solid Crate Collider, Hidden Star, " +
                "its Pickup Trigger, and Player Collider.", this);
            enabled = false;
            return;
        }
        floorHeight = crateCollider.bounds.min.y;
        hiddenStar.CollectedSuccessfully += OnCollected;
    }

    private void FixedUpdate()
    {
        if (collected || hiddenStar == null) return;
        // The star is independent of the crate, so its position never moves with the prop.
        // Gate before physics callbacks as the crate falls; it cannot be collected through wood.
        float clearance = playerCollider.bounds.size.y + headroomMargin;
        pickupTrigger.enabled = crateCollider.bounds.min.y >= floorHeight + clearance;
    }

    private void OnCollected()
    {
        collected = true;
        pickupTrigger.enabled = false;
    }

    private void OnDisable()
    {
        if (pickupTrigger != null) pickupTrigger.enabled = false;
    }

    private void OnDestroy()
    {
        if (hiddenStar != null) hiddenStar.CollectedSuccessfully -= OnCollected;
    }
}
