using UnityEngine;

public class PlayAudioSignal
{
    public AudioEventRef EventRef;
    public Transform FollowTarget;
}

public class StopAudioSignal
{
    public AudioEventRef EventRef;
    public AudioHandle Handle;
}

public class StopAllAudioSignal
{
}