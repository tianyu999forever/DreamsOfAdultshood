using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runtime-only labels: keep the scene, colliders and shared font assets unchanged.
public static class mission3GuestNote_World_ShengChiehLee
{
    const string BackName = "Mission3_GuestNote_Back_ShengChiehLee";
    const string FrontName = "Mission3_GuestNote_Front_ShengChiehLee";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() { SceneManager.sceneLoaded -= Install; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register() { SceneManager.sceneLoaded -= Install; SceneManager.sceneLoaded += Install; }

    static void Install(Scene scene, LoadSceneMode mode)
    {
        if (scene.path != "Assets/Scenes/Mission3.unity" &&
            scene.path != "Assets/Mission3_ShengChiehLee/Scenes/Mission3_Preview.unity") return;
        foreach (var note in scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<mission3Interact_Prop_ShengChiehLee>(true))
            .Where(prop => prop.kind == mission3Interact_Prop_ShengChiehLee.Kind.GuestNote))
            RefreshNote(note);
    }

    public static void RefreshNote(mission3Interact_Prop_ShengChiehLee note)
    {
        if (note == null || note.kind != mission3Interact_Prop_ShengChiehLee.Kind.GuestNote) return;
        var labels = note.GetComponentsInChildren<TextMeshPro>(true)
            .Where(text => text.name.StartsWith("Guest note text") || text.name == BackName || text.name == FrontName).ToArray();
        // The original front faces the note's negative local Z side.
        var source = labels.Where(text => text.name.StartsWith("Guest note text"))
            .OrderByDescending(text => Vector3.Dot(text.transform.forward, note.transform.forward)).FirstOrDefault();
        var card = note.GetComponent<BoxCollider>();
        if (source == null || card == null) return;

        var front = Face(labels, source, note.transform, FrontName);
        var back = Face(labels, source, note.transform, BackName);
        // Retire older hand-placed labels so coplanar text cannot flicker.
        foreach (var label in labels)
            if (label != front && label != back) label.gameObject.SetActive(false);

        var axes = new Vector3(note.transform.TransformVector(Vector3.right).magnitude,
            note.transform.TransformVector(Vector3.up).magnitude, note.transform.TransformVector(Vector3.forward).magnitude);
        if (Mathf.Min(axes.x, axes.y, axes.z) < .0001f) return;
        foreach (var face in new[] { front, back })
        {
            face.text = source.text;
            face.color = new Color(.12f, .09f, .06f, 1);
            face.raycastTarget = false; face.enabled = true; face.gameObject.SetActive(true);
            face.GetComponent<MeshRenderer>().enabled = true;
            face.transform.localRotation = face == front ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
            face.transform.localScale = new Vector3(1 / axes.x, 1 / axes.y, 1 / axes.z);
            face.ForceMeshUpdate(true);
            var glyphs = face.textBounds;
            float fit = Mathf.Min(card.size.x * axes.x * .86f / Mathf.Max(.001f, glyphs.size.x),
                card.size.y * axes.y * .78f / Mathf.Max(.001f, glyphs.size.y));
            face.transform.localScale *= fit;
            float side = face == front ? -1 : 1;
            var surface = card.center + Vector3.forward * side * (card.size.z * .5f + .008f / axes.z);
            face.transform.localPosition = surface - face.transform.localRotation * Vector3.Scale(glyphs.center, face.transform.localScale);
        }
    }

    static TextMeshPro Face(TextMeshPro[] labels, TextMeshPro source, Transform parent, string name)
    {
        var face = labels.FirstOrDefault(text => text.name == name);
        if (face != null) return face;
        face = Object.Instantiate(source, parent, false); face.name = name;
        return face;
    }
}
