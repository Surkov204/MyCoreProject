#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AudioEventRef))]
public class AudioEventRefDrawer : PropertyDrawer
{
    private static AudioDatabase cachedDatabase;
    private static List<Option> cachedOptions;

    private struct Option
    {
        public readonly string Label;
        public readonly string Guid;

        public Option(string label, string guid)
        {
            Label = label;
            Guid = guid;
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty guidProperty = property.FindPropertyRelative("guid");

        if (guidProperty == null)
        {
            EditorGUI.LabelField(position, label.text, "Invalid AudioEventRef");
            return;
        }

        EnsureCache();

        if (cachedOptions == null || cachedOptions.Count == 0)
        {
            EditorGUI.PropertyField(position, guidProperty, label);
            return;
        }

        string currentGuid = guidProperty.stringValue;
        string[] labels = BuildLabels(currentGuid, out int currentIndex);

        EditorGUI.BeginProperty(position, label, property);

        int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, labels);

        if (selectedIndex >= 0 && selectedIndex < cachedOptions.Count)
        {
            guidProperty.stringValue = cachedOptions[selectedIndex].Guid;
        }

        EditorGUI.EndProperty();
    }

    private static void EnsureCache()
    {
        if (cachedOptions != null)
            return;

        cachedDatabase = FindDatabase();

        cachedOptions = new List<Option>
        {
            new Option("None", string.Empty)
        };

        if (cachedDatabase == null)
            return;

        IReadOnlyList<AudioLibraryData> libraries = cachedDatabase.EditorLibraries;

        if (libraries == null)
            return;

        foreach (AudioLibraryData library in libraries)
        {
            if (library == null || library.entries == null)
                continue;

            foreach (AudioEntryData entry in library.entries)
            {
                if (entry == null)
                    continue;

                if (string.IsNullOrWhiteSpace(entry.guid))
                    continue;

                string entryName = ResolveEntryName(entry);
                string libraryName = string.IsNullOrWhiteSpace(library.libraryName)
                    ? "(Unnamed Library)"
                    : library.libraryName;

                string optionLabel = $"{library.defaultType}/{libraryName}/{entryName}";

                cachedOptions.Add(new Option(optionLabel, entry.guid));
            }
        }
    }

    private static string ResolveEntryName(AudioEntryData entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.displayName))
            return entry.displayName;

        if (!string.IsNullOrWhiteSpace(entry.id))
            return entry.id;

        return "(Unnamed Audio)";
    }

    private static string[] BuildLabels(string currentGuid, out int currentIndex)
    {
        currentIndex = 0;

        bool found = string.IsNullOrEmpty(currentGuid);

        for (int i = 0; i < cachedOptions.Count; i++)
        {
            if (cachedOptions[i].Guid == currentGuid)
            {
                currentIndex = i;
                found = true;
                break;
            }
        }

        if (!found)
        {
            cachedOptions.Insert(
                1,
                new Option($"Missing Reference ({currentGuid})", currentGuid));

            currentIndex = 1;
        }

        string[] labels = new string[cachedOptions.Count];

        for (int i = 0; i < cachedOptions.Count; i++)
            labels[i] = cachedOptions[i].Label;

        return labels;
    }

    private static AudioDatabase FindDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioDatabase");

        if (guids == null || guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<AudioDatabase>(path);
    }

    public static void ClearCache()
    {
        cachedDatabase = null;
        cachedOptions = null;
    }
}
#endif