using UnityEngine;

/// <summary>One tutorial-only pickup, gated until the ice has finished melting and dropping the star.</summary>
[RequireComponent(typeof(SphereCollider))]
public class Tutorial_StarCollectible_Star_ZhuohengWu : MonoBehaviour
{
    [SerializeField] private Tutorial_IceLesson_Tutorial_ZhuohengWu lesson;
    [SerializeField] private SphereCollider pickupTrigger;
    [SerializeField] private Transform visual;
    [SerializeField] private Transform viewCamera;
    public bool Collected { get; private set; }

    public void SetPickupEnabled(bool value)
    {
        if (pickupTrigger != null) pickupTrigger.enabled = value && !Collected;
    }

    private void OnTriggerEnter(Collider other) => TryCollect(other);
    private void OnTriggerStay(Collider other) => TryCollect(other);

    public bool TryCollect(Collider other)
    {
        if (Collected || !isActiveAndEnabled || lesson == null || pickupTrigger == null ||
            !pickupTrigger.enabled || !lesson.StarReady || other == null ||
            !(other.CompareTag("Player") || other.transform.root.CompareTag("Player"))) return false;
        if (!lesson.CompleteTutorial()) return false;
        Collected = true;
        pickupTrigger.enabled = false;
        gameObject.SetActive(false);
        return true;
    }

    private void LateUpdate()
    {
        if (Collected || visual == null || viewCamera == null) return;
        Vector3 direction = viewCamera.position - visual.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) visual.rotation = Quaternion.LookRotation(direction);
    }
}
