using System.Collections.Generic;
using UnityEngine;

public class AudioService : IAudioService
{
    private readonly AudioDatabaseRuntimeCache cache;
    private readonly AudioPlayerRouter router;

    private readonly Dictionary<string, float> lastPlayTimes = new();

    public AudioService(
        AudioDatabaseRuntimeCache cache,
        AudioPlayerRouter router)
    {
        this.cache = cache;
        this.router = router;
    }

    public AudioHandle Play(AudioEventRef eventRef)
    {
        if (!TryResolvePlayer(eventRef, out AudioEntryData entry, out AudioPlayer player))
            return AudioHandle.Invalid;

        return player.Play(eventRef, entry);
    }

    public AudioHandle PlayAt(AudioEventRef eventRef, Vector3 position)
    {
        if (!TryResolvePlayer(eventRef, out AudioEntryData entry, out AudioPlayer player))
            return AudioHandle.Invalid;

        return player.PlayAt(eventRef, entry, position);
    }

    public AudioHandle PlayAttached(AudioEventRef eventRef, Transform target)
    {
        if (target == null)
            return AudioHandle.Invalid;

        if (!TryResolvePlayer(eventRef, out AudioEntryData entry, out AudioPlayer player))
            return AudioHandle.Invalid;

        return player.PlayAttached(eventRef, entry, target);
    }

    public void Stop(AudioHandle handle)
    {
        if (!handle.IsValid)
            return;

        handle.Stop();
    }

    public void Stop(AudioEventRef eventRef)
    {
        if (!cache.TryGet(eventRef, out AudioEntryData entry))
            return;

        router.GetPlayer(entry.type)?.Stop(eventRef);
    }

    public void StopAll()
    {
        router.StopAll();
    }

    private bool TryResolvePlayer(
        AudioEventRef eventRef,
        out AudioEntryData entry,
        out AudioPlayer player)
    {
        entry = null;
        player = null;

        if (!eventRef.IsValid)
            return false;

        if (!cache.TryGet(eventRef, out entry))
            return false;

        if (entry == null || !entry.IsValid)
            return false;

        if (!CanPlay(eventRef, entry))
            return false;

        player = router.GetPlayer(entry.type);

        return player != null;
    }

    private bool CanPlay(AudioEventRef eventRef, AudioEntryData entry)
    {
        if (entry.cooldown <= 0f)
            return true;

        string key = eventRef.Guid;

        if (lastPlayTimes.TryGetValue(key, out float lastTime))
        {
            if (Time.unscaledTime - lastTime < entry.cooldown)
                return false;
        }

        lastPlayTimes[key] = Time.unscaledTime;
        return true;
    }
}