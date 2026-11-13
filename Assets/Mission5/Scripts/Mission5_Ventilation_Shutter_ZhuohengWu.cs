using System.Collections.Generic;
using UnityEngine;

/// <summary>A rotating wall vent whose frozen opening is judged by actual star visibility.</summary>
public class Mission5_Ventilation_Shutter_ZhuohengWu : MonoBehaviour
{
    [SerializeField] private Mission5_CardHotbar_CardHotbar_ZhuohengWu hotbar;
    [SerializeField] private Rigidbody rotatingCase;
    [SerializeField] private Mission5_StarCollectible_ZhuohengWu rewardStar;
    [SerializeField] private Collider pickupTrigger;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private Transform stationaryMount;
    [SerializeField] private Transform[] louvers;
    [SerializeField, Min(1f)] private float rotationSpeed = 144f;
    [SerializeField, Min(0.1f)] private float louverSpeed = 7f;
    [SerializeField] private float startingAngle = 110f;

    public bool IsFrozen => hotbar != null && hotbar.isActiveAndEnabled &&
        hotbar.SelectedSlotIndex == 4 && Time.timeScale > 0f;
    // Preserve the public interface, but use visible mesh samples instead of a fixed angle.
    public bool IsAligned => apertureExposesStar;
    public bool AccessOpen => IsFrozen && (rewardCollected || apertureExposesStar) && opening >= 1f;
    public float CurrentAngle => angle;

    private float angle, opening;
    private bool rewardCollected, apertureExposesStar;
    private Transform starMesh;
    private Camera viewCamera;
    private readonly List<Vector3> samples = new List<Vector3>();
    private readonly HashSet<Collider> innerBlades = new HashSet<Collider>();
    private readonly RaycastHit[] rayHits = new RaycastHit[32];

    private void Awake()
    {
        if (pickupTrigger != null) pickupTrigger.enabled = false;
        if (hotbar == null || rotatingCase == null || rewardStar == null || pickupTrigger == null ||
            playerCollider == null || stationaryMount == null || louvers == null || louvers.Length == 0 ||
            !rotatingCase.isKinematic || !pickupTrigger.isTrigger ||
            pickupTrigger.gameObject != rewardStar.gameObject || rewardStar.transform.IsChildOf(transform))
        {
            Debug.LogError("Mission 5 ventilation: assign the rotor, independent star, mount, player and louvers.", this);
            enabled = false;
            return;
        }
        foreach (Transform louver in louvers)
        {
            if (louver == null)
            {
                Debug.LogError("Mission 5 ventilation: every louver reference must be assigned.", this);
                enabled = false;
                return;
            }
            foreach (Collider shape in louver.GetComponentsInChildren<Collider>()) innerBlades.Add(shape);
        }
        var meshFilter = rewardStar.Visual.GetComponentInChildren<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null || !meshFilter.sharedMesh.isReadable)
        {
            Debug.LogError("Mission 5 ventilation: the reward needs its readable star mesh.", this);
            enabled = false;
            return;
        }
        starMesh = meshFilter.transform;
        CacheSurfaceSamples(meshFilter.sharedMesh);
        viewCamera = playerCollider.GetComponentInChildren<Camera>();
        angle = startingAngle;
        rotatingCase.rotation = transform.rotation * Quaternion.AngleAxis(angle, Vector3.forward);
        ApplyLouvers();
    }

    private void CacheSurfaceSamples(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] indices = mesh.triangles;
        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector3 a = vertices[indices[i]], b = vertices[indices[i + 1]], c = vertices[indices[i + 2]];
            if (Mathf.Abs(Vector3.Cross(b - a, c - a).normalized.z) < 0.8f) continue;
            Vector3 center = (a + b + c) / 3f;
            AddSample(center);
            AddSample(Vector3.Lerp(center, a, 0.45f));
            AddSample(Vector3.Lerp(center, b, 0.45f));
            AddSample(Vector3.Lerp(center, c, 0.45f));
        }
    }

    private void AddSample(Vector3 point)
    {
        // Deduplicate the two faces; two clear interior samples represent a real visible patch.
        point.z = 0f;
        foreach (Vector3 sample in samples)
            if ((sample - point).sqrMagnitude < 0.000001f) return;
        samples.Add(point);
    }

    private void OnEnable()
    {
        if (hotbar != null) hotbar.SelectionChanged += OnSelectionChanged;
        if (rewardStar != null) rewardStar.CollectedSuccessfully += OnStarCollected;
    }

    private void OnStarCollected()
    {
        if (rewardCollected) return;
        rewardCollected = true;
        pickupTrigger.enabled = false;
        // The shared inventory owns scoring and notification. Only hide this frozen reward.
        if (rewardStar.Visual != null) rewardStar.Visual.gameObject.SetActive(false);
    }

    private void OnSelectionChanged(int slot)
    {
        if (slot == 4 && rotatingCase != null)
        {
            angle = (Quaternion.Inverse(transform.rotation) * rotatingCase.rotation).eulerAngles.z;
            rotatingCase.transform.rotation = rotatingCase.rotation;
        }
        if (slot != 4 && pickupTrigger != null) pickupTrigger.enabled = false;
    }

    private bool AtFront()
    {
        Vector3 local = stationaryMount.InverseTransformPoint(playerCollider.bounds.center);
        return local.z > 0.35f && local.z < 2.1f &&
            Mathf.Abs(local.x - 0.42f) < 1f && Mathf.Abs(local.y) < 1.25f;
    }

    private Vector3 VisibilityOrigin(bool nearby)
    {
        if (nearby && viewCamera != null) return viewCamera.transform.position;
        return stationaryMount.TransformPoint(new Vector3(0.42f, 0f, 1.4f));
    }

    private void FixedUpdate()
    {
        if (Time.timeScale <= 0f) return;
        bool nearby = AtFront();
        bool hasStar = rewardStar != null && !rewardStar.Collected;
        Vector3 origin = VisibilityOrigin(nearby);
        // Ignore only the inner louvers when deciding whether the outer aperture exposes
        // the star. Include them again for actual pickup after they have physically opened.
        apertureExposesStar = hasStar && IsFrozen && ExposedFrom(origin, true);
        bool unlock = IsFrozen && (rewardCollected || apertureExposesStar);
        opening = Mathf.MoveTowards(opening, unlock ? 1f : 0f, louverSpeed * Time.fixedDeltaTime);
        ApplyLouvers();
        if (!IsFrozen && opening == 0f)
        {
            angle = Mathf.Repeat(angle + rotationSpeed * Time.fixedDeltaTime, 360f);
            rotatingCase.MoveRotation(transform.rotation * Quaternion.AngleAxis(angle, Vector3.forward));
        }
        Physics.SyncTransforms();
        if (!hasStar || pickupTrigger == null) return;
        pickupTrigger.enabled = AccessOpen && nearby && ExposedFrom(origin, false);
        if (pickupTrigger.enabled && Physics.ComputePenetration(pickupTrigger,
            pickupTrigger.transform.position, pickupTrigger.transform.rotation, playerCollider,
            playerCollider.transform.position, playerCollider.transform.rotation, out _, out _))
            rewardStar.TryCollect(playerCollider);
    }

    private bool ExposedFrom(Vector3 origin, bool ignoreInnerBlades)
    {
        int visible = 0;
        foreach (Vector3 sample in samples)
            if (ClearRay(origin, starMesh.TransformPoint(sample), ignoreInnerBlades) && ++visible >= 2)
                return true;
        return false;
    }

    private bool ClearRay(Vector3 origin, Vector3 target, bool ignoreInnerBlades)
    {
        Vector3 delta = target - origin;
        int count = Physics.RaycastNonAlloc(origin, delta.normalized, rayHits, delta.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider shape = rayHits[i].collider;
            if (shape == playerCollider || shape.transform.IsChildOf(playerCollider.transform) ||
                (ignoreInnerBlades && innerBlades.Contains(shape))) continue;
            return false;
        }
        return true;
    }

    private void ApplyLouvers()
    {
        foreach (Transform louver in louvers)
            louver.localRotation = Quaternion.Euler(Mathf.Lerp(55f, 85f, opening), 0f, 0f);
    }

    private void OnDisable()
    {
        if (hotbar != null) hotbar.SelectionChanged -= OnSelectionChanged;
        if (rewardStar != null) rewardStar.CollectedSuccessfully -= OnStarCollected;
        if (pickupTrigger != null) pickupTrigger.enabled = false;
    }
}
