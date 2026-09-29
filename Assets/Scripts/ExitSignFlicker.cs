using System.Collections;
using TMPro;
using UnityEngine;

public class ExitSignFlicker : MonoBehaviour
{
    [SerializeField] private TMP_Text exitText;
    [SerializeField] private Light glowLight;

    [Header("Flicker Settings")]
    [SerializeField] private float minPause = 1.5f;
    [SerializeField] private float maxPause = 5f;
    [SerializeField] private int minFlashes = 1;
    [SerializeField] private int maxFlashes = 4;
    [SerializeField] private float minOffTime = 0.04f;
    [SerializeField] private float maxOffTime = 0.22f;
    [SerializeField, Range(0f, 1f)]
    private float dimBrightness = 0.05f;

    private float normalLightIntensity;

    private void Awake()
    {
        if (exitText == null)
            exitText = GetComponent<TMP_Text>();

        if (glowLight == null)
            glowLight = GetComponentInChildren<Light>();

        if (glowLight != null)
            normalLightIntensity = glowLight.intensity;
    }

    private IEnumerator Start()
    {
        SetBrightness(1f);

        while (true)
        {
            yield return new WaitForSeconds(
                Random.Range(minPause, maxPause));

            int flashes =
                Random.Range(minFlashes, maxFlashes + 1);

            for (int i = 0; i < flashes; i++)
            {
                SetBrightness(dimBrightness);

                yield return new WaitForSeconds(
                    Random.Range(minOffTime, maxOffTime));

                SetBrightness(1f);

                if (i < flashes - 1)
                {
                    yield return new WaitForSeconds(
                        Random.Range(0.04f, 0.12f));
                }
            }
        }
    }

    private void SetBrightness(float brightness)
    {
        if (exitText != null)
            exitText.alpha = brightness;

        if (glowLight != null)
        {
            glowLight.intensity =
                normalLightIntensity * brightness;
        }
    }

    private void OnDisable()
    {
        SetBrightness(1f);
    }
}