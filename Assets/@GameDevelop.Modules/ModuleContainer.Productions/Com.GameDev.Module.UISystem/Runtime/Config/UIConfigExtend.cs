using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Com.GameDev.Module.UISystem
{
    [CreateAssetMenu(
        fileName = "UIConfig",
        menuName = "GameDev Modules/UI System/UI Config",
        order = 0)]
    public class UIConfigExtend : ScriptableObject
    {
        public const int UseDefaultSortingOrder = -1;

        [Serializable]
        public class UIPairExtend
        {
            [Header("Prefab")]
            public UIBase prefab;

            [Header("Presentation")]
            public CanvasType canvasType = CanvasType.FullScreen;
            public UIAnimationType animationType = UIAnimationType.FadeScale;
            public int customSortingOrder = UseDefaultSortingOrder;

            [Header("Lifecycle")]
            public UILifecycle lifecycle = UILifecycle.SpawnOnDemandCached;
            public bool showOnInit;

            [Header("Before Show")]
            public UIBeforeShowAction beforeShowAction = UIBeforeShowAction.None;

            [Header("Validation")]
            public bool requireGraphicRaycaster = true;

            [TextArea]
            public string note;

            public Type UIType => prefab != null ? prefab.GetType() : null;
            public string DisplayName => prefab != null ? prefab.name : "<Missing Prefab>";
        }

        [SerializeField] private List<UIPairExtend> uiList = new();

        private Dictionary<Type, UIPairExtend> lookup;

        public IReadOnlyList<UIPairExtend> GetAll()
        {
            EnsureLookup();
            return uiList;
        }

        public UIPairExtend Get(Type uiType)
        {
            if (uiType == null)
                return null;

            EnsureLookup();
            lookup.TryGetValue(uiType, out UIPairExtend pair);
            return pair;
        }

        public bool Contains(Type uiType)
        {
            if (uiType == null)
                return false;

            EnsureLookup();
            return lookup.ContainsKey(uiType);
        }

        public bool ShouldPreload(UIPairExtend pair)
        {
            return pair != null &&
                   pair.lifecycle == UILifecycle.PreloadPersistent;
        }

        public bool ShouldDestroyAfterHide(UIPairExtend pair)
        {
            return pair != null &&
                   pair.lifecycle == UILifecycle.DestroyAfterHide;
        }

        public int ResolveSortingOrder(UIPairExtend pair, int defaultOrder)
        {
            if (pair == null)
                return defaultOrder;

            return pair.customSortingOrder >= 0
                ? pair.customSortingOrder
                : defaultOrder;
        }

        public bool ValidateConfig(bool logDetails = true)
        {
            bool isValid = true;
            HashSet<Type> seenTypes = new();

            for (int i = 0; i < uiList.Count; i++)
            {
                UIPairExtend pair = uiList[i];

                if (pair == null)
                {
                    isValid = false;
                    LogError(logDetails, $"Element {i} is null.");
                    continue;
                }

                if (!ValidatePrefab(i, pair, seenTypes, logDetails))
                    isValid = false;

                ValidatePolicy(pair, logDetails);
            }

            return isValid;
        }

        private bool ValidatePrefab(
            int index,
            UIPairExtend pair,
            HashSet<Type> seenTypes,
            bool logDetails)
        {
            if (pair.prefab == null)
            {
                LogError(logDetails, $"Element {index} has null prefab.");
                return false;
            }

            bool isValid = true;
            Type type = pair.prefab.GetType();
            GameObject prefabObject = pair.prefab.gameObject;

            if (!seenTypes.Add(type))
            {
                isValid = false;
                LogError(logDetails, $"Duplicate UI type: {type.Name}");
            }

            if (!prefabObject.TryGetComponent(out Canvas _))
            {
                isValid = false;
                LogError(logDetails, $"{type.Name} is missing Canvas.");
            }

            if (!prefabObject.TryGetComponent(out CanvasGroup _))
            {
                isValid = false;
                LogError(logDetails, $"{type.Name} is missing CanvasGroup.");
            }

            if (pair.requireGraphicRaycaster &&
                !prefabObject.TryGetComponent(out GraphicRaycaster _))
            {
                isValid = false;
                LogError(logDetails, $"{type.Name} is missing GraphicRaycaster.");
            }

            return isValid;
        }

        private void ValidatePolicy(UIPairExtend pair, bool logDetails)
        {
            if (pair == null || pair.prefab == null)
                return;

            string uiName = pair.prefab.GetType().Name;

            if (pair.showOnInit && pair.canvasType == CanvasType.Popup)
            {
                LogWarning(logDetails, $"{uiName}: Popup should usually not use showOnInit.");
            }

            if (pair.showOnInit && pair.lifecycle == UILifecycle.DestroyAfterHide)
            {
                LogWarning(logDetails, $"{uiName}: showOnInit + DestroyAfterHide is usually suspicious.");
            }

            if (pair.canvasType == CanvasType.Toast &&
                pair.lifecycle == UILifecycle.DestroyAfterHide)
            {
                LogWarning(logDetails, $"{uiName}: Toast should usually be PreloadPersistent.");
            }

            if (pair.canvasType == CanvasType.Loading &&
                pair.lifecycle == UILifecycle.DestroyAfterHide)
            {
                LogWarning(logDetails, $"{uiName}: Loading should usually be PreloadPersistent.");
            }

            if (pair.beforeShowAction == UIBeforeShowAction.HideAll &&
                pair.showOnInit)
            {
                LogWarning(logDetails, $"{uiName}: showOnInit + HideAll before show may hide other initial UI.");
            }
        }

        private void EnsureLookup()
        {
            if (lookup != null)
                return;

            lookup = new Dictionary<Type, UIPairExtend>();

            foreach (UIPairExtend pair in uiList)
            {
                if (pair == null || pair.prefab == null)
                    continue;

                Type type = pair.prefab.GetType();

                if (!lookup.TryAdd(type, pair))
                {
                    Debug.LogWarning(
                        $"[UIConfigExtend] Duplicate UI type ignored: {type.Name}",
                        this
                    );
                }
            }
        }

        private void ClearLookup()
        {
            lookup = null;
        }

        private void LogError(bool enabled, string message)
        {
            if (!enabled)
                return;

            Debug.LogError($"[UIConfigExtend] {message}", this);
        }

        private void LogWarning(bool enabled, string message)
        {
            if (!enabled)
                return;

            Debug.LogWarning($"[UIConfigExtend] {message}", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClearLookup();
        }

        [ContextMenu("Validate UI Config")]
        private void ValidateFromContextMenu()
        {
            ValidateConfig(true);
        }
#endif
    }
}