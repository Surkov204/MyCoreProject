using System;
using Unity.VisualScripting;

namespace Com.GameDev.Module.UISystem
{
    public interface IUIService
    {
        bool HasPopupOnTop { get; }

        void Show<T>(ViewData data = null, Action onComplete = null) where T : UIBase;
        void Hide<T>() where T : UIBase;
        bool IsVisible<T>() where T : UIBase;

        void Back();
        void HideAll();
    }
}