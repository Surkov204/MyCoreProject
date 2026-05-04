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
    [Range(0f, 1f)] public float spatialBlend = 0f;

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
            return pitch;

        return UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
    }

    public float GetSpatialBlend()
    {
        return spatial3D ? 1f : spatialBlend;
    }
}