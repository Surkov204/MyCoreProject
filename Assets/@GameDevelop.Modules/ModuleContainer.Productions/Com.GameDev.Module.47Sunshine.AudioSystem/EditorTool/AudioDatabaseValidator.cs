using System.Collections.Generic;
using UnityEngine;

public static class AudioDatabaseValidator
{
    public static List<string> Validate(AudioDatabase database)
    {
        List<string> errors = new();

        if (database == null)
        {
            errors.Add("[AudioDatabase] Database is null.");
            return errors;
        }

        IReadOnlyList<AudioLibraryData> libraries = database.EditorLibraries;

        if (libraries == null || libraries.Count == 0)
        {
            errors.Add("[AudioDatabase] Database has no libraries.");
            return errors;
        }

        HashSet<string> guids = new();
        HashSet<string> globalIds = new();

        for (int libraryIndex = 0; libraryIndex < libraries.Count; libraryIndex++)
        {
            AudioLibraryData library = libraries[libraryIndex];

            if (library == null)
            {
                errors.Add($"[AudioDatabase] Library at index {libraryIndex} is null.");
                continue;
            }

            ValidateLibrary(errors, library, libraryIndex);
            ValidateEntries(errors, library, libraryIndex, guids, globalIds);
        }

        return errors;
    }

    private static void ValidateLibrary(
        List<string> errors,
        AudioLibraryData library,
        int libraryIndex)
    {
        if (string.IsNullOrWhiteSpace(library.libraryName))
        {
            errors.Add($"[AudioDatabase] Library at index {libraryIndex} has empty name.");
        }

        if (library.entries == null)
        {
            errors.Add($"[AudioDatabase] Library '{library.libraryName}' has null entry list.");
        }
    }

    private static void ValidateEntries(
        List<string> errors,
        AudioLibraryData library,
        int libraryIndex,
        HashSet<string> guids,
        HashSet<string> globalIds)
    {
        if (library.entries == null)
            return;

        HashSet<string> localIds = new();

        for (int entryIndex = 0; entryIndex < library.entries.Count; entryIndex++)
        {
            AudioEntryData entry = library.entries[entryIndex];

            if (entry == null)
            {
                errors.Add($"[AudioDatabase] Null entry in library '{library.libraryName}' at index {entryIndex}.");
                continue;
            }

            string location = $"Library '{library.libraryName}' / Entry index {entryIndex}";

            if (string.IsNullOrWhiteSpace(entry.guid))
            {
                errors.Add($"[AudioDatabase] {location} has empty GUID.");
            }
            else if (!guids.Add(entry.guid))
            {
                errors.Add($"[AudioDatabase] Duplicate GUID '{entry.guid}' at {location}.");
            }

            if (string.IsNullOrWhiteSpace(entry.id))
            {
                errors.Add($"[AudioDatabase] {location} has empty ID.");
            }
            else
            {
                string localIdKey = entry.id.ToLowerInvariant();
                string globalIdKey = $"{entry.type}/{entry.id}".ToLowerInvariant();

                if (!localIds.Add(localIdKey))
                {
                    errors.Add($"[AudioDatabase] Duplicate ID '{entry.id}' inside library '{library.libraryName}'.");
                }

                if (!globalIds.Add(globalIdKey))
                {
                    errors.Add($"[AudioDatabase] Duplicate global ID '{entry.id}' for type '{entry.type}'.");
                }
            }

            if (entry.type != library.defaultType)
            {
                errors.Add(
                    $"[AudioDatabase] {location} type mismatch. Entry type is '{entry.type}', but library type is '{library.defaultType}'.");
            }

            ValidateClips(errors, entry, location);
            ValidateSpatial(errors, entry, location);
        }
    }

    private static void ValidateClips(
        List<string> errors,
        AudioEntryData entry,
        string location)
    {
        if (entry.clips == null || entry.clips.Length == 0)
        {
            errors.Add($"[AudioDatabase] {location} has no AudioClip.");
            return;
        }

        for (int clipIndex = 0; clipIndex < entry.clips.Length; clipIndex++)
        {
            if (entry.clips[clipIndex] == null)
            {
                errors.Add($"[AudioDatabase] {location} has null clip at index {clipIndex}.");
            }
        }
    }

    private static void ValidateSpatial(
        List<string> errors,
        AudioEntryData entry,
        string location)
    {
        if (entry.minDistance < 0f)
        {
            errors.Add($"[AudioDatabase] {location} has invalid Min Distance: {entry.minDistance}.");
        }

        if (entry.maxDistance < entry.minDistance)
        {
            errors.Add(
                $"[AudioDatabase] {location} has Max Distance smaller than Min Distance. Min={entry.minDistance}, Max={entry.maxDistance}.");
        }

        if (entry.spatialBlend < 0f || entry.spatialBlend > 1f)
        {
            errors.Add($"[AudioDatabase] {location} has invalid Spatial Blend: {entry.spatialBlend}.");
        }
    }
}