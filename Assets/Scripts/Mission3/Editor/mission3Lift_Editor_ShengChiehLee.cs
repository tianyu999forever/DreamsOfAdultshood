using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class mission3Lift_Editor_ShengChiehLee
{
    const string ScenePath = "Assets/Scenes/Mission3.unity";
    const string Marker = "Library/Mission3LiftApplied-v3.txt";
    const string Preview = "C:/Users/j1a2m/Documents/Codex/2026-10-15/ban/outputs/mission3-lift.png";
    static Material brass, linen, walnut, ink;

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        if (!File.Exists(Marker)) EditorApplication.delayCall += Upgrade;
    }

    static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name; box.transform.SetParent(parent, false);
        box.transform.localPosition = position; box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    static void Label(Transform parent, string name, string text, Vector3 position, float size, float width = 2)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        var label = go.AddComponent<TextMeshPro>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        label.text = text; label.fontSize = size; label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1, .9f, .65f); label.rectTransform.sizeDelta = new Vector2(width, .4f);
    }

    static void Action(GameObject go, mission3Interact_Prop_ShengChiehLee.Kind kind, string prompt)
    {
        var action = go.AddComponent<mission3Interact_Prop_ShengChiehLee>();
        action.kind = kind; action.displayName = prompt;
    }

    public static GameObject Create(Transform parent)
    {
        brass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Mission3/mission3_Brass.mat");
        linen = AssetDatabase.LoadAssetAtPath<Material>("Assets/Mission3/mission3_Linen.mat");
        walnut = AssetDatabase.LoadAssetAtPath<Material>("Assets/Mission3/mission3_Walnut.mat");
        ink = AssetDatabase.LoadAssetAtPath<Material>("Assets/Mission3/mission3_Ink.mat");
        if (brass == null || linen == null || walnut == null || ink == null) throw new Exception("Missing Mission3 lift materials.");
        var root = new GameObject("Exit lift"); root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(15, 0, -3.7f);
        var t = root.transform;
        // Cabin stays inside the existing lobby, in front of its north wall.
        Box(t, "Cabin floor", new Vector3(0, .025f, .95f), new Vector3(2.6f, .05f, 1.9f), walnut);
        Box(t, "Cabin back wall", new Vector3(0, 1.5f, 1.85f), new Vector3(2.6f, 3, .12f), linen);
        Box(t, "Cabin left wall", new Vector3(-1.25f, 1.5f, .95f), new Vector3(.12f, 3, 1.9f), walnut);
        Box(t, "Cabin right wall", new Vector3(1.25f, 1.5f, .95f), new Vector3(.12f, 3, 1.9f), walnut);
        Box(t, "Cabin ceiling", new Vector3(0, 3.05f, .95f), new Vector3(2.6f, .1f, 1.9f), linen);
        Box(t, "Back handrail", new Vector3(0, .95f, 1.73f), new Vector3(2.15f, .045f, .06f), brass);
        Box(t, "Left door frame", new Vector3(-1.12f, 1.5f, -.1f), new Vector3(.56f, 3, .28f), brass);
        Box(t, "Right door frame", new Vector3(1.12f, 1.5f, -.1f), new Vector3(.56f, 3, .28f), brass);
        Box(t, "Top door frame", new Vector3(0, 3.05f, -.1f), new Vector3(2.8f, .18f, .28f), brass);
        Box(t, "Threshold", new Vector3(0, .035f, -.08f), new Vector3(1.7f, .04f, .4f), brass);
        var lift = root.AddComponent<mission3Lift_Prop_ShengChiehLee>();
        lift.leftDoor = Box(t, "Left sliding door", new Vector3(-.4f, 1.46f, .12f), new Vector3(.8f, 2.88f, .08f), linen).transform;
        lift.rightDoor = Box(t, "Right sliding door", new Vector3(.4f, 1.46f, .12f), new Vector3(.8f, 2.88f, .08f), linen).transform;
        // Thin dark meeting edges make the two sliding leaves readable when closed.
        var leftEdge = Box(lift.leftDoor, "Left door meeting edge", new Vector3(.492f, 0, -.55f), new Vector3(.016f, 1, .12f), ink);
        var rightEdge = Box(lift.rightDoor, "Right door meeting edge", new Vector3(-.492f, 0, -.55f), new Vector3(.016f, 1, .12f), ink);
        UnityEngine.Object.DestroyImmediate(leftEdge.GetComponent<Collider>());
        UnityEngine.Object.DestroyImmediate(rightEdge.GetComponent<Collider>());
        Action(lift.leftDoor.gameObject, mission3Interact_Prop_ShengChiehLee.Kind.LiftDoors, "Open lift doors");
        Action(lift.rightDoor.gameObject, mission3Interact_Prop_ShengChiehLee.Kind.LiftDoors, "Open lift doors");
        var call = Box(t, "Lift call button", new Vector3(1.12f, 1.25f, -.27f), new Vector3(.24f, .3f, .08f), ink);
        Action(call, mission3Interact_Prop_ShengChiehLee.Kind.LiftDoors, "Call lift / open doors");
        Label(t, "Call arrow", "^", new Vector3(1.12f, 1.26f, -.32f), 1.8f, .24f);
        Box(t, "Floor indicator", new Vector3(0, 3.34f, -.1f), new Vector3(1.5f, .38f, .12f), ink);
        Label(t, "Lift floor number", "Elevator / EXIT", new Vector3(0, 3.34f, -.17f), 1.6f);
        var checkout = Box(t, "Lift checkout panel", new Vector3(0, 1.25f, 1.73f), new Vector3(.85f, .6f, .08f), ink);
        Action(checkout, mission3Interact_Prop_ShengChiehLee.Kind.Checkout, "Checkout / return belongings");
        Label(t, "Lift checkout text", "CHECKOUT\nLOST & FOUND", new Vector3(0, 1.25f, 1.67f), 1.35f, .8f);
        var lightGo = new GameObject("Cabin light"); lightGo.transform.SetParent(t, false);
        lightGo.transform.localPosition = new Vector3(0, 2.7f, .9f);
        var light = lightGo.AddComponent<Light>(); light.type = LightType.Point;
        light.color = new Color(1, .84f, .62f); light.intensity = 1.7f; light.range = 3.5f;
        Validate(root);
        return root;
    }

    static void Validate(GameObject root)
    {
        var lift = root.GetComponent<mission3Lift_Prop_ShengChiehLee>();
        if (lift == null || lift.leftDoor == null || lift.rightDoor == null) throw new Exception("Lift doors are missing.");
        int checkout = 0, buttons = 0;
        foreach (var action in root.GetComponentsInChildren<mission3Interact_Prop_ShengChiehLee>())
        {
            if (action.GetComponent<Collider>() == null) throw new Exception("Lift interaction needs a collider.");
            if (action.kind == mission3Interact_Prop_ShengChiehLee.Kind.Checkout) checkout++;
            if (action.kind == mission3Interact_Prop_ShengChiehLee.Kind.LiftDoors) buttons++;
        }
        if (checkout != 1 || buttons != 3) throw new Exception("Unexpected lift interactions.");
        float gap = 2 * lift.travel;
        if (gap < .9f) throw new Exception("Lift doorway is too narrow for the player.");
    }

    [MenuItem("Mission3/Upgrade Exit Lift")]
    public static void Upgrade()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += Upgrade; return; }
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            Transform parent = null; GameObject oldLift = null, oldLabel = null;
            foreach (var sceneRoot in scene.GetRootGameObjects())
                foreach (var child in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "Mission3_PropsAndInteractions") parent = child;
                    if (child.name == "Exit lift") oldLift = child.gameObject;
                    if (child.name == "Lift label") oldLabel = child.gameObject;
                }
            if (parent == null) throw new Exception("Mission3 props root was not found.");
            string existingTitle = null;
            if (oldLift != null)
            {
                var oldTitle = oldLift.transform.Find("Lift floor number");
                if (oldTitle != null) existingTitle = oldTitle.GetComponent<TextMeshPro>()?.text;
                Undo.DestroyObjectImmediate(oldLift);
            }
            if (oldLabel != null) Undo.DestroyObjectImmediate(oldLabel);
            var root = Create(parent); Undo.RegisterCreatedObjectUndo(root, "Upgrade Exit Lift");
            if (existingTitle == "3  /  EXIT") existingTitle = "Elevator / EXIT";
            if (!string.IsNullOrEmpty(existingTitle)) root.transform.Find("Lift floor number").GetComponent<TextMeshPro>().text = existingTitle;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save Mission3 lift.");
            Capture(root);
            File.WriteAllText(Marker, "PASS: cabin, two sliding doors, call button, checkout panel, player clearance.\nScene saved: " + ScenePath + "\nPreview: " + Preview);
            Debug.Log("MISSION3_LIFT_UPGRADE_PASS " + ScenePath);
        }
        catch (Exception exception) { Debug.LogException(exception); }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    static void Capture(GameObject root)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Preview));
        var go = new GameObject("Lift preview camera"); SceneManager.MoveGameObjectToScene(go, root.scene);
        var cam = go.AddComponent<Camera>();
        go.transform.position = new Vector3(12.9f, 2.1f, -7.8f);
        go.transform.LookAt(new Vector3(15, 1.65f, -3.35f));
        cam.fieldOfView = 54; cam.nearClipPlane = .05f; cam.farClipPlane = 40;
        var texture = new RenderTexture(1200, 1000, 24); texture.Create();
        var previous = RenderTexture.active;
        Texture2D image = null;
        try
        {
            cam.targetTexture = texture; cam.Render(); RenderTexture.active = texture;
            image = new Texture2D(1200, 1000, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1200, 1000), 0, 0); image.Apply();
            File.WriteAllBytes(Preview, image.EncodeToPNG());
        }
        finally
        {
            cam.targetTexture = null; RenderTexture.active = previous;
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            texture.Release(); UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
