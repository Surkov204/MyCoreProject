using System;
using Zenject;

public class AudioSignalHandler : IInitializable, IDisposable
{
    private readonly SignalBus signalBus;
    private readonly IAudioService audioService;

    public AudioSignalHandler(
        SignalBus signalBus,
        IAudioService audioService)
    {
        this.signalBus = signalBus;
        this.audioService = audioService;
    }

    public void Initialize()
    {
        signalBus.Subscribe<PlayAudioSignal>(OnPlay);
        signalBus.Subscribe<StopAudioSignal>(OnStop);
        signalBus.Subscribe<StopAllAudioSignal>(OnStopAll);
    }

    public void Dispose()
    {
        signalBus.Unsubscribe<PlayAudioSignal>(OnPlay);
        signalBus.Unsubscribe<StopAudioSignal>(OnStop);
        signalBus.Unsubscribe<StopAllAudioSignal>(OnStopAll);
    }

    private void OnPlay(PlayAudioSignal signal)
    {
        switch (signal.PlayMode)
        {
            case AudioPlayMode.Normal:
                audioService.Play(signal.EventRef);
                break;

            case AudioPlayMode.AtPosition:
                audioService.PlayAt(signal.EventRef, signal.Position);
                break;

            case AudioPlayMode.Attached:
                audioService.PlayAttached(signal.EventRef, signal.Target);
                break;

            default:
                audioService.Play(signal.EventRef);
                break;
        }
    }

    private void OnStop(StopAudioSignal signal)
    {
        if (signal.Handle.IsValid)
        {
            audioService.Stop(signal.Handle);
            return;
        }

        audioService.Stop(signal.EventRef);
    }

    private void OnStopAll(StopAllAudioSignal signal)
    {
        audioService.StopAll();
    }
}