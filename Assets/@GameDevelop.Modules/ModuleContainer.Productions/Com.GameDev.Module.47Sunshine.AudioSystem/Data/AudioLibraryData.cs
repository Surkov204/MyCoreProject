using System;
using System.Collections.Generic;

[Serializable]
public class AudioLibraryData
{
    public string libraryName;
    public List<AudioEntryData> entries = new();
}