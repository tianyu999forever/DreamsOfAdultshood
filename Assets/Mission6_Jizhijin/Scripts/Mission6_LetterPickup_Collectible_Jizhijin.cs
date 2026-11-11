using System.Collections;
using UnityEngine;

public class Mission6_LetterPickup_Collectible_Jizhijin : MonoBehaviour
{
    public string letter = "D";
    public Mission6_Letters_GameManager_Jizhijin inventory;
    public ParticleSystem pickupParticlePrefab;
    public Transform visual;
    bool collected;

    void OnTriggerEnter(Collider other)
    {
        if (collected || (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player"))) return;
        if (inventory == null || !inventory.AddLetter(letter)) return;
        collected = true;
        GetComponent<Collider>().enabled = false;
        var floating = GetComponent<FloatingItem>();
        if (floating != null) floating.enabled = false;
        if (pickupParticlePrefab != null)
        {
            var particles = Instantiate(pickupParticlePrefab, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, 3f);
        }
        StartCoroutine(AnimatePickup());
    }

    IEnumerator AnimatePickup()
    {
        if (visual == null) visual = transform;
        var position = visual.localPosition;
        var scale = visual.localScale;
        float elapsed = 0;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.4f);
            visual.localPosition = position + Vector3.up * t * 0.8f;
            visual.localScale = Vector3.Lerp(scale, Vector3.zero, t);
            visual.Rotate(0, 500f * Time.deltaTime, 0);
            yield return null;
        }
        Destroy(gameObject);
    }
}
