using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AudioDatabase",
    menuName = "47Sunshine/Audio/Audio Database")]
public class AudioDatabase : ScriptableObject
{
    [SerializeField] private List<AudioLibraryData> libraries = new();

    public IReadOnlyList<AudioLibraryData> Libraries => libraries;

#if UNITY_EDITOR
    public List<AudioLibraryData> EditorLibraries => libraries;
#endif
}