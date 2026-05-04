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
        return Play(eventRef, null);
    }

    public AudioHandle Play(AudioEventRef eventRef, Transform followTarget)
    {
        if (!cache.TryGet(eventRef, out AudioEntryData entry))
            return AudioHandle.Invalid;

        if (!entry.IsValid)
            return AudioHandle.Invalid;

        if (!CanPlay(eventRef, entry))
            return AudioHandle.Invalid;

        AudioPlayer player = router.GetPlayer(entry.type);
        if (player == null)
            return AudioHandle.Invalid;

        return player.Play(eventRef, entry, followTarget);
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