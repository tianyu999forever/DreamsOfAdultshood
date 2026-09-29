using UnityEngine;
using System.Collections;

public class Collectible : MonoBehaviour
{
    public enum ItemType
    {
        Number,
        Operator
    }

    public ItemType itemType;
    public ParticleSystem pickupParticlePrefab;
    public int numberValue;
    public string operatorValue;

    public Transform visual;

    public float animationTime = 0.5f;
    public float riseHeight = 0.8f;
    public float spinSpeed = 500f;

    bool collected;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger touched by: " + other.name);

        bool isPlayer =
            other.CompareTag("Player") ||
            other.transform.root.CompareTag("Player");

        if (!isPlayer || collected)
            return;

        collected = true;
        PlayPickupParticles();
        if (GameManager.instance != null)
        {
            if (itemType == ItemType.Number)
                GameManager.instance.AddNumber(numberValue);
            else
                GameManager.instance.AddOperator(operatorValue);
        }
        else
        {
            Debug.LogError("GameManager not found!");
        }

        GetComponent<Collider>().enabled = false;

        FloatingItem floating = GetComponent<FloatingItem>();

        if (floating != null)
            floating.enabled = false;

        StartCoroutine(PickupAnimation());
    }

    IEnumerator PickupAnimation()
    {
        if (visual == null)
            visual = transform;

        Vector3 startPosition = visual.localPosition;
        Vector3 startScale = visual.localScale;

        float elapsed = 0;

        while (elapsed < animationTime)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / animationTime;

            visual.localPosition =
                startPosition + Vector3.up * riseHeight * t;

            visual.localScale =
                Vector3.Lerp(startScale, Vector3.zero, t);

            visual.Rotate(
                0,
                spinSpeed * Time.deltaTime,
                0
            );

            yield return null;
        }

        Destroy(gameObject);
    }
    void PlayPickupParticles()
    {
        if (pickupParticlePrefab == null)
            return;

        Vector3 effectPosition = transform.position;

        if (visual != null)
            effectPosition = visual.position;

        ParticleSystem particles = Instantiate(
            pickupParticlePrefab,
            effectPosition,
            Quaternion.identity
        );

        particles.Play();

        Destroy(particles.gameObject, 3f);
    }
}