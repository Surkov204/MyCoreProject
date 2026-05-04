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
            source.gameObject.SetActive(false);
            pool.Enqueue(source);
        }
    }

    public AudioSource Get()
    {
        AudioSource source = pool.Count > 0 ? pool.Dequeue() : CreateSource();

        source.gameObject.SetActive(true);
        return source;
    }

    public void Release(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
        source.clip = null;
        source.loop = false;
        source.pitch = 1f;
        source.volume = 1f;
        source.spatialBlend = 0f;

        source.transform.SetParent(transform);
        source.transform.localPosition = Vector3.zero;

        source.gameObject.SetActive(false);
        pool.Enqueue(source);
    }

    private AudioSource CreateSource()
    {
        if (sourcePrefab != null)
            return Instantiate(sourcePrefab, transform);

        GameObject go = new GameObject("PooledAudioSource");
        go.transform.SetParent(transform);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;

        return source;
    }
}