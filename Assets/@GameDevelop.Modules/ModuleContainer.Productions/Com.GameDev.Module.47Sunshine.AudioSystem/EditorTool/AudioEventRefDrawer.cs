#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AudioEventRef))]
public class AudioEventRefDrawer : PropertyDrawer
{
    private static AudioDatabase cachedDatabase;
    private static string[] displayOptions;
    private static string[] guidOptions;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnsureCache();

        SerializedProperty guidProp = property.FindPropertyRelative("guid");

        if (cachedDatabase == null || displayOptions == null || displayOptions.Length == 0)
        {
            EditorGUI.PropertyField(position, guidProp, label);
            return;
        }

        int currentIndex = 0;

        for (int i = 0; i < guidOptions.Length; i++)
        {
            if (guidOptions[i] == guidProp.stringValue)
            {
                currentIndex = i;
                break;
            }
        }

        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayOptions);

        if (newIndex >= 0 && newIndex < guidOptions.Length)
            guidProp.stringValue = guidOptions[newIndex];
    }

    private static void EnsureCache()
    {
        if (cachedDatabase != null && displayOptions != null)
            return;

        string[] guids = AssetDatabase.FindAssets("t:AudioDatabase");

        if (guids.Length == 0)
            return;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        cachedDatabase = AssetDatabase.LoadAssetAtPath<AudioDatabase>(path);

        if (cachedDatabase == null)
            return;

        List<string> displays = new();
        List<string> keys = new();

        displays.Add("None");
        keys.Add("");

        foreach (AudioLibraryData library in cachedDatabase.EditorLibraries)
        {
            foreach (AudioEntryData entry in library.entries)
            {
                displays.Add($"{library.libraryName}/{entry.id}");
                keys.Add(entry.guid);
            }
        }

        displayOptions = displays.ToArray();
        guidOptions = keys.ToArray();
    }
}
#endif