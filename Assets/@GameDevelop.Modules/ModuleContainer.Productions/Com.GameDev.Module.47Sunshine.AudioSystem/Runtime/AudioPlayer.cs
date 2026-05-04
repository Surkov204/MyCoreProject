using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioSourcePool sourcePool;

    private readonly Dictionary<string, List<AudioSource>> playingSources = new();

    public AudioHandle Play(AudioEventRef eventRef, AudioEntryData entry, Transform followTarget)
    {
        AudioClip clip = entry.GetClip();
        if (clip == null)
            return AudioHandle.Invalid;

        AudioSource source = sourcePool.Get();

        source.clip = clip;
        source.volume = entry.volume;
        source.pitch = entry.GetPitch();
        source.loop = entry.loop;
        source.spatialBlend = entry.GetSpatialBlend();
        source.playOnAwake = false;

        if (followTarget != null)
        {
            source.transform.SetParent(followTarget);
            source.transform.localPosition = Vector3.zero;
        }
        else
        {
            source.transform.SetParent(transform);
            source.transform.localPosition = Vector3.zero;
        }

        Register(eventRef, source);

        source.Play();

        if (!entry.loop)
            StartCoroutine(ReleaseWhenFinished(eventRef, source));

        return new AudioHandle(eventRef, source, this);
    }

    public void Stop(AudioEventRef eventRef)
    {
        if (!eventRef.IsValid)
            return;

        string key = eventRef.Guid;

        if (!playingSources.TryGetValue(key, out var sources))
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

        source.Stop();
        Unregister(handle.EventRef, source);
        sourcePool.Release(source);
    }

    public void StopAll()
    {
        foreach (var pair in playingSources)
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

        if (!playingSources.TryGetValue(key, out var sources))
        {
            sources = new List<AudioSource>();
            playingSources.Add(key, sources);
        }

        sources.Add(source);
    }

    private void Unregister(AudioEventRef eventRef, AudioSource source)
    {
        string key = eventRef.Guid;

        if (!playingSources.TryGetValue(key, out var sources))
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