using UnityEngine;

public interface IAudioService
{
    /// <summary>
    /// Play a non-positioned sound.
    /// Use for UI, Music, or normal 2D audio.
    /// </summary>
    AudioHandle Play(AudioEventRef eventRef);

    /// <summary>
    /// Play a sound at a fixed world position.
    /// Use for explosion, hit impact, door sound, pickup sound in world.
    /// </summary>
    AudioHandle PlayAt(AudioEventRef eventRef, Vector3 position);

    /// <summary>
    /// Play a sound attached to a Transform.
    /// Use for looping world sounds such as fire, machine, waterfall, monster idle sound.
    /// </summary>
    AudioHandle PlayAttached(AudioEventRef eventRef, Transform target);

    void Stop(AudioHandle handle);
    void Stop(AudioEventRef eventRef);
    void StopAll();
}