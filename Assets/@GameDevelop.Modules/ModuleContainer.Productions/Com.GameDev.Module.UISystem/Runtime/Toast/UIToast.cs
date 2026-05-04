using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Com.GameDev.Module.UISystem
{
    public class UIToast : UIBase
    {
        [Header("Toast Root")]
        [SerializeField] private RectTransform toastRoot;

        [Header("Toast Prefabs")]
        [SerializeField] private GameObject toastMessageGray;
        [SerializeField] private GameObject toastMessageGreen;
        [SerializeField] private GameObject toastMessageRed;
        [SerializeField] private GameObject toastMessageYellow;

        [Header("Timing")]
        [SerializeField] private float timeAppear = 1f;
        [SerializeField] private float showDuration = 0.3f;
        [SerializeField] private float hideDuration = 0.5f;
        [SerializeField] private float moveUpDistance = 100f;

        private readonly Dictionary<ToastType, GameObject> prefabMap = new();
        private readonly Dictionary<ToastType, Queue<UIPopupToast>> poolMap = new();
        private readonly Dictionary<UIPopupToast, ToastType> toastTypeMap = new();
        private readonly List<UIPopupToast> activeToasts = new();

        protected override void OnInit()
        {
            base.OnInit();

            if (toastRoot == null)
                toastRoot = transform as RectTransform;

            prefabMap[ToastType.Gray] = toastMessageGray;
            prefabMap[ToastType.Green] = toastMessageGreen;
            prefabMap[ToastType.Red] = toastMessageRed;
            prefabMap[ToastType.Yellow] = toastMessageYellow;

            foreach (ToastType type in prefabMap.Keys)
            {
                if (!poolMap.ContainsKey(type))
                    poolMap[type] = new Queue<UIPopupToast>();
            }
        }

        public override void OnShow(UIAnimationType type = UIAnimationType.FadeScale)
        {
            gameObject.SetActive(true);
            IsVisible = true;
            IsAnimating = false;
        }

        public override void HideByExit(UIAnimationType type = UIAnimationType.FadeScale)
        {
            HideAllToasts();

            gameObject.SetActive(false);
            IsVisible = false;
            IsAnimating = false;

            NotifyClosed();
        }

        public override void HideByOverride(UIAnimationType type = UIAnimationType.FadeScale)
        {
            HideAllToasts();

            gameObject.SetActive(false);
            IsVisible = false;
            IsAnimating = false;
        }

        public override void Init(ViewData viewData)
        {
            base.Init(viewData);

            if (viewData is not UIToastData toastData)
            {
                Debug.LogWarning($"[UIToast] Invalid ViewData. Expected {nameof(UIToastData)}.");
                return;
            }

            UIPopupToast toast = GetToastFromPool(toastData.ToastType);
            if (toast == null)
                return;

            toast.transform.SetParent(toastRoot, false);
            toast.transform.SetAsLastSibling();

            toast.RectTransform.localScale = Vector3.zero;
            toast.RectTransform.localPosition = Vector3.zero;
            toast.CanvasGroup.alpha = 1f;
            toast.Text.text = toastData.Message;

            toast.gameObject.SetActive(true);

            activeToasts.Add(toast);
            toastTypeMap[toast] = toastData.ToastType;

            Sequence sequence = DOTween.Sequence().SetUpdate(true);

            sequence.Append(
                    toast.RectTransform
                        .DOScale(1f, showDuration)
                        .SetEase(Ease.OutBack)
                )
                .AppendInterval(timeAppear)
                .Append(
                    toast.RectTransform
                        .DOLocalMoveY(
                            toast.RectTransform.localPosition.y + moveUpDistance,
                            hideDuration)
                        .SetEase(Ease.OutQuad)
                )
                .Join(
                    toast.CanvasGroup
                        .DOFade(0f, hideDuration)
                        .SetEase(Ease.OutQuad)
                )
                .OnComplete(() =>
                {
                    ReturnToast(toast);
                })
                .SetLink(toast.gameObject);
        }

        private UIPopupToast GetToastFromPool(ToastType type)
        {
            if (!poolMap.ContainsKey(type))
            {
                Debug.LogError($"[UIToast] Missing pool for toast type: {type}");
                return null;
            }

            if (poolMap[type].Count > 0)
                return poolMap[type].Dequeue();

            if (!prefabMap.TryGetValue(type, out GameObject prefab) || prefab == null)
            {
                Debug.LogError($"[UIToast] Missing prefab for toast type: {type}");
                return null;
            }

            GameObject instance = Instantiate(prefab, toastRoot);
            instance.SetActive(false);

            UIPopupToast toast = instance.GetComponent<UIPopupToast>();
            if (toast == null)
            {
                Debug.LogError($"[UIToast] Prefab {prefab.name} missing {nameof(UIPopupToast)}.");
                Destroy(instance);
                return null;
            }

            return toast;
        }

        private void ReturnToast(UIPopupToast toast)
        {
            if (toast == null)
                return;

            DOTween.Kill(toast.gameObject);

            toast.gameObject.SetActive(false);
            activeToasts.Remove(toast);

            if (toastTypeMap.TryGetValue(toast, out ToastType type))
            {
                toastTypeMap.Remove(toast);
                poolMap[type].Enqueue(toast);
            }
        }

        public void HideAllToasts()
        {
            for (int i = activeToasts.Count - 1; i >= 0; i--)
            {
                ReturnToast(activeToasts[i]);
            }

            activeToasts.Clear();
            toastTypeMap.Clear();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            HideAllToasts();

            foreach (Queue<UIPopupToast> pool in poolMap.Values)
            {
                while (pool.Count > 0)
                {
                    UIPopupToast toast = pool.Dequeue();

                    if (toast != null)
                        Destroy(toast.gameObject);
                }
            }

            poolMap.Clear();
        }
    }
}