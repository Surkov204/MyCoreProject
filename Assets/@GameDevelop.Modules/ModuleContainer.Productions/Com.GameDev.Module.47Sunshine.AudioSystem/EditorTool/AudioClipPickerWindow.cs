#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AudioClipPickerWindow : EditorWindow
{
    private static Action<List<AudioClip>> onConfirm;

    private readonly List<AudioClip> clips = new List<AudioClip>();
    private readonly HashSet<AudioClip> selectedClips = new HashSet<AudioClip>();

    private Vector2 scroll;
    private string searchText = "";

    public static void Open(string title, Action<List<AudioClip>> confirmCallback)
    {
        AudioClipPickerWindow window = CreateInstance<AudioClipPickerWindow>();
        window.titleContent = new GUIContent(title);
        window.minSize = new Vector2(560, 520);

        onConfirm = confirmCallback;

        window.LoadAllAudioClips();
        window.ShowUtility();
    }

    private void OnGUI()
    {
        DrawHeader();

        GUILayout.Space(6);

        DrawClipList();

        GUILayout.Space(8);

        DrawFooter();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUILayout.Label("Project AudioClips", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("Search", GUILayout.Width(50));
        searchText = EditorGUILayout.TextField(searchText);

        if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            LoadAllAudioClips();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            $"Found: {clips.Count} clips  |  Selected: {selectedClips.Count}",
            EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    private void DrawClipList()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (AudioClip clip in clips)
        {
            if (clip == null)
                continue;

            if (!PassesSearch(clip))
                continue;

            DrawClipRow(clip);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawClipRow(AudioClip clip)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        bool isSelected = selectedClips.Contains(clip);
        bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(18));

        if (newSelected != isSelected)
        {
            if (newSelected)
                selectedClips.Add(clip);
            else
                selectedClips.Remove(clip);
        }

        EditorGUILayout.ObjectField(clip, typeof(AudioClip), false);

        float length = clip.length;
        GUILayout.Label($"{length:0.00}s", EditorStyles.miniLabel, GUILayout.Width(55));

        if (GUILayout.Button("Ping", GUILayout.Width(45)))
        {
            EditorGUIUtility.PingObject(clip);
            Selection.activeObject = clip;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Select All Visible", GUILayout.Height(28)))
        {
            foreach (AudioClip clip in clips)
            {
                if (clip != null && PassesSearch(clip))
                    selectedClips.Add(clip);
            }
        }

        if (GUILayout.Button("Clear", GUILayout.Height(28)))
        {
            selectedClips.Clear();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Cancel", GUILayout.Width(90), GUILayout.Height(28)))
        {
            Close();
        }

        GUI.backgroundColor = new Color(0.22f, 0.68f, 0.32f);

        if (GUILayout.Button("Add Selected", GUILayout.Width(120), GUILayout.Height(28)))
        {
            onConfirm?.Invoke(new List<AudioClip>(selectedClips));
            Close();
        }

        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void LoadAllAudioClips()
    {
        clips.Clear();
        selectedClips.Clear();

        string[] guids = AssetDatabase.FindAssets("t:AudioClip");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            if (clip != null)
                clips.Add(clip);
        }

        clips.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
    }

    private bool PassesSearch(AudioClip clip)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        string clipName = clip.name.ToLowerInvariant();
        string query = searchText.ToLowerInvariant();

        return clipName.Contains(query);
    }
}
#endif