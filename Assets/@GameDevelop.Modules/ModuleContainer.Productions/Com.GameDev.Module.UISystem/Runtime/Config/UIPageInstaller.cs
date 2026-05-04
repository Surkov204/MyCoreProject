using UnityEngine;
using Zenject;

namespace Com.GameDev.Module.UISystem
{
    [CreateAssetMenu(
        fileName = "UIPageInstaller",
        menuName = "GameDev Modules/UI System/UI Page Installer")]
    public class UIPageInstaller : ScriptableObjectInstaller<UIPageInstaller>
    {
        [SerializeField] private UIConfigExtend uiConfig;

        public override void InstallBindings()
        {
            Container.BindInstance(uiConfig)
                .AsSingle();

            Container.BindInterfacesAndSelfTo<UIManager>()
                .AsSingle()
                .NonLazy();
        }
    }
}