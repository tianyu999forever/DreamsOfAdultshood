using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Mission3-only room plaques. The scene and shared font assets remain unchanged.
[DisallowMultipleComponent, RequireComponent(typeof(TextMeshPro))]
[DefaultExecutionOrder(1100)]
public class mission3RoomSign_World_ShengChiehLee : MonoBehaviour
{
    public float linePixelsAt900 = 28;
    public float maximumWorldWidth = 2.8f;
    public float visibleDistance = 18;
    public Camera Viewer { get; private set; }
    public int LineCount { get; private set; }
    TextMeshPro label;
    MeshRenderer textRenderer, plateRenderer;
    Transform plate;
    Mesh plateMesh;
    Material fontMaterial;
    Vector3 anchor;
    Bounds glyphBounds;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() { SceneManager.sceneLoaded -= Install; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register() { SceneManager.sceneLoaded -= Install; SceneManager.sceneLoaded += Install; }
    static void Install(Scene scene, LoadSceneMode mode)
    {
        if (scene.path != "Assets/Scenes/Mission3.unity") return;
        var roots = scene.GetRootGameObjects();
        var manager = roots.SelectMany(root => root.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).FirstOrDefault();
        if (manager == null || manager.playerCamera == null) return;
        var texts = roots.SelectMany(root => root.GetComponentsInChildren<TextMeshPro>(true)).ToArray();
        string[] targets = { "301 sign", "302 sign", "303 sign", "301 identity", "302 identity", "303 identity", "Lounge sign" };
        foreach (var text in texts.Where(text => targets.Contains(text.name)))
        {
            // The old 301/303 plaques overlap their newer room identities. Keep a single readable label there.
            var identity = texts.FirstOrDefault(other => other.name == text.name.Replace(" sign", " identity"));
            if (text.name.EndsWith(" sign") && identity != null && identity != text && Vector3.Distance(text.transform.position, identity.transform.position) < 1.5f)
            { text.gameObject.SetActive(false); continue; }
            var sign = text.GetComponent<mission3RoomSign_World_ShengChiehLee>();
            if (sign == null) sign = text.gameObject.AddComponent<mission3RoomSign_World_ShengChiehLee>();
            sign.Viewer = manager.playerCamera; sign.RefreshForCamera();
        }
    }
    void Awake()
    {
        label = GetComponent<TextMeshPro>(); textRenderer = GetComponent<MeshRenderer>();
        anchor = transform.localPosition;
        label.text = label.text.Replace(" / ", "\n").Replace("MIDNIGHT LOUNGE", "MIDNIGHT\nLOUNGE");
        label.fontSize = 6; label.fontStyle = FontStyles.Bold;
        label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Overflow;
        label.rectTransform.sizeDelta = new Vector2(16, 4);
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        fontMaterial = new Material(label.fontSharedMaterial) { name = "Mission3 room sign font (instance)" };
        fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(.015f,.025f,.03f,1));
        fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, .14f);
        label.fontSharedMaterial = fontMaterial; label.extraPadding = true; label.ForceMeshUpdate();
        glyphBounds = label.textBounds; LineCount = Mathf.Max(1,label.textInfo.lineCount);
        var go = new GameObject("Mission3_RoomSignBackdrop", typeof(MeshFilter), typeof(MeshRenderer));
        plate = go.transform; plate.SetParent(transform,false);
        plateMesh = new Mesh { name = "Mission3 room sign backdrop" };
        plateMesh.vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(-.5f,.5f,0), new Vector3(.5f,.5f,0), new Vector3(.5f,-.5f,0) };
        plateMesh.triangles = new[] { 0,1,2,0,2,3 }; plateMesh.RecalculateNormals(); plateMesh.RecalculateBounds();
        go.GetComponent<MeshFilter>().sharedMesh = plateMesh;
        plateRenderer = go.GetComponent<MeshRenderer>();
        plateRenderer.sharedMaterial = Resources.Load<Material>("Mission3/RoomSignBackdrop");
        plateRenderer.shadowCastingMode = ShadowCastingMode.Off; plateRenderer.receiveShadows = false;
        plate.localPosition = glyphBounds.center + new Vector3(0,0,.035f);
        plate.localScale = new Vector3(glyphBounds.size.x+.38f,glyphBounds.size.y+.24f,1);
    }
    void LateUpdate() { RefreshForCamera(); }
    public void RefreshForCamera()
    {
        if (Viewer == null || label == null) return;
        var worldAnchor = transform.parent == null ? anchor : transform.parent.TransformPoint(anchor);
        var direction = worldAnchor - Viewer.transform.position; float distance = direction.magnitude;
        transform.position = worldAnchor - direction.normalized * .08f;
        if (distance > .001f) transform.rotation = Quaternion.LookRotation(direction,Viewer.transform.up);
        float depth = Mathf.Max(.01f,Vector3.Dot(transform.position-Viewer.transform.position,Viewer.transform.forward));
        float unitsPerPixel = Viewer.orthographic ? 2*Viewer.orthographicSize/Mathf.Max(1,Viewer.pixelHeight)
            : 2*depth*Mathf.Tan(Viewer.fieldOfView*Mathf.Deg2Rad*.5f)/Mathf.Max(1,Viewer.pixelHeight);
        float height = linePixelsAt900*LineCount*Mathf.Max(1,Viewer.pixelHeight)/900f*unitsPerPixel;
        float scale = Mathf.Min(height/Mathf.Max(.01f,glyphBounds.size.y),maximumWorldWidth/Mathf.Max(.01f,glyphBounds.size.x+.38f));
        transform.localScale = Vector3.one * Mathf.Max(.001f,scale);
        bool visible = distance <= visibleDistance && distance > Viewer.nearClipPlane+.15f && depth > Viewer.nearClipPlane;
        textRenderer.enabled = visible; plateRenderer.enabled = visible && plateRenderer.sharedMaterial != null;
    }
    void OnDestroy()
    {
        if (fontMaterial != null) Destroy(fontMaterial);
        if (plateMesh != null) Destroy(plateMesh);
    }
}
