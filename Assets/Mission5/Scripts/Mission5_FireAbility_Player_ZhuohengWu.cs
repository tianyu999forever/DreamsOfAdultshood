using UnityEngine;

/// <summary>Uses the existing hotbar selection; particles are visual only.</summary>
public class Mission5_FireAbility_Player_ZhuohengWu : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Mission5_CardHotbar_CardHotbar_ZhuohengWu hotbar;
    [SerializeField] private Transform fireOrigin;
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private Mission5_IcePuzzle_IceSphere_ZhuohengWu targetIce;

    [Header("Detection Box (World Units)")]
    [SerializeField, Min(0.1f)] private float fireLength = 4f;
    [SerializeField, Min(0.1f)] private float fireWidth = 1.4f;
    [SerializeField, Min(0.1f)] private float fireHeight = 1.4f;

    private bool emitting;

    private void Awake()
    {
        if (hotbar == null || fireOrigin == null || fireParticles == null || targetIce == null)
        {
            Debug.LogError("Mission 5 fire: assign Hotbar, Fire Origin, Fire Particles and Target Ice.", this);
            enabled = false;
        }
        SetFireVisible(false);
    }

    private void LateUpdate()
    {
        // Hotbar.Update has already run, so release stops fire in this same rendered frame.
        bool active = Time.timeScale > 0f && hotbar.isActiveAndEnabled &&
            hotbar.SelectedSlotIndex == 0;
        SetFireVisible(active);
        if (!active || targetIce == null) return;

        Vector3 center = fireOrigin.position + fireOrigin.forward * (fireLength * 0.5f);
        Vector3 halfSize = new Vector3(fireWidth, fireHeight, fireLength) * 0.5f;
        Collider[] hits = Physics.OverlapBox(center, halfSize, fireOrigin.rotation,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            if (hit.GetComponentInParent<Mission5_IcePuzzle_IceSphere_ZhuohengWu>() != targetIce)
                continue;
            targetIce.TryMelt();
            break;
        }
    }

    private void SetFireVisible(bool active)
    {
        if (fireParticles == null) return;
        if (active)
        {
            if (!emitting) fireParticles.Play(true);
        }
        else if (emitting || fireParticles.isPlaying || fireParticles.particleCount > 0)
        {
            // Clear existing particles too: key release has no lingering visual flame.
            fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        emitting = active;
    }

    private void OnDisable() => SetFireVisible(false);

    private void OnDrawGizmosSelected()
    {
        if (fireOrigin == null) return;
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(fireOrigin.position, fireOrigin.rotation, Vector3.one);
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.8f);
        Gizmos.DrawWireCube(Vector3.forward * (fireLength * 0.5f),
            new Vector3(fireWidth, fireHeight, fireLength));
        Gizmos.matrix = previous;
    }
}
