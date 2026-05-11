using System;
using UnityEngine;

[Serializable]
public class AudioEntryData
{
    public string guid;
    public string id;
    public string displayName;

    public AudioType type;

    public AudioClip[] clips;

    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;

    public bool loop;
    public bool randomClip;
    public bool randomPitch;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    public float cooldown;

    public bool spatial3D;
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public float minDistance = 1f;
    public float maxDistance = 15f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    public bool IsValid => clips != null && clips.Length > 0;

    public AudioClip GetClip()
    {
        if (!IsValid)
            return null;

        if (!randomClip || clips.Length == 1)
            return clips[0];

        return clips[UnityEngine.Random.Range(0, clips.Length)];
    }

    public float GetPitch()
    {
        if (!randomPitch)
            return Mathf.Clamp(pitch, 0.1f, 3f);

        float min = Mathf.Min(pitchRange.x, pitchRange.y);
        float max = Mathf.Max(pitchRange.x, pitchRange.y);

        return Mathf.Clamp(UnityEngine.Random.Range(min, max), 0.1f, 3f);
    }

    public float GetSpatialBlend()
    {
        return spatial3D ? Mathf.Clamp01(spatialBlend) : 0f;
    }

    public float GetMinDistance()
    {
        return Mathf.Max(0.01f, minDistance);
    }

    public float GetMaxDistance()
    {
        return Mathf.Max(GetMinDistance(), maxDistance);
    }
}