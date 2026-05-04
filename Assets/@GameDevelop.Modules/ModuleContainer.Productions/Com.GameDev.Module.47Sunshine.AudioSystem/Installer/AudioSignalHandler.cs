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
        if (signal.FollowTarget != null)
            audioService.Play(signal.EventRef, signal.FollowTarget);
        else
            audioService.Play(signal.EventRef);
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