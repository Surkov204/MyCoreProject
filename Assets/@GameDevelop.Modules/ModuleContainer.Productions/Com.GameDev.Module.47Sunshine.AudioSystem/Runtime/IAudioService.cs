using UnityEngine;

public interface IAudioService
{
    AudioHandle Play(AudioEventRef eventRef);
    AudioHandle Play(AudioEventRef eventRef, Transform followTarget);

    void Stop(AudioHandle handle);
    void Stop(AudioEventRef eventRef);
    void StopAll();
}