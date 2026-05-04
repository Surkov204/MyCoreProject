using System.Collections.Generic;

public class AudioDatabaseRuntimeCache
{
    private readonly Dictionary<string, AudioEntryData> entryMap = new();

    public AudioDatabaseRuntimeCache(AudioDatabase database)
    {
        Build(database);
    }

    public bool TryGet(AudioEventRef eventRef, out AudioEntryData entry)
    {
        if (!eventRef.IsValid)
        {
            entry = null;
            return false;
        }

        return entryMap.TryGetValue(eventRef.Guid, out entry);
    }

    private void Build(AudioDatabase database)
    {
        if (database == null)
            return;

        foreach (AudioLibraryData library in database.Libraries)
        {
            if (library?.entries == null)
                continue;

            foreach (AudioEntryData entry in library.entries)
            {
                if (entry == null)
                    continue;

                if (string.IsNullOrEmpty(entry.guid))
                    continue;

                entryMap[entry.guid] = entry;
            }
        }
    }
}