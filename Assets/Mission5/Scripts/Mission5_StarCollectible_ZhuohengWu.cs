using System.Collections;
using UnityEngine;

/// <summary>A gated, single-use star. Puzzle controllers own when its trigger is enabled.</summary>
public class Mission5_StarCollectible_ZhuohengWu : MonoBehaviour
{
    [SerializeField] private Mission5_StarInventory_ZhuohengWu inventory;
    [SerializeField] private string starId;
    [SerializeField] private Transform visual;
    [SerializeField] private Collider pickupTrigger;
    [SerializeField] private Transform viewCamera;
    public bool Collected { get; private set; }
    public Transform Visual => visual;
    public event System.Action CollectedSuccessfully;

    private void OnTriggerEnter(Collider other) => TryCollect(other);
    private void OnTriggerStay(Collider other) => TryCollect(other);

    public bool TryCollect(Collider other)
    {
        if (Collected || !isActiveAndEnabled || inventory == null || pickupTrigger == null ||
            !pickupTrigger.enabled || visual == null || !visual.gameObject.activeInHierarchy ||
            other == null || !(other.CompareTag("Player") || other.transform.root.CompareTag("Player")))
            return false;
        if (!inventory.Register(starId)) return false;
        Collected = true;
        pickupTrigger.enabled = false;
        StartCoroutine(PickupAnimation());
        CollectedSuccessfully?.Invoke();
        return true;
    }

    private void LateUpdate()
    {
        if (Collected || visual == null || viewCamera == null || Mission5_TimeFreeze_Environment_ZhuohengWu.IsFrozenIn(this)) return;
        Vector3 direction = viewCamera.position - visual.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) visual.rotation = Quaternion.LookRotation(direction);
    }

    private IEnumerator PickupAnimation()
    {
        Vector3 position = visual.localPosition;
        Vector3 scale = visual.localScale;
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            if (Mission5_TimeFreeze_Environment_ZhuohengWu.IsFrozenIn(this)) { yield return null; continue; }
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.35f);
            visual.localPosition = position + Vector3.up * (0.8f * t);
            visual.localScale = Vector3.Lerp(scale, Vector3.zero, t);
            yield return null;
        }
        Destroy(gameObject);
    }
}
