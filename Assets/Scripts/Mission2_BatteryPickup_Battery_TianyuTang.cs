using UnityEngine;

public class Mission2_BatteryPickup_Battery_TianyuTang : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float rechargeSeconds = 7f;

    [Header("Pickup Effect")]
    [SerializeField]
    private ParticleSystem pickupEffectPrefab;

    [SerializeField]
    private Vector3 effectOffset = Vector3.zero;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        TryPickup(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryPickup(other);
    }

    private void TryPickup(Collider other)
    {
        if (collected)
            return;

        Transform player = other.transform;

        while (player != null && !player.CompareTag("Player"))
        {
            player = player.parent;
        }

        if (player == null)
            return;

        var flashlight = player.GetComponentInChildren<
            Mission2_Flashlight_MainCamera_TianyuTang>();

        if (flashlight == null)
            return;

        if (flashlight.BatteryPercent >= 0.999f)
            return;

        collected = true;
        flashlight.AddBattery(rechargeSeconds);

        if (pickupEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(
                pickupEffectPrefab,
                transform.position + effectOffset,
                pickupEffectPrefab.transform.rotation);

            effect.gameObject.SetActive(true);

            effect.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            effect.Play(true);

            // Safety cleanup after the short pickup effect finishes.
            Destroy(effect.gameObject, 5f);
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}