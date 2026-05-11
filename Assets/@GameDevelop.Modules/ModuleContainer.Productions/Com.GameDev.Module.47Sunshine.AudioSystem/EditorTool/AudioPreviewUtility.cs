#if UNITY_EDITOR
using UnityEngine;

public static class AudioPreviewUtility
{
    private static GameObject previewObject;
    private static AudioSource previewSource;

    public static void Play(AudioEntryData entry)
    {
        if (entry == null)
            return;

        AudioClip clip = entry.GetClip();

        if (clip == null)
            return;

        EnsurePreviewSource();

        previewSource.Stop();

        previewSource.clip = clip;
        previewSource.loop = entry.loop;
        previewSource.volume = Mathf.Clamp01(entry.volume);
        previewSource.pitch = ResolvePitch(entry);
        previewSource.spatialBlend = 0f;
        previewSource.playOnAwake = false;

        previewSource.Play();
    }

    public static void Stop()
    {
        if (previewSource == null)
            return;

        previewSource.Stop();
    }

    private static float ResolvePitch(AudioEntryData entry)
    {
        if (entry == null)
            return 1f;

        if (!entry.randomPitch)
            return Mathf.Clamp(entry.pitch, 0.1f, 3f);

        float min = Mathf.Min(entry.pitchRange.x, entry.pitchRange.y);
        float max = Mathf.Max(entry.pitchRange.x, entry.pitchRange.y);

        return Mathf.Clamp(Random.Range(min, max), 0.1f, 3f);
    }

    private static void EnsurePreviewSource()
    {
        if (previewObject != null && previewSource != null)
            return;

        previewObject = GameObject.Find("__47Sunshine_AudioPreview");

        if (previewObject == null)
        {
            previewObject = new GameObject("__47Sunshine_AudioPreview");
            previewObject.hideFlags = HideFlags.HideAndDontSave;
        }

        previewSource = previewObject.GetComponent<AudioSource>();

        if (previewSource == null)
            previewSource = previewObject.AddComponent<AudioSource>();

        previewSource.playOnAwake = false;
        previewSource.spatialBlend = 0f;
    }
}
#endif