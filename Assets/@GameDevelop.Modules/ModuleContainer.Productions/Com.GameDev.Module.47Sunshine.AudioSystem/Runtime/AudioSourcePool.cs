using System.Collections.Generic;
using UnityEngine;

public class AudioSourcePool : MonoBehaviour
{
    [SerializeField] private AudioSource sourcePrefab;
    [SerializeField] private int initialSize = 16;

    private readonly Queue<AudioSource> pool = new();

    private void Awake()
    {
        for (int i = 0; i < initialSize; i++)
        {
            AudioSource source = CreateSource();
            ResetSource(source);
            source.gameObject.SetActive(false);
            pool.Enqueue(source);
        }
    }

    public AudioSource Get()
    {
        AudioSource source = pool.Count > 0 ? pool.Dequeue() : CreateSource();

        source.gameObject.SetActive(true);
        source.transform.SetParent(transform);
        source.transform.localPosition = Vector3.zero;
        source.transform.localRotation = Quaternion.identity;
        source.transform.localScale = Vector3.one;

        return source;
    }

    public void Release(AudioSource source)
    {
        if (source == null)
            return;

        ResetSource(source);

        source.transform.SetParent(transform);
        source.transform.localPosition = Vector3.zero;
        source.transform.localRotation = Quaternion.identity;
        source.transform.localScale = Vector3.one;

        source.gameObject.SetActive(false);
        pool.Enqueue(source);
    }

    private AudioSource CreateSource()
    {
        AudioSource source;

        if (sourcePrefab != null)
        {
            source = Instantiate(sourcePrefab, transform);
            source.gameObject.name = "PooledAudioSource";
        }
        else
        {
            GameObject go = new GameObject("PooledAudioSource");
            go.transform.SetParent(transform);

            source = go.AddComponent<AudioSource>();
        }

        ResetSource(source);
        return source;
    }

    private void ResetSource(AudioSource source)
    {
        source.Stop();

        source.clip = null;
        source.outputAudioMixerGroup = null;

        source.playOnAwake = false;
        source.loop = false;
        source.mute = false;
        source.bypassEffects = false;
        source.bypassListenerEffects = false;
        source.bypassReverbZones = false;

        source.volume = 1f;
        source.pitch = 1f;
        source.panStereo = 0f;

        source.spatialBlend = 0f;
        source.spatialize = false;
        source.spatializePostEffects = false;

        source.minDistance = 1f;
        source.maxDistance = 500f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.dopplerLevel = 1f;
        source.spread = 0f;

        source.priority = 128;
        source.reverbZoneMix = 1f;

        source.time = 0f;
    }
}