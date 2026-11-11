using UnityEngine;
using TMPro;

public class Mission2_LightDoor_Door_TianyuTang : MonoBehaviour
{
    [Header("Flashlight — assign in the scene")]
    [SerializeField]
    private Mission2_Flashlight_MainCamera_TianyuTang flashlightController;

    [SerializeField] private Light flashlight;

    [Header("Door")]
    [SerializeField] private Transform doorPanel;
    [SerializeField] private TMP_Text promptText;

    [Header("Charging")]
    [SerializeField, Min(0.1f)]
    private float detectionDistance = 5f;

    [SerializeField, Min(0.1f)]
    private float chargeDuration = 1f;

    [SerializeField] private LayerMask blockingLayers = ~0;

    [Header("Sliding")]
    [SerializeField] private Vector3 slideOffset = new Vector3(3f, 0f, 0f);

    [SerializeField, Min(0.1f)]
    private float slideSpeed = 4f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private float charge;
    private bool opened;

    private void Start()
    {
        if (doorPanel == null ||
            flashlightController == null ||
            flashlight == null)
        {
            Debug.LogError(
                "Assign Door Panel, Flashlight Controller and Flashlight.",
                this);

            enabled = false;
            return;
        }

        closedPosition = doorPanel.localPosition;
        openPosition = closedPosition + slideOffset;

        UpdatePrompt();
    }

    private void Update()
    {
        if (opened)
        {
            doorPanel.localPosition = Vector3.MoveTowards(
                doorPanel.localPosition,
                openPosition,
                slideSpeed * Time.deltaTime);

            return;
        }

        if (IsLightPointingAtDoor())
        {
            charge = Mathf.Min(
                charge + Time.deltaTime,
                chargeDuration);

            if (charge >= chargeDuration)
                opened = true;
        }
        else
        {
            charge = 0f;
        }

        UpdatePrompt();
    }

    private bool IsLightPointingAtDoor()
    {
        if (!flashlightController.IsOn ||
            !flashlight.gameObject.activeInHierarchy)
        {
            return false;
        }

        float reach = Mathf.Min(
            detectionDistance,
            flashlight.range);

        Ray ray = new Ray(
            flashlight.transform.position,
            flashlight.transform.forward);

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                reach,
                blockingLayers,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return hit.transform == doorPanel ||
            hit.transform.IsChildOf(doorPanel);
    }

    private void UpdatePrompt()
    {
        if (promptText == null)
            return;

        if (opened)
        {
            promptText.text = "OPEN";
            promptText.color = Color.green;
        }
        else if (charge > 0f)
        {
            int percent = Mathf.FloorToInt(
                charge / chargeDuration * 100f);

            promptText.text = $"CHARGING {percent}%";
            promptText.color = Color.yellow;
        }
        else
        {
            promptText.text = "SHINE LIGHT TO OPEN";
            promptText.color = Color.white;
        }
    }
}