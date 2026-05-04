using UnityEngine;
using Zenject;

namespace Com.GameDev.Module.UISystem
{
    public class UIRootInstaller : MonoInstaller
    {
        [SerializeField] private Transform uiRoot;

        public override void InstallBindings()
        {
            Container.Bind<Transform>()
                .WithId(UIInjectIds.UIRoot)
                .FromInstance(uiRoot)
                .AsSingle();
        }
    }
}