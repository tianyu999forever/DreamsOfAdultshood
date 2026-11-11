using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Runs once for this checkout; the menu can be used again after changing Game view settings.
public static class mission3Clarity_Editor_ShengChiehLee
{
    const string Marker = "Library/Mission3ClarityApplied.txt";

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        if (!File.Exists(Marker)) EditorApplication.delayCall += Apply;
    }

    [MenuItem("Mission3/Fix Game View Clarity")]
    public static void Apply()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Apply;
            return;
        }
        try
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var property = type?.GetProperty("lowResolutionForAspectRatios", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
            {
                Debug.LogWarning("Mission3: Game view clarity setting was not found in this Unity version.");
                return;
            }
            var views = Resources.FindObjectsOfTypeAll(type);
            if (views.Length == 0) return;
            foreach (var view in views)
            {
                property.SetValue(view, false);
                ((EditorWindow)view).Repaint();
                if ((bool)property.GetValue(view)) throw new InvalidOperationException("Low resolution preview is still enabled.");
            }
            File.WriteAllText(Marker, "Low Resolution Aspect Ratios: disabled\nGame views updated: " + views.Length + "\n" + DateTime.Now.ToString("s"));
            Debug.Log("Mission3: Low Resolution Aspect Ratios disabled. PC render scale is configured at 100%.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
