using UnityEngine;
using Zenject;

public class AudioInstaller : MonoInstaller
{
    [SerializeField] private AudioDatabase audioDatabase;
    [SerializeField] private AudioPlayerRouter audioPlayerRouter;

    public override void InstallBindings()
    {
        SignalBusInstaller.Install(Container);

        Container.BindInstance(audioDatabase).AsSingle();

        Container.Bind<AudioDatabaseRuntimeCache>()
            .AsSingle();

        Container.Bind<AudioPlayerRouter>()
            .FromInstance(audioPlayerRouter)
            .AsSingle();

        Container.Bind<IAudioService>()
            .To<AudioService>()
            .AsSingle();

        Container.DeclareSignal<PlayAudioSignal>();
        Container.DeclareSignal<StopAudioSignal>();
        Container.DeclareSignal<StopAllAudioSignal>();

        Container.BindInterfacesTo<AudioSignalHandler>().AsSingle();
    }
}