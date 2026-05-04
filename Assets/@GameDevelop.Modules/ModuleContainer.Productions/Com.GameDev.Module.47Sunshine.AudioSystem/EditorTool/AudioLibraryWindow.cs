#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public class AudioLibraryWindow : EditorWindow
{
    private AudioDatabase database;
    private Vector2 scroll;

    [MenuItem("Tools/47Sunshine/Audio Library")]
    public static void Open()
    {
        GetWindow<AudioLibraryWindow>("47Sunshine Audio");
    }

    private void OnGUI()
    {
        DrawDatabaseField();

        if (database == null)
            return;

        DrawToolbar();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (AudioLibraryData library in database.EditorLibraries)
        {
            DrawLibrary(library);
        }

        EditorGUILayout.EndScrollView();

        DrawValidation();
    }

    private void DrawDatabaseField()
    {
        database = (AudioDatabase)EditorGUILayout.ObjectField(
            "Audio Database",
            database,
            typeof(AudioDatabase),
            false);
    }

    private void DrawToolbar()
    {
        EditorGUILayout.Space();

        if (GUILayout.Button("Add Library"))
        {
            database.EditorLibraries.Add(new AudioLibraryData
            {
                libraryName = "New Library"
            });

            Save();
        }
    }

    private void DrawLibrary(AudioLibraryData library)
    {
        EditorGUILayout.BeginVertical("box");

        library.libraryName = EditorGUILayout.TextField("Library", library.libraryName);

        DrawDropArea(library);

        for (int i = 0; i < library.entries.Count; i++)
        {
            DrawEntry(library, library.entries[i], i);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawDropArea(AudioLibraryData library)
    {
        Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag AudioClips Here");

        Event evt = Event.current;

        if (!dropArea.Contains(evt.mousePosition))
            return;

        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
            return;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

        if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();

            foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
            {
                if (obj is AudioClip clip)
                    AddClipAsEntry(library, clip);
            }

            Save();
        }

        evt.Use();
    }

    private void AddClipAsEntry(AudioLibraryData library, AudioClip clip)
    {
        string id = ObjectNames.NicifyVariableName(clip.name)
            .Replace(" ", "_")
            .ToLowerInvariant();

        library.entries.Add(new AudioEntryData
        {
            guid = Guid.NewGuid().ToString("N"),
            id = id,
            displayName = ObjectNames.NicifyVariableName(clip.name),
            type = AudioType.SFX,
            clips = new[] { clip }
        });
    }

    private void DrawEntry(AudioLibraryData library, AudioEntryData entry, int index)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(entry.displayName, EditorStyles.boldLabel);

        entry.id = EditorGUILayout.TextField("ID", entry.id);
        entry.displayName = EditorGUILayout.TextField("Display Name", entry.displayName);
        entry.type = (AudioType)EditorGUILayout.EnumPopup("Type", entry.type);

        SerializedObject dummy = null;

        entry.volume = EditorGUILayout.Slider("Volume", entry.volume, 0f, 1f);
        entry.pitch = EditorGUILayout.Slider("Pitch", entry.pitch, 0.1f, 3f);
        entry.loop = EditorGUILayout.Toggle("Loop", entry.loop);
        entry.cooldown = EditorGUILayout.FloatField("Cooldown", entry.cooldown);

        if (GUILayout.Button("Remove"))
        {
            library.entries.RemoveAt(index);
            Save();
            return;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawValidation()
    {
        EditorGUILayout.Space();

        if (GUILayout.Button("Validate"))
        {
            var errors = AudioDatabaseValidator.Validate(database);

            if (errors.Count == 0)
            {
                Debug.Log("[AudioDatabase] Validation passed.");
            }
            else
            {
                foreach (string error in errors)
                    Debug.LogWarning(error);
            }
        }
    }

    private void Save()
    {
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }
}
#endif