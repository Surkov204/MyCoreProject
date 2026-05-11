using System;
using System.Collections.Generic;

[Serializable]
public class AudioLibraryData
{
    public string libraryName;
    public AudioType defaultType = AudioType.SFX;
    public List<AudioType> allowedTypes = new() { AudioType.SFX };

    public bool expanded = true;
    public List<AudioEntryData> entries = new();

    public bool Allows(AudioType type)
    {
        return allowedTypes == null || allowedTypes.Count == 0 || allowedTypes.Contains(type);
    }
}