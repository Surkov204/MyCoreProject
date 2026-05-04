using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Com.GameDev.Module.UISystem
{
    public class UIManager : IUIService, IInitializable, ITickable, IDisposable
    {
        private readonly DiContainer container;
        private readonly UIConfigExtend uiConfig;

        private readonly Dictionary<Type, UIBase> spawned = new();
        private readonly List<UIBase> popupStack = new();

        private UIBase currentFullScreen;
        private UIBase currentHUD;
        private UIBase currentPopup;
        private UIToast currentToast;
        private UILoading currentLoading;

        public bool HasPopupOnTop => currentPopup != null || popupStack.Count > 0;

        public UIManager(
            DiContainer container,
            UIConfigExtend uiConfig)
        {
            this.container = container;
            this.uiConfig = uiConfig;
        }

        public void Initialize()
        {
            if (uiConfig == null)
            {
                Debug.LogError("[UIManager] UIConfigExtend is null.");
                return;
            }

            uiConfig.ValidateConfig(true);

            PreloadConfiguredUI();
            ShowInitialUI();
        }

        public void Tick()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Back();
            }
        }

        public void Dispose()
        {
            foreach (UIBase ui in spawned.Values)
            {
                if (ui != null)
                    ui.OnClose -= HandleUIClose;
            }

            spawned.Clear();
            popupStack.Clear();

            currentFullScreen = null;
            currentHUD = null;
            currentPopup = null;
            currentToast = null;
            currentLoading = null;
        }

        private void PreloadConfiguredUI()
        {
            foreach (UIConfigExtend.UIPairExtend pair in uiConfig.GetAll())
            {
                if (!uiConfig.ShouldPreload(pair) || pair.prefab == null)
                    continue;

                Type type = pair.prefab.GetType();

                if (spawned.ContainsKey(type))
                    continue;

                UIBase instance = container.InstantiatePrefabForComponent<UIBase>(
                    pair.prefab.gameObject
                );

                instance.gameObject.SetActive(false);
                instance.OnClose -= HandleUIClose;
                instance.OnClose += HandleUIClose;

                spawned[type] = instance;

                SetCanvasProperties(instance, pair);
            }
        }

        private void ShowInitialUI()
        {
            foreach (UIConfigExtend.UIPairExtend pair in uiConfig.GetAll())
            {
                if (!pair.showOnInit || pair.prefab == null)
                    continue;

                Type type = pair.prefab.GetType();

                MethodInfo showMethod = GetType()
                    .GetMethod(nameof(Show))
                    ?.MakeGenericMethod(type);

                showMethod?.Invoke(this, new object[] { null, null });
            }
        }

        public void Show<T>(ViewData data = null, Action onComplete = null) where T : UIBase
        {
            if (typeof(UIToast).IsAssignableFrom(typeof(T)))
            {
                ShowToast(data);
                onComplete?.Invoke();
                return;
            }

            if (currentToast != null)
            {
                currentToast.HideAllToasts();
            }

            ShowAsync<T>(data, onComplete).Forget();
        }

        private async UniTaskVoid ShowAsync<T>(
            ViewData data = null,
            Action onComplete = null) where T : UIBase
        {
            Type type = typeof(T);

            UIConfigExtend.UIPairExtend pair = uiConfig.Get(type);
            if (pair == null)
            {
                Debug.LogError($"[UIManager] No UIConfig for {type.Name}");
                return;
            }

            ApplyBeforeShowAction(pair, type);

            UILoading loading = null;

            PreLoadAttribute preloadAttr = type.GetCustomAttribute<PreLoadAttribute>();
            if (preloadAttr != null && preloadAttr.ShowLoading)
            {
                loading = GetOrSpawn<UILoading>();

                if (loading != null)
                {
                    currentLoading = loading;

                    UIConfigExtend.UIPairExtend loadingPair = uiConfig.Get(loading.UIType);
                    SetCanvasProperties(loading, loadingPair);

                    loading.OnShow(
                        loadingPair != null
                            ? loadingPair.animationType
                            : UIAnimationType.FadeScale
                    );
                }
            }

            UIBase newUI = GetOrSpawn<T>();
            if (newUI == null)
            {
                Debug.LogError($"[UIManager] Cannot spawn UI: {type.Name}");

                if (loading != null)
                {
                    UIConfigExtend.UIPairExtend loadingPair = uiConfig.Get(loading.UIType);
                    loading.HideByExit(
                        loadingPair != null
                            ? loadingPair.animationType
                            : UIAnimationType.FadeScale
                    );
                }

                return;
            }

            newUI.OnClose -= HandleUIClose;
            newUI.OnClose += HandleUIClose;

            SetCanvasProperties(newUI, pair);
            newUI.Init(data);

            if (loading != null)
            {
                await loading.RunProgressRoutine(
                    newUI.OnWaitProgressAsync,
                    loading.UpdateProgressBarSmoothly
                );
            }
            else
            {
                await newUI.OnWaitProgressAsync(null);
            }

            HandleCurrentUIBeforeShow(newUI, pair);

            newUI.OnShow(pair.animationType);
            onComplete?.Invoke();

            if (loading != null)
            {
                UIConfigExtend.UIPairExtend loadingPair = uiConfig.Get(loading.UIType);

                loading.HideByExit(
                    loadingPair != null
                        ? loadingPair.animationType
                        : UIAnimationType.FadeScale
                );

                currentLoading = null;
            }
        }

        public void Hide<T>() where T : UIBase
        {
            Type type = typeof(T);

            if (!spawned.TryGetValue(type, out UIBase ui) || ui == null)
                return;

            UIConfigExtend.UIPairExtend pair = uiConfig.Get(type);
            if (pair == null)
                return;

            HideUI(
                ui,
                pair.animationType,
                uiConfig.ShouldDestroyAfterHide(pair)
            );
        }

        public bool IsVisible<T>() where T : UIBase
        {
            return spawned.TryGetValue(typeof(T), out UIBase ui)
                   && ui != null
                   && ui.IsVisible;
        }

        public void Back()
        {
            if (currentPopup == null)
                return;

            UIBase closingPopup = currentPopup;
            UIConfigExtend.UIPairExtend closingPair = uiConfig.Get(closingPopup.UIType);

            UIAnimationType animationType = closingPair != null
                ? closingPair.animationType
                : UIAnimationType.FadeScale;

            popupStack.Remove(closingPopup);
            closingPopup.HideByExit(animationType);

            currentPopup = popupStack.Count > 0
                ? popupStack[^1]
                : null;

            if (currentPopup != null)
            {
                UIConfigExtend.UIPairExtend currentPair = uiConfig.Get(currentPopup.UIType);

                SetCanvasProperties(currentPopup, currentPair);

                currentPopup.OnShow(
                    currentPair != null
                        ? currentPair.animationType
                        : UIAnimationType.FadeScale
                );
            }
        }

        public void HideAll()
        {
            if (currentFullScreen != null)
            {
                currentFullScreen.HideByExit(currentFullScreen.LastShowAnimationType);
            }

            if (currentHUD != null)
            {
                currentHUD.HideByExit(currentHUD.LastShowAnimationType);
            }

            if (currentPopup != null)
            {
                currentPopup.HideByExit(currentPopup.LastShowAnimationType);
            }

            if (currentToast != null)
            {
                currentToast.HideAllToasts();
            }

            if (currentLoading != null)
            {
                currentLoading.HideByExit(currentLoading.LastShowAnimationType);
            }

            foreach (UIBase popup in popupStack.ToList())
            {
                if (popup != null)
                {
                    popup.HideByExit(popup.LastShowAnimationType);
                }
            }

            popupStack.Clear();

            currentFullScreen = null;
            currentHUD = null;
            currentPopup = null;
            currentLoading = null;
        }

        private void HandleCurrentUIBeforeShow(
            UIBase newUI,
            UIConfigExtend.UIPairExtend pair)
        {
            switch (pair.canvasType)
            {
                case CanvasType.FullScreen:
                    HandleFullScreenBeforeShow(newUI);
                    break;

                case CanvasType.HUD:
                    HandleHUDBeforeShow(newUI);
                    break;

                case CanvasType.Popup:
                    HandlePopupBeforeShow(newUI);
                    break;

                case CanvasType.Toast:
                    currentToast = newUI as UIToast;
                    break;

                case CanvasType.Loading:
                    currentLoading = newUI as UILoading;
                    break;
            }
        }

        private void HandleFullScreenBeforeShow(UIBase newUI)
        {
            if (currentFullScreen != null && currentFullScreen != newUI)
            {
                UIConfigExtend.UIPairExtend oldPair = uiConfig.Get(currentFullScreen.UIType);

                currentFullScreen.HideByOverride(
                    oldPair != null
                        ? oldPair.animationType
                        : UIAnimationType.FadeScale
                );
            }

            currentFullScreen = newUI;
        }

        private void HandleHUDBeforeShow(UIBase newUI)
        {
            if (currentHUD != null && currentHUD != newUI)
            {
                UIConfigExtend.UIPairExtend oldPair = uiConfig.Get(currentHUD.UIType);

                currentHUD.HideByOverride(
                    oldPair != null
                        ? oldPair.animationType
                        : UIAnimationType.FadeScale
                );
            }

            currentHUD = newUI;
        }

        private void HandlePopupBeforeShow(UIBase newUI)
        {
            popupStack.Remove(newUI);
            popupStack.Add(newUI);

            currentPopup = newUI;

            for (int i = 0; i < popupStack.Count; i++)
            {
                UIBase popup = popupStack[i];

                if (popup == null)
                    continue;

                UIConfigExtend.UIPairExtend pair = uiConfig.Get(popup.UIType);
                SetCanvasProperties(popup, pair);
            }
        }

        private void HideUI(
            UIBase ui,
            UIAnimationType animationType,
            bool destroyAfterHide)
        {
            if (ui == null)
                return;

            ui.HideByExit(animationType);
            HandleUIClose(ui);

            if (!destroyAfterHide)
                return;

            Type type = ui.UIType;

            DOVirtual.DelayedCall(0.4f, () =>
            {
                if (ui == null)
                    return;

                UnityEngine.Object.Destroy(ui.gameObject);
                spawned.Remove(type);
            }, true);
        }

        private void HandleUIClose(UIBase ui)
        {
            if (ui == null)
                return;

            popupStack.Remove(ui);

            if (currentFullScreen == ui)
                currentFullScreen = null;

            if (currentHUD == ui)
                currentHUD = null;

            if (currentPopup == ui)
            {
                currentPopup = popupStack.Count > 0
                    ? popupStack[^1]
                    : null;
            }

            if (currentLoading == ui)
                currentLoading = null;
        }

        private T GetOrSpawn<T>() where T : UIBase
        {
            Type type = typeof(T);

            if (spawned.TryGetValue(type, out UIBase cached) && cached != null)
                return cached as T;

            UIConfigExtend.UIPairExtend pair = uiConfig.Get(type);
            if (pair == null || pair.prefab == null)
            {
                Debug.LogError($"[UIManager] Missing prefab config for {type.Name}");
                return null;
            }

            T ui = container.InstantiatePrefabForComponent<T>(
                pair.prefab.gameObject
            );

            ui.gameObject.SetActive(false);
            ui.OnClose -= HandleUIClose;
            ui.OnClose += HandleUIClose;

            spawned[type] = ui;

            SetCanvasProperties(ui, pair);

            return ui;
        }

        private void ShowToast(ViewData data)
        {
            UIToast toast = GetOrSpawn<UIToast>();
            if (toast == null)
                return;

            currentToast = toast;

            UIConfigExtend.UIPairExtend pair = uiConfig.Get(toast.UIType);
            SetCanvasProperties(toast, pair);

            toast.OnShow(UIAnimationType.None);
            toast.Init(data);
        }

        private void SetCanvasProperties(
            UIBase ui,
            UIConfigExtend.UIPairExtend pair)
        {
            if (ui == null || ui.Canvas == null)
                return;

            if (pair == null)
            {
                Debug.LogError($"[UIManager] Missing UIConfig pair for {ui.UIType.Name}");
                return;
            }

            CanvasType canvasType = pair.canvasType;
            int baseOrder = GetBaseOrder(canvasType);

            ui.Canvas.overrideSorting = true;
            ui.Canvas.sortingOrder = uiConfig.ResolveSortingOrder(pair, baseOrder);

            if (canvasType == CanvasType.Popup)
            {
                int popupIndex = popupStack.IndexOf(ui);

                if (popupIndex < 0)
                    popupIndex = popupStack.Count;

                ui.Canvas.sortingOrder += popupIndex;
            }
        }

        private int GetBaseOrder(CanvasType type)
        {
            return type switch
            {
                CanvasType.FullScreen => 0,
                CanvasType.HUD => 100,
                CanvasType.Popup => 200,
                CanvasType.Toast => 300,
                CanvasType.Loading => 400,
                _ => 0
            };
        }

        private void ApplyBeforeShowAction(
        UIConfigExtend.UIPairExtend pair,
        Type targetType)
            {
                if (pair == null)
                    return;

                switch (pair.beforeShowAction)
                {
                    case UIBeforeShowAction.None:
                        return;

                    case UIBeforeShowAction.Back:
                        if (currentPopup != null && currentPopup.UIType != targetType)
                        {
                            Back();
                        }
                        return;

                    case UIBeforeShowAction.HideAll:
                        HideAll();
                        return;
                }
        }
    }
}