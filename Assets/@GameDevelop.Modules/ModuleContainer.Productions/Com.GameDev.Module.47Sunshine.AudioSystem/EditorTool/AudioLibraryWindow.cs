#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public class AudioLibraryWindow : EditorWindow
{
    #region Constants

    private const string DatabasePathPrefsKey = "47Sunshine.AudioSystem.DatabasePath";

    private const float HeaderPanelHeight = 220f;
    private const float MinWindowWidth = 1100f;
    private const float MinWindowHeight = 720f;

    #endregion

    #region Fields

    private AudioDatabase database;

    private Vector2 libraryListScroll;
    private Vector2 detailScroll;

    private int selectedLibraryIndex = -1;
    private string searchText = string.Empty;

    private readonly Dictionary<string, bool> entryFoldouts = new();
    private readonly Dictionary<string, bool> identityFoldouts = new();
    private readonly Dictionary<string, bool> playbackFoldouts = new();
    private readonly Dictionary<string, bool> variationFoldouts = new();
    private readonly Dictionary<string, bool> spatialFoldouts = new();
    private readonly Dictionary<string, bool> clipsFoldouts = new();

    #endregion

    #region Unity Menu / Lifecycle

    [MenuItem("Tools/47Sunshine/Audio Library")]
    public static void Open()
    {
        GetWindow<AudioLibraryWindow>("47Sunshine Audio");
    }

    private void OnEnable()
    {
        LoadSavedDatabase();
        TryAutoFindDatabase();

        minSize = new Vector2(MinWindowWidth, MinWindowHeight);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

        DrawDatabaseField();

        if (database == null)
        {
            EditorGUILayout.HelpBox("Assign an AudioDatabase asset first.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        DrawPlayModeWarning();

        GUILayout.Space(8);

        EditorGUILayout.BeginHorizontal(GUILayout.Height(HeaderPanelHeight));
        DrawWelcomePanel();
        DrawLibraryListPanel();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);

        DrawSelectedLibraryDetails();

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Main Layout

    private void DrawWelcomePanel()
    {
        float leftWidth = Mathf.Clamp(position.width * 0.38f, 260f, 380f);

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox,
            GUILayout.Width(leftWidth),
            GUILayout.MinHeight(HeaderPanelHeight));

        GUILayout.Space(16);

        GUILayout.Label("Welcome to", EditorStyles.boldLabel);
        GUILayout.Label("47Sunshine Audio System", EditorStyles.largeLabel);

        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "Organize libraries, register clips, preview sounds, and check audio data from one editor window.",
            EditorStyles.wordWrappedLabel);

        GUILayout.Space(20);

        using (new EditorGUI.DisabledScope(IsEditingDisabled()))
        {
            if (DrawColoredButton("Add Library", new Color(0.22f, 0.68f, 0.32f), GUILayout.Height(40)))
            {
                AddLibrary();
            }
        }

        GUILayout.Space(8);

        if (DrawColoredButton("Check Database", new Color(0.85f, 0.62f, 0.18f), GUILayout.Height(26)))
        {
            ValidateDatabase();
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField(
            $"Libraries: {GetLibraryCount()}",
            EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    private void DrawLibraryListPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(HeaderPanelHeight));

        GUILayout.Label("Libraries", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Search", GUILayout.Width(48));
        searchText = EditorGUILayout.TextField(searchText);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);

        libraryListScroll = EditorGUILayout.BeginScrollView(libraryListScroll, GUILayout.MinHeight(180));

        for (int i = 0; i < GetLibraryCount(); i++)
        {
            AudioLibraryData library = database.EditorLibraries[i];

            if (!PassesLibrarySearch(library))
                continue;

            DrawLibraryListItem(library, i);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawLibraryListItem(AudioLibraryData library, int index)
    {
        bool isSelected = selectedLibraryIndex == index;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical();

        GUILayout.Label(
            string.IsNullOrWhiteSpace(library.libraryName) ? "(Unnamed Library)" : library.libraryName,
            EditorStyles.boldLabel);

        GUILayout.Label($"{GetEntryCount(library)} entries", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();

        GUILayout.Space(8);

        DrawTypeBadge(library.defaultType);

        GUILayout.FlexibleSpace();

        Color selectColor = isSelected
            ? GetTypeColor(library.defaultType)
            : new Color(0.35f, 0.35f, 0.35f);

        if (DrawColoredButton(
                isSelected ? "Selected" : "Select",
                selectColor,
                GUILayout.Width(80),
                GUILayout.Height(26)))
        {
            selectedLibraryIndex = index;
            GUI.FocusControl(null);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        GUILayout.Space(4);
    }

    private void DrawSelectedLibraryDetails()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

        if (!TryGetSelectedLibrary(out AudioLibraryData library))
        {
            EditorGUILayout.LabelField("Select a library to edit its details.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        DrawSelectedLibraryHeader(library);

        GUILayout.Space(6);

        detailScroll = EditorGUILayout.BeginScrollView(detailScroll, GUILayout.ExpandHeight(true));
        DrawSelectedLibraryScrollableContent(library, selectedLibraryIndex);
        EditorGUILayout.EndScrollView();

        GUILayout.Space(6);

        DrawSelectedLibraryFooter(library, selectedLibraryIndex);

        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedLibraryHeader(AudioLibraryData library)
    {
        GUILayout.Label($"Library Details — {library.libraryName}", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(IsEditingDisabled()))
        {
            EditorGUI.BeginChangeCheck();

            library.libraryName = EditorGUILayout.TextField("Library Name", library.libraryName);
            library.defaultType = (AudioType)EditorGUILayout.EnumPopup("Library Type", library.defaultType);

            if (EditorGUI.EndChangeCheck())
            {
                SyncEntryTypes(library);
                Save();
            }
        }
    }

    private void DrawSelectedLibraryScrollableContent(AudioLibraryData library, int libraryIndex)
    {
        DrawDropArea(library);

        GUILayout.Space(10);

        DrawEntryTable(library, libraryIndex);
    }

    private void DrawSelectedLibraryFooter(AudioLibraryData library, int libraryIndex)
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        using (new EditorGUI.DisabledScope(IsEditingDisabled()))
        {
            if (DrawColoredButton(
                    "Delete Selected Library",
                    new Color(0.78f, 0.22f, 0.22f),
                    GUILayout.Width(190),
                    GUILayout.Height(28)))
            {
                DeleteLibrary(library, libraryIndex);
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Database Field / Database Loading

    private void DrawDatabaseField()
    {
        EditorGUI.BeginChangeCheck();

        database = (AudioDatabase)EditorGUILayout.ObjectField(
            "Audio Database",
            database,
            typeof(AudioDatabase),
            false);

        if (!EditorGUI.EndChangeCheck())
            return;

        if (database != null)
        {
            string path = AssetDatabase.GetAssetPath(database);
            EditorPrefs.SetString(DatabasePathPrefsKey, path);
        }
        else
        {
            EditorPrefs.DeleteKey(DatabasePathPrefsKey);
        }

        AudioEventRefDrawer.ClearCache();
    }

    private void LoadSavedDatabase()
    {
        string path = EditorPrefs.GetString(DatabasePathPrefsKey, string.Empty);

        if (string.IsNullOrEmpty(path))
            return;

        database = AssetDatabase.LoadAssetAtPath<AudioDatabase>(path);
    }

    private void TryAutoFindDatabase()
    {
        if (database != null)
            return;

        string[] guids = AssetDatabase.FindAssets("t:AudioDatabase");

        if (guids == null || guids.Length == 0)
            return;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        database = AssetDatabase.LoadAssetAtPath<AudioDatabase>(path);

        if (database != null)
            EditorPrefs.SetString(DatabasePathPrefsKey, path);
    }

    #endregion

    #region Drop Area / Add Clips

    private void DrawDropArea(AudioLibraryData library)
    {
        Rect dropArea = GUILayoutUtility.GetRect(0, 58, GUILayout.ExpandWidth(true));

        DrawDropAreaBackground(dropArea, library);
        DrawDropAreaContent(dropArea, library);

        if (!IsEditingDisabled())
        {
            HandleAudioClipDragAndDrop(dropArea, library);
        }
    }

    private void DrawDropAreaBackground(Rect dropArea, AudioLibraryData library)
    {
        Color baseColor = GetTypeColor(library.defaultType);
        Color background = new Color(
            baseColor.r * 0.35f,
            baseColor.g * 0.35f,
            baseColor.b * 0.35f,
            0.95f);

        EditorGUI.DrawRect(dropArea, background);
        GUI.Box(dropArea, GUIContent.none);
    }

    private void DrawDropAreaContent(Rect dropArea, AudioLibraryData library)
    {
        Rect labelRect = new Rect(
            dropArea.x + 12,
            dropArea.y + 8,
            dropArea.width - 170,
            20);

        Rect subLabelRect = new Rect(
            dropArea.x + 12,
            dropArea.y + 30,
            dropArea.width - 170,
            18);

        Rect buttonRect = new Rect(
            dropArea.xMax - 150,
            dropArea.y + 15,
            135,
            28);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft
        };
        titleStyle.normal.textColor = Color.white;

        GUIStyle subStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft
        };
        subStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

        GUI.Label(labelRect, "Drop AudioClips Here", titleStyle);
        GUI.Label(subLabelRect, $"Default Type: {library.defaultType}", subStyle);

        using (new EditorGUI.DisabledScope(IsEditingDisabled()))
        {
            Color oldBackground = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.22f, 0.45f, 0.75f);

            if (GUI.Button(buttonRect, "Add From Project"))
            {
                OpenAudioClipPicker(library);
            }

            GUI.backgroundColor = oldBackground;
        }
    }

    private void HandleAudioClipDragAndDrop(Rect dropArea, AudioLibraryData library)
    {
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

            SyncEntryTypes(library);
            Save();
        }

        evt.Use();
    }

    private void OpenAudioClipPicker(AudioLibraryData library)
    {
        AudioClipPickerWindow.Open(
            $"Add AudioClips to {library.libraryName}",
            selectedClips =>
            {
                foreach (AudioClip clip in selectedClips)
                    AddClipAsEntry(library, clip);

                SyncEntryTypes(library);
                Save();
                Repaint();
            });
    }

    private void AddClipAsEntry(AudioLibraryData library, AudioClip clip)
    {
        if (library == null || clip == null)
            return;

        if (library.entries == null)
            library.entries = new List<AudioEntryData>();

        string stableGuid = CreateStableEntryGuid(library.defaultType, clip);

        if (EntryGuidExists(stableGuid))
        {
            Debug.LogWarning($"[AudioLibrary] Clip '{clip.name}' already exists for type '{library.defaultType}'.");
            return;
        }

        if (ClipExistsInLibrary(library, clip))
        {
            Debug.LogWarning($"[AudioLibrary] Clip '{clip.name}' already exists in library '{library.libraryName}'.");
            return;
        }

        string id = CreateEntryId(clip);

        library.entries.Add(new AudioEntryData
        {
            guid = stableGuid,
            id = id,
            displayName = ObjectNames.NicifyVariableName(clip.name),
            type = library.defaultType,
            clips = new[] { clip },
            volume = 1f,
            pitch = 1f,
            pitchRange = new Vector2(0.95f, 1.05f),
            spatialBlend = 1f,
            minDistance = 1f,
            maxDistance = 15f,
            rolloffMode = AudioRolloffMode.Logarithmic
        });
    }

    private string CreateEntryId(AudioClip clip)
    {
        return ObjectNames.NicifyVariableName(clip.name)
            .Replace(" ", "_")
            .Replace("-", "_")
            .ToLowerInvariant();
    }

    private string CreateStableEntryGuid(AudioType type, AudioClip clip)
    {
        string assetPath = AssetDatabase.GetAssetPath(clip);
        string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

        if (string.IsNullOrWhiteSpace(assetGuid))
            assetGuid = Guid.NewGuid().ToString("N");

        return $"{type}_{assetGuid}";
    }

    #endregion

    #region Entry Table

    private void DrawEntryTable(AudioLibraryData library, int libraryIndex)
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("Audio Entries", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Show All Details", GUILayout.Width(130)))
        {
            SetAllEntryFoldouts(library, true);
        }

        if (GUILayout.Button("Hide All Details", GUILayout.Width(130)))
        {
            SetAllEntryFoldouts(library, false);
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);

        if (library.entries == null || library.entries.Count == 0)
        {
            EditorGUILayout.HelpBox("No audio entries in this library.", MessageType.None);
            return;
        }

        for (int entryIndex = 0; entryIndex < library.entries.Count; entryIndex++)
        {
            DrawEntry(library, libraryIndex, library.entries[entryIndex], entryIndex);
        }
    }

    private void SetAllEntryFoldouts(AudioLibraryData library, bool expanded)
    {
        if (library == null || library.entries == null)
            return;

        foreach (AudioEntryData entry in library.entries)
        {
            if (entry == null)
                continue;

            string key = GetEntryKey(entry);
            entryFoldouts[key] = expanded;
        }
    }

    private void DrawEntry(
        AudioLibraryData library,
        int libraryIndex,
        AudioEntryData entry,
        int entryIndex)
    {
        if (entry == null)
            return;

        EnsureEntryFoldoutState(entry);

        string key = GetEntryKey(entry);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        DrawEntryHeader(library, entry, entryIndex);

        if (entryFoldouts[key])
        {
            GUILayout.Space(6);

            using (new EditorGUI.DisabledScope(IsEditingDisabled()))
            {
                EditorGUI.BeginChangeCheck();

                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 110f;

                DrawIdentitySection(entry);
                DrawPlaybackSection(entry);
                DrawVariationSection(entry);
                DrawSpatialSection(entry);
                DrawClipsSection(libraryIndex, entry);

                entry.type = library.defaultType;

                EditorGUIUtility.labelWidth = oldLabelWidth;

                if (EditorGUI.EndChangeCheck())
                    Save();
            }
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(4);
    }

    private void DrawEntryHeader(AudioLibraryData library, AudioEntryData entry, int entryIndex)
    {
        string key = GetEntryKey(entry);

        EditorGUILayout.BeginHorizontal();

        entryFoldouts[key] = EditorGUILayout.Foldout(
            entryFoldouts[key],
            string.IsNullOrWhiteSpace(entry.displayName) ? "(Unnamed Audio)" : entry.displayName,
            true);

        GUILayout.FlexibleSpace();

        DrawTypeBadge(library.defaultType, 60f);

        if (DrawColoredButton("Preview", new Color(0.24f, 0.56f, 0.86f), GUILayout.Width(70)))
        {
            GUI.FocusControl(null);

            if (!IsEditingDisabled())
                Save();

            AudioPreviewUtility.Play(entry);
        }

        if (DrawColoredButton("Stop", new Color(0.38f, 0.38f, 0.38f), GUILayout.Width(50)))
        {
            AudioPreviewUtility.Stop();
        }

        using (new EditorGUI.DisabledScope(IsEditingDisabled()))
        {
            if (DrawColoredButton("Remove", new Color(0.78f, 0.28f, 0.28f), GUILayout.Width(70)))
            {
                RemoveEntry(library, entry, entryIndex);
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Entry Sections

    private void DrawIdentitySection(AudioEntryData entry)
    {
        string key = GetEntryKey(entry);

        identityFoldouts[key] = EditorGUILayout.Foldout(
            identityFoldouts[key],
            "Identity",
            true,
            EditorStyles.foldoutHeader);

        if (!identityFoldouts[key])
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Guid", entry.guid);
        }

        entry.id = EditorGUILayout.TextField("ID", entry.id);
        entry.displayName = EditorGUILayout.TextField("Display Name", entry.displayName);

        EditorGUILayout.EndVertical();
    }

    private void DrawPlaybackSection(AudioEntryData entry)
    {
        string key = GetEntryKey(entry);

        playbackFoldouts[key] = EditorGUILayout.Foldout(
            playbackFoldouts[key],
            "Playback",
            true,
            EditorStyles.foldoutHeader);

        if (!playbackFoldouts[key])
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        entry.volume = EditorGUILayout.Slider("Volume", entry.volume, 0f, 1f);
        entry.pitch = EditorGUILayout.Slider("Pitch", entry.pitch, 0.1f, 3f);
        entry.loop = EditorGUILayout.Toggle("Loop", entry.loop);
        entry.cooldown = EditorGUILayout.FloatField("Cooldown", entry.cooldown);

        if (entry.cooldown < 0f)
            entry.cooldown = 0f;

        EditorGUILayout.EndVertical();
    }

    private void DrawVariationSection(AudioEntryData entry)
    {
        string key = GetEntryKey(entry);

        variationFoldouts[key] = EditorGUILayout.Foldout(
            variationFoldouts[key],
            "Variation",
            true,
            EditorStyles.foldoutHeader);

        if (!variationFoldouts[key])
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        entry.randomClip = EditorGUILayout.Toggle("Random Clip", entry.randomClip);
        entry.randomPitch = EditorGUILayout.Toggle("Random Pitch", entry.randomPitch);

        using (new EditorGUI.DisabledScope(!entry.randomPitch))
        {
            entry.pitchRange = EditorGUILayout.Vector2Field("Pitch Range", entry.pitchRange);

            if (entry.pitchRange.x < 0.1f)
                entry.pitchRange.x = 0.1f;

            if (entry.pitchRange.y < entry.pitchRange.x)
                entry.pitchRange.y = entry.pitchRange.x;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSpatialSection(AudioEntryData entry)
    {
        string key = GetEntryKey(entry);

        spatialFoldouts[key] = EditorGUILayout.Foldout(
            spatialFoldouts[key],
            "Spatial",
            true,
            EditorStyles.foldoutHeader);

        if (!spatialFoldouts[key])
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        entry.spatial3D = EditorGUILayout.Toggle("Spatial 3D", entry.spatial3D);

        using (new EditorGUI.DisabledScope(!entry.spatial3D))
        {
            entry.spatialBlend = EditorGUILayout.Slider("Spatial Blend", entry.spatialBlend, 0f, 1f);
            entry.minDistance = EditorGUILayout.FloatField("Min Distance", entry.minDistance);
            entry.maxDistance = EditorGUILayout.FloatField("Max Distance", entry.maxDistance);
            entry.rolloffMode = (AudioRolloffMode)EditorGUILayout.EnumPopup("Rolloff Mode", entry.rolloffMode);

            if (entry.minDistance < 0.01f)
                entry.minDistance = 0.01f;

            if (entry.maxDistance < entry.minDistance)
                entry.maxDistance = entry.minDistance;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawClipsSection(int libraryIndex, AudioEntryData entry)
    {
        string key = GetEntryKey(entry);

        clipsFoldouts[key] = EditorGUILayout.Foldout(
            clipsFoldouts[key],
            "Clips",
            true,
            EditorStyles.foldoutHeader);

        if (!clipsFoldouts[key])
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        SerializedObject serializedDatabase = new SerializedObject(database);
        SerializedProperty librariesProperty = serializedDatabase.FindProperty("libraries");

        SerializedProperty clipsProperty = FindClipsProperty(librariesProperty, libraryIndex, entry);
        if (clipsProperty != null)
        {
            EditorGUILayout.PropertyField(clipsProperty, GUIContent.none, true);
            serializedDatabase.ApplyModifiedProperties();
        }
        else
        {
            EditorGUILayout.HelpBox("Could not resolve clips property.", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Foldout State

    private void EnsureEntryFoldoutState(AudioEntryData entry)
    {
        string key = GetEntryKey(entry);

        if (!entryFoldouts.ContainsKey(key))
            entryFoldouts[key] = false;

        if (!identityFoldouts.ContainsKey(key))
            identityFoldouts[key] = false;

        if (!playbackFoldouts.ContainsKey(key))
            playbackFoldouts[key] = true;

        if (!variationFoldouts.ContainsKey(key))
            variationFoldouts[key] = false;

        if (!spatialFoldouts.ContainsKey(key))
            spatialFoldouts[key] = false;

        if (!clipsFoldouts.ContainsKey(key))
            clipsFoldouts[key] = false;
    }

    private string GetEntryKey(AudioEntryData entry)
    {
        if (entry == null)
            return "null";

        if (!string.IsNullOrWhiteSpace(entry.guid))
            return entry.guid;

        if (!string.IsNullOrWhiteSpace(entry.id))
            return entry.id;

        return RuntimeHelpers.GetHashCode(entry).ToString();
    }

    #endregion

    #region Library / Entry Operations

    private void AddLibrary()
    {
        database.EditorLibraries.Add(new AudioLibraryData
        {
            libraryName = "New Library",
            defaultType = AudioType.SFX,
            expanded = true
        });

        selectedLibraryIndex = database.EditorLibraries.Count - 1;
        Save();
    }

    private void DeleteLibrary(AudioLibraryData library, int libraryIndex)
    {
        if (!EditorUtility.DisplayDialog(
                "Delete Library",
                $"Delete library '{library.libraryName}' and all entries inside it?",
                "Delete",
                "Cancel"))
        {
            return;
        }

        database.EditorLibraries.RemoveAt(libraryIndex);
        selectedLibraryIndex = -1;

        Save();
        GUIUtility.ExitGUI();
    }

    private void RemoveEntry(AudioLibraryData library, AudioEntryData entry, int entryIndex)
    {
        if (!EditorUtility.DisplayDialog(
                "Remove Audio Entry",
                $"Remove audio entry '{entry.displayName}'?",
                "Remove",
                "Cancel"))
        {
            return;
        }

        library.entries.RemoveAt(entryIndex);

        Save();
        GUIUtility.ExitGUI();
    }

    private void SyncEntryTypes(AudioLibraryData library)
    {
        if (library == null || library.entries == null)
            return;

        foreach (AudioEntryData entry in library.entries)
        {
            if (entry == null)
                continue;

            entry.type = library.defaultType;
        }
    }

    private bool ClipExistsInLibrary(AudioLibraryData library, AudioClip clip)
    {
        if (library == null || library.entries == null || clip == null)
            return false;

        foreach (AudioEntryData existingEntry in library.entries)
        {
            if (existingEntry == null || existingEntry.clips == null)
                continue;

            foreach (AudioClip existingClip in existingEntry.clips)
            {
                if (existingClip == clip)
                    return true;
            }
        }

        return false;
    }

    private bool EntryGuidExists(string guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
            return false;

        if (database == null || database.EditorLibraries == null)
            return false;

        foreach (AudioLibraryData library in database.EditorLibraries)
        {
            if (library == null || library.entries == null)
                continue;

            foreach (AudioEntryData entry in library.entries)
            {
                if (entry == null)
                    continue;

                if (entry.guid == guid)
                    return true;
            }
        }

        return false;
    }

    #endregion

    #region Validation / Save / Safety

    private void ValidateDatabase()
    {
        List<string> errors = AudioDatabaseValidator.Validate(database);

        if (errors.Count == 0)
        {
            Debug.Log("[AudioDatabase] Validation passed.");
            return;
        }

        Debug.LogWarning($"[AudioDatabase] Validation failed. Error count: {errors.Count}");

        foreach (string error in errors)
        {
            Debug.LogWarning(error);
        }
    }

    private void Save()
    {
        if (database == null)
            return;

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        AudioEventRefDrawer.ClearCache();
    }

    private void DrawPlayModeWarning()
    {
        if (!IsEditingDisabled())
            return;

        EditorGUILayout.HelpBox(
            "Audio Database editing is disabled during Play Mode. You can inspect and preview, but cannot add, remove, or edit entries.",
            MessageType.Warning);
    }

    private bool IsEditingDisabled()
    {
        return EditorApplication.isPlayingOrWillChangePlaymode;
    }

    #endregion

    #region SerializedProperty Utilities

    private SerializedProperty FindClipsProperty(
        SerializedProperty librariesProperty,
        int libraryIndex,
        AudioEntryData targetEntry)
    {
        if (librariesProperty == null)
            return null;

        if (libraryIndex < 0 || libraryIndex >= librariesProperty.arraySize)
            return null;

        SerializedProperty libraryProperty = librariesProperty.GetArrayElementAtIndex(libraryIndex);
        SerializedProperty entriesProperty = libraryProperty.FindPropertyRelative("entries");

        if (entriesProperty == null)
            return null;

        for (int entryIndex = 0; entryIndex < entriesProperty.arraySize; entryIndex++)
        {
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(entryIndex);
            SerializedProperty guidProperty = entryProperty.FindPropertyRelative("guid");

            if (guidProperty != null && guidProperty.stringValue == targetEntry.guid)
                return entryProperty.FindPropertyRelative("clips");
        }

        return null;
    }

    #endregion

    #region Query Helpers

    private bool TryGetSelectedLibrary(out AudioLibraryData library)
    {
        library = null;

        if (database == null || database.EditorLibraries == null)
            return false;

        if (selectedLibraryIndex < 0 || selectedLibraryIndex >= database.EditorLibraries.Count)
            return false;

        library = database.EditorLibraries[selectedLibraryIndex];

        return library != null;
    }

    private bool PassesLibrarySearch(AudioLibraryData library)
    {
        if (library == null)
            return false;

        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        string libraryName = library.libraryName ?? string.Empty;
        return libraryName.ToLowerInvariant().Contains(searchText.ToLowerInvariant());
    }

    private int GetLibraryCount()
    {
        if (database == null || database.EditorLibraries == null)
            return 0;

        return database.EditorLibraries.Count;
    }

    private int GetEntryCount(AudioLibraryData library)
    {
        if (library == null || library.entries == null)
            return 0;

        return library.entries.Count;
    }

    #endregion

    #region Visual Helpers

    private bool DrawColoredButton(string label, Color color, params GUILayoutOption[] options)
    {
        Color oldBackground = GUI.backgroundColor;
        Color oldContent = GUI.contentColor;

        GUI.backgroundColor = color;
        GUI.contentColor = Color.white;

        bool clicked = GUILayout.Button(label, options);

        GUI.backgroundColor = oldBackground;
        GUI.contentColor = oldContent;

        return clicked;
    }

    private void DrawTypeBadge(AudioType type, float width = 70f)
    {
        Rect rect = GUILayoutUtility.GetRect(width, 20f, GUILayout.Width(width));

        EditorGUI.DrawRect(rect, GetTypeColor(type));

        GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        badgeStyle.normal.textColor = Color.white;

        GUI.Label(rect, type.ToString(), badgeStyle);
    }

    private Color GetTypeColor(AudioType type)
    {
        return type switch
        {
            AudioType.Music => new Color(0.62f, 0.45f, 0.95f),
            AudioType.UI => new Color(0.25f, 0.75f, 0.95f),
            AudioType.Ambience => new Color(0.35f, 0.78f, 0.45f),
            AudioType.SFX => new Color(0.95f, 0.58f, 0.22f),
            AudioType.VoiceOver => new Color(0.92f, 0.38f, 0.72f),
            _ => new Color(0.60f, 0.60f, 0.60f),
        };
    }

    #endregion
}
#endif