using System.Collections.Generic;
using UnityEngine;

public static class AudioDatabaseValidator
{
    public static List<string> Validate(AudioDatabase database)
    {
        List<string> errors = new();

        if (database == null)
        {
            errors.Add("AudioDatabase is null.");
            return errors;
        }

        HashSet<string> ids = new();
        HashSet<string> guids = new();

        foreach (AudioLibraryData library in database.EditorLibraries)
        {
            if (library == null)
                continue;

            if (string.IsNullOrWhiteSpace(library.libraryName))
                errors.Add("Library has empty name.");

            foreach (AudioEntryData entry in library.entries)
            {
                if (entry == null)
                    continue;

                if (string.IsNullOrWhiteSpace(entry.guid))
                    errors.Add($"Entry '{entry.displayName}' has empty guid.");

                if (string.IsNullOrWhiteSpace(entry.id))
                    errors.Add($"Entry '{entry.displayName}' has empty id.");

                if (!string.IsNullOrWhiteSpace(entry.guid) && !guids.Add(entry.guid))
                    errors.Add($"Duplicate guid: {entry.guid}");

                if (!string.IsNullOrWhiteSpace(entry.id) && !ids.Add(entry.id))
                    errors.Add($"Duplicate id: {entry.id}");

                if (!entry.IsValid)
                    errors.Add($"Entry '{entry.id}' has no audio clips.");
            }
        }

        return errors;
    }
}