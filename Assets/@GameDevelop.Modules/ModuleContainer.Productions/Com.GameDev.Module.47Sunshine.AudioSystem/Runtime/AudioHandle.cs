using UnityEngine;

public readonly struct AudioHandle
{
    public static readonly AudioHandle Invalid = new AudioHandle(default, null, null);

    public readonly AudioEventRef EventRef;
    public readonly AudioSource Source;

    private readonly AudioPlayer owner;

    public bool IsValid => EventRef.IsValid && Source != null && owner != null;

    public AudioHandle(AudioEventRef eventRef, AudioSource source, AudioPlayer owner)
    {
        EventRef = eventRef;
        Source = source;
        this.owner = owner;
    }

    public void Stop()
    {
        if (!IsValid)
            return;

        owner.Stop(this);
    }
}