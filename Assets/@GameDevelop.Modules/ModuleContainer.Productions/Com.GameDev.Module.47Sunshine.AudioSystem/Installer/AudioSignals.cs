using UnityEngine;

public enum AudioPlayMode
{
    Normal,
    AtPosition,
    Attached
}

public readonly struct PlayAudioSignal
{
    public readonly AudioEventRef EventRef;
    public readonly AudioPlayMode PlayMode;
    public readonly Vector3 Position;
    public readonly Transform Target;

    private PlayAudioSignal(
        AudioEventRef eventRef,
        AudioPlayMode playMode,
        Vector3 position,
        Transform target)
    {
        EventRef = eventRef;
        PlayMode = playMode;
        Position = position;
        Target = target;
    }

    public static PlayAudioSignal Normal(AudioEventRef eventRef)
    {
        return new PlayAudioSignal(
            eventRef,
            AudioPlayMode.Normal,
            Vector3.zero,
            null);
    }

    public static PlayAudioSignal AtPosition(AudioEventRef eventRef, Vector3 position)
    {
        return new PlayAudioSignal(
            eventRef,
            AudioPlayMode.AtPosition,
            position,
            null);
    }

    public static PlayAudioSignal Attached(AudioEventRef eventRef, Transform target)
    {
        return new PlayAudioSignal(
            eventRef,
            AudioPlayMode.Attached,
            Vector3.zero,
            target);
    }
}

public readonly struct StopAudioSignal
{
    public readonly AudioEventRef EventRef;
    public readonly AudioHandle Handle;

    public StopAudioSignal(AudioEventRef eventRef)
    {
        EventRef = eventRef;
        Handle = AudioHandle.Invalid;
    }

    public StopAudioSignal(AudioHandle handle)
    {
        EventRef = default;
        Handle = handle;
    }
}

public readonly struct StopAllAudioSignal
{
}