using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioSourcePool sourcePool;

    private readonly Dictionary<string, List<AudioSource>> playingSources = new();

    public AudioHandle Play(AudioEventRef eventRef, AudioEntryData entry)
    {
        if (!TryPrepareSource(entry, out AudioClip clip, out AudioSource source))
            return AudioHandle.Invalid;

        source.transform.SetParent(transform);
        source.transform.localPosition = Vector3.zero;

        source.clip = clip;
        ApplyEntrySettings(source, entry);

        return PlayPreparedSource(eventRef, entry, source);
    }

    public AudioHandle PlayAt(AudioEventRef eventRef, AudioEntryData entry, Vector3 position)
    {
        if (!TryPrepareSource(entry, out AudioClip clip, out AudioSource source))
            return AudioHandle.Invalid;

        source.transform.SetParent(transform);
        source.transform.position = position;

        source.clip = clip;
        ApplyEntrySettings(source, entry);

        return PlayPreparedSource(eventRef, entry, source);
    }

    public AudioHandle PlayAttached(AudioEventRef eventRef, AudioEntryData entry, Transform target)
    {
        if (target == null)
            return AudioHandle.Invalid;

        if (!TryPrepareSource(entry, out AudioClip clip, out AudioSource source))
            return AudioHandle.Invalid;

        source.transform.SetParent(target);
        source.transform.localPosition = Vector3.zero;

        source.clip = clip;
        ApplyEntrySettings(source, entry);

        return PlayPreparedSource(eventRef, entry, source);
    }

    private bool TryPrepareSource(
        AudioEntryData entry,
        out AudioClip clip,
        out AudioSource source)
    {
        clip = null;
        source = null;

        if (entry == null)
            return false;

        clip = entry.GetClip();

        if (clip == null)
            return false;

        source = sourcePool.Get();

        return source != null;
    }

    private AudioHandle PlayPreparedSource(
        AudioEventRef eventRef,
        AudioEntryData entry,
        AudioSource source)
    {
        Register(eventRef, source);

        source.Play();

        if (!entry.loop)
            StartCoroutine(ReleaseWhenFinished(eventRef, source));

        return new AudioHandle(eventRef, source, this);
    }

    private void ApplyEntrySettings(AudioSource source, AudioEntryData entry)
    {
        source.volume = Mathf.Clamp01(entry.volume);
        source.pitch = entry.GetPitch();
        source.loop = entry.loop;

        source.spatialBlend = entry.GetSpatialBlend();
        source.minDistance = entry.GetMinDistance();
        source.maxDistance = entry.GetMaxDistance();
        source.rolloffMode = entry.rolloffMode;

        source.playOnAwake = false;
    }

    public void Stop(AudioEventRef eventRef)
    {
        if (!eventRef.IsValid)
            return;

        string key = eventRef.Guid;

        if (!playingSources.TryGetValue(key, out List<AudioSource> sources))
            return;

        for (int i = sources.Count - 1; i >= 0; i--)
        {
            AudioSource source = sources[i];

            if (source == null)
                continue;

            source.Stop();
            sourcePool.Release(source);
        }

        sources.Clear();
    }

    public void Stop(AudioHandle handle)
    {
        if (!handle.IsValid)
            return;

        AudioSource source = handle.Source;

        if (source == null)
            return;

        source.Stop();
        Unregister(handle.EventRef, source);
        sourcePool.Release(source);
    }

    public void StopAll()
    {
        foreach (KeyValuePair<string, List<AudioSource>> pair in playingSources)
        {
            foreach (AudioSource source in pair.Value)
            {
                if (source == null)
                    continue;

                source.Stop();
                sourcePool.Release(source);
            }
        }

        playingSources.Clear();
    }

    private void Register(AudioEventRef eventRef, AudioSource source)
    {
        string key = eventRef.Guid;

        if (!playingSources.TryGetValue(key, out List<AudioSource> sources))
        {
            sources = new List<AudioSource>();
            playingSources.Add(key, sources);
        }

        sources.Add(source);
    }

    private void Unregister(AudioEventRef eventRef, AudioSource source)
    {
        string key = eventRef.Guid;

        if (!playingSources.TryGetValue(key, out List<AudioSource> sources))
            return;

        sources.Remove(source);
    }

    private IEnumerator ReleaseWhenFinished(AudioEventRef eventRef, AudioSource source)
    {
        yield return new WaitWhile(() => source != null && source.isPlaying);

        if (source == null)
            yield break;

        Unregister(eventRef, source);
        sourcePool.Release(source);
    }
}