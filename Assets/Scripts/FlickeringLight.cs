using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class FlickeringLight : MonoBehaviour
{
    [Header("FIND OBJECTS")]
    [SerializeField] private Light[] targetLights;
    [SerializeField] private Renderer[] lightPanels;
    [SerializeField] private LensFlareComponentSRP[] lensFlares;

    [Header("shining setting")]
    [SerializeField] private float minPause = 2f;
    [SerializeField] private float maxPause = 6f;
    [SerializeField] private int minFlashes = 1;
    [SerializeField] private int maxFlashes = 3;
    [SerializeField] private float minOffTime = 0.05f;
    [SerializeField] private float maxOffTime = 0.15f;

    private float[] normalIntensities;
    private Color[] normalEmissions;
    private MaterialPropertyBlock[] propertyBlocks;

    private static readonly int EmissionColor =
        Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        // 自动寻找这组灯下面的所有组件
        if (targetLights == null || targetLights.Length == 0)
            targetLights = GetComponentsInChildren<Light>(true);

        if (lightPanels == null || lightPanels.Length == 0)
            lightPanels = GetComponentsInChildren<Renderer>(true);

        if (lensFlares == null || lensFlares.Length == 0)
            lensFlares =
                GetComponentsInChildren<LensFlareComponentSRP>(true);

        normalIntensities = new float[targetLights.Length];

        for (int i = 0; i < targetLights.Length; i++)
            normalIntensities[i] = targetLights[i].intensity;

        normalEmissions = new Color[lightPanels.Length];
        propertyBlocks =
            new MaterialPropertyBlock[lightPanels.Length];

        for (int i = 0; i < lightPanels.Length; i++)
        {
            propertyBlocks[i] = new MaterialPropertyBlock();

            Material material = lightPanels[i].sharedMaterial;

            if (material != null &&
                material.HasProperty(EmissionColor))
            {
                normalEmissions[i] =
                    material.GetColor(EmissionColor);
            }
        }
    }

    private IEnumerator Start()
    {
        SetLamp(true);

        while (true)
        {
            yield return new WaitForSeconds(
                Random.Range(minPause, maxPause));

            int flashCount =
                Random.Range(minFlashes, maxFlashes + 1);

            for (int i = 0; i < flashCount; i++)
            {
                SetLamp(false);

                yield return new WaitForSeconds(
                    Random.Range(minOffTime, maxOffTime));

                SetLamp(true);

                if (i < flashCount - 1)
                {
                    yield return new WaitForSeconds(
                        Random.Range(0.04f, 0.12f));
                }
            }
        }
    }

    private void SetLamp(bool isOn)
    {
        for (int i = 0; i < targetLights.Length; i++)
        {
            targetLights[i].intensity =
                isOn ? normalIntensities[i] : 0f;
        }

        for (int i = 0; i < lightPanels.Length; i++)
        {
            Renderer panel = lightPanels[i];
            MaterialPropertyBlock block = propertyBlocks[i];

            panel.GetPropertyBlock(block);

            block.SetColor(
                EmissionColor,
                isOn ? normalEmissions[i] : Color.black);

            panel.SetPropertyBlock(block);
        }

        for (int i = 0; i < lensFlares.Length; i++)
            lensFlares[i].enabled = isOn;
    }

    private void OnDisable()
    {
        if (targetLights != null && normalIntensities != null)
            SetLamp(true);
    }
}