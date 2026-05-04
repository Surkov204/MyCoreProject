using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Com.GameDev.Module.UISystem
{
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Canvas))]
    [DisallowMultipleComponent]
    public class UIBase : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private RectTransform panelRoot;

        [Header("Optional: Control Buttons")]
        [SerializeField] private List<Button> listButtonControl = new();

        [Header("Button Animation")]
        [SerializeField] private bool enableButtonAnimation = false;
        [SerializeField] private UIButtonAnimationType buttonAnimationType = UIButtonAnimationType.None;
        [SerializeField] private float buttonAnimDelay = 0.2f;
        [SerializeField] private float buttonAnimDuration = 0.5f;
        [SerializeField] private float buttonAnimInterval = 0.12f;
        [SerializeField] private List<RectTransform> animatedButtons = new();

        [Header("Button Reverse On Hide")]
        [SerializeField] private bool reverseButtonAnimationOnHide = false;
        [SerializeField] private float buttonHideDelay = 0f;
        [SerializeField] private float buttonHideDuration = 0.25f;
        [SerializeField] private float buttonHideInterval = 0.06f;

        private Vector2 shownAnchoredPos;
        private bool hasCachedShownPos;

        private CanvasGroup canvasGroup;
        private Canvas canvas;

        private Tween fadeTween;
        private Tween scaleTween;
        private Tween slideTween;
        private DG.Tweening.Sequence buttonSequence;

        private Vector2 targetPos;
        private Vector2 hiddenPos;
        private bool hasSlideHiddenPos;

        private readonly Dictionary<RectTransform, Vector2> buttonTargetPos = new();
        private readonly Dictionary<RectTransform, Vector3> buttonTargetScale = new();
        private readonly Dictionary<RectTransform, float> buttonTargetAlpha = new();

        public event Action<UIBase> OnClose;

        public Type UIType => GetType();
        public Canvas Canvas => canvas;

        public bool IsVisible { get; protected set; }
        public bool IsAnimating { get; protected set; }
        public UIAnimationType LastShowAnimationType { get; private set; } = UIAnimationType.FadeScale;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvas = GetComponent<Canvas>();

            if (panelRoot == null)
                panelRoot = transform as RectTransform;
            CacheShownPositionIfNeeded();
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);

            OnInit();
        }

        protected virtual void OnDestroy()
        {
            KillPanelTweens();
            KillButtonTweens();
        }

        protected virtual void OnInit()
        {
        }

        public virtual void Init(ViewData viewData = null)
        {
        }

        public virtual async UniTask OnWaitProgressAsync(Action<float> reportProgress)
        {
            reportProgress?.Invoke(1f);
            await UniTask.Yield();
        }

        public virtual void OnShow(UIAnimationType type = UIAnimationType.FadeScale)
        {
            LastShowAnimationType = type;

            switch (type)
            {
                case UIAnimationType.None:
                    ShowInstant();
                    break;

                case UIAnimationType.FadeScale:
                    PlayFadeIn();
                    break;

                case UIAnimationType.SlideLeft:
                    PlaySlideIn(Vector2.left);
                    break;

                case UIAnimationType.SlideRight:
                    PlaySlideIn(Vector2.right);
                    break;

                case UIAnimationType.SlideTop:
                    PlaySlideIn(Vector2.up);
                    break;

                case UIAnimationType.SlideBottom:
                    PlaySlideIn(Vector2.down);
                    break;
            }

            if (enableButtonAnimation)
                PlayButtonAnimation();
        }

        public virtual void OnHide(UIAnimationType type = UIAnimationType.FadeScale)
        {
            HideByExit(type);
        }

        public virtual void OnHideAuto()
        {
            HideByExit(LastShowAnimationType);
        }

        public virtual void HideByOverride(UIAnimationType type = UIAnimationType.FadeScale)
        {
            HideInternal(type, invokeClose: false);
        }

        public virtual void HideByExit(UIAnimationType type = UIAnimationType.FadeScale)
        {
            HideInternal(type, invokeClose: true);
        }

        protected void NotifyClosed()
        {
            OnClose?.Invoke(this);
        }

        private void HideInternal(UIAnimationType type, bool invokeClose)
        {
            if (!enableButtonAnimation || !reverseButtonAnimationOnHide)
            {
                PlayPanelHide(type, invokeClose);
                return;
            }

            PlayButtonReverseAnimation();

            int count = animatedButtons != null ? animatedButtons.Count : 0;
            float total = buttonHideDelay + Mathf.Max(0, count - 1) * buttonHideInterval + buttonHideDuration;

            DOVirtual.DelayedCall(total, () =>
            {
                PlayPanelHide(type, invokeClose);
            }).SetUpdate(true);
        }

        private void ShowInstant()
        {
            KillPanelTweens();

            RectTransform rect = GetPanelRect();
            if (rect != null)
            {
                CacheShownPositionIfNeeded();
                rect.anchoredPosition = shownAnchoredPos;
                rect.localScale = Vector3.one;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            canvasGroup.alpha = 1f;
            IsVisible = true;
            IsAnimating = false;
        }

        private void PlayPanelHide(UIAnimationType type, bool invokeClose)
        {
            switch (type)
            {
                case UIAnimationType.None:
                    HideInstant(invokeClose);
                    break;

                case UIAnimationType.FadeScale:
                    PlayFadeOut(invokeClose);
                    break;

                case UIAnimationType.SlideLeft:
                    PlaySlideOut(Vector2.left, invokeClose);
                    break;

                case UIAnimationType.SlideRight:
                    PlaySlideOut(Vector2.right, invokeClose);
                    break;

                case UIAnimationType.SlideTop:
                    PlaySlideOut(Vector2.up, invokeClose);
                    break;

                case UIAnimationType.SlideBottom:
                    PlaySlideOut(Vector2.down, invokeClose);
                    break;
            }
        }

        private void HideInstant(bool invokeClose)
        {
            KillPanelTweens();

            RectTransform rect = GetPanelRect();
            if (rect != null)
            {
                CacheShownPositionIfNeeded();
                rect.anchoredPosition = shownAnchoredPos;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);

            IsVisible = false;
            IsAnimating = false;

            if (invokeClose)
                NotifyClosed();
        }

        private void PlayFadeIn()
        {
            if (IsAnimating)
                return;

            KillPanelTweens();

            IsAnimating = true;
            IsVisible = true;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            canvasGroup.alpha = 0f;

            if (panelRoot != null)
                panelRoot.localScale = Vector3.one * 0.85f;

            fadeTween = canvasGroup
                .DOFade(1f, 0.2f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);

            if (panelRoot != null)
            {
                scaleTween = panelRoot
                    .DOScale(1f, 0.2f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        canvasGroup.alpha = 1f;
                        IsAnimating = false;
                    });
            }
            else
            {
                fadeTween.OnComplete(() =>
                {
                    canvasGroup.alpha = 1f;
                    IsAnimating = false;
                });
            }
        }

        private void PlayFadeOut(bool invokeClose)
        {
            if (IsAnimating || !IsVisible)
            {
                if (invokeClose)
                    NotifyClosed();

                return;
            }

            KillPanelTweens();

            IsAnimating = true;
            IsVisible = false;

            fadeTween = canvasGroup
                .DOFade(0f, 0.2f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);

            if (panelRoot != null)
            {
                scaleTween = panelRoot
                    .DOScale(0.9f, 0.2f)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                        IsAnimating = false;

                        if (invokeClose)
                            NotifyClosed();
                    });
            }
            else
            {
                fadeTween.OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    IsAnimating = false;

                    if (invokeClose)
                        NotifyClosed();
                });
            }
        }

        private void PlaySlideIn(Vector2 direction)
        {
            if (IsAnimating)
                return;

            KillPanelTweens();

            RectTransform rect = GetPanelRect();
            if (rect == null)
            {
                ShowInstant();
                return;
            }

            CacheShownPositionIfNeeded();

            IsAnimating = true;
            IsVisible = true;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            Vector2 startPos = GetHiddenPosition(rect, direction);

            rect.anchoredPosition = startPos;
            canvasGroup.alpha = 1f;

            slideTween = rect
                .DOAnchorPos(shownAnchoredPos, 0.35f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    rect.anchoredPosition = shownAnchoredPos;
                    IsAnimating = false;
                });
        }

        private void PlaySlideOut(Vector2 direction, bool invokeClose)
        {
            if (IsAnimating || !IsVisible)
            {
                if (invokeClose)
                    NotifyClosed();

                return;
            }

            KillPanelTweens();

            RectTransform rect = GetPanelRect();
            if (rect == null)
            {
                HideInstant(invokeClose);
                return;
            }

            CacheShownPositionIfNeeded();

            IsAnimating = true;
            IsVisible = false;

            Vector2 endPos = GetHiddenPosition(rect, direction);

            rect.anchoredPosition = shownAnchoredPos;

            slideTween = rect
                .DOAnchorPos(endPos, 0.25f)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    rect.anchoredPosition = shownAnchoredPos;
                    gameObject.SetActive(false);
                    IsAnimating = false;

                    if (invokeClose)
                        NotifyClosed();
                });
        }

        public void BlockMultiClick(float delay = 0.2f)
        {
            SetInteractableControlButton(false);

            DOVirtual.DelayedCall(delay, () =>
            {
                SetInteractableControlButton(true);
            }).SetUpdate(true);
        }

        public void SetInteractableControlButton(bool value)
        {
            foreach (Button button in listButtonControl)
            {
                if (button != null)
                    button.interactable = value;
            }
        }

        private void PlayButtonAnimation()
        {
            if (animatedButtons == null || animatedButtons.Count == 0)
                return;

            KillButtonTweens();

            buttonSequence = DOTween.Sequence().SetUpdate(true);

            for (int i = 0; i < animatedButtons.Count; i++)
            {
                RectTransform button = animatedButtons[i];
                if (button == null)
                    continue;

                CacheButtonTarget(button);

                float delay = buttonAnimDelay + i * buttonAnimInterval;

                switch (buttonAnimationType)
                {
                    case UIButtonAnimationType.SlideLeft:
                    case UIButtonAnimationType.SlideRight:
                        {
                            Vector2 target = buttonTargetPos[button];
                            Vector2 hidden = GetButtonOffscreenPos(button, buttonAnimationType);

                            button.anchoredPosition = hidden;

                            buttonSequence.Insert(
                                delay,
                                button.DOAnchorPos(target, buttonAnimDuration)
                                    .SetEase(Ease.OutCubic)
                            );

                            break;
                        }

                    case UIButtonAnimationType.Fade:
                        {
                            CanvasGroup cg = button.GetComponent<CanvasGroup>();
                            if (cg == null)
                                cg = button.gameObject.AddComponent<CanvasGroup>();

                            cg.alpha = 0f;

                            float targetAlpha = 1f;
                            if (buttonTargetAlpha.TryGetValue(button, out float cachedAlpha))
                                targetAlpha = cachedAlpha;

                            buttonSequence.Insert(
                                delay,
                                cg.DOFade(targetAlpha, buttonAnimDuration)
                            );

                            break;
                        }

                    case UIButtonAnimationType.Scale:
                        {
                            Vector3 targetScale = buttonTargetScale[button];
                            button.localScale = Vector3.zero;

                            buttonSequence.Insert(
                                delay,
                                button.DOScale(targetScale, buttonAnimDuration)
                                    .SetEase(Ease.OutBack)
                            );

                            break;
                        }
                }
            }
        }

        private void PlayButtonReverseAnimation()
        {
            if (animatedButtons == null || animatedButtons.Count == 0)
                return;

            KillButtonTweens();

            buttonSequence = DOTween.Sequence().SetUpdate(true);

            for (int i = 0; i < animatedButtons.Count; i++)
            {
                RectTransform button = animatedButtons[i];
                if (button == null)
                    continue;

                CacheButtonTarget(button);

                float delay = buttonHideDelay + i * buttonHideInterval;

                switch (buttonAnimationType)
                {
                    case UIButtonAnimationType.SlideLeft:
                    case UIButtonAnimationType.SlideRight:
                        {
                            Vector2 hidden = GetButtonOffscreenPos(button, buttonAnimationType);

                            buttonSequence.Insert(
                                delay,
                                button.DOAnchorPos(hidden, buttonHideDuration)
                                    .SetEase(Ease.InCubic)
                            );

                            break;
                        }

                    case UIButtonAnimationType.Fade:
                        {
                            CanvasGroup cg = button.GetComponent<CanvasGroup>();
                            if (cg == null)
                                cg = button.gameObject.AddComponent<CanvasGroup>();

                            buttonSequence.Insert(
                                delay,
                                cg.DOFade(0f, buttonHideDuration)
                            );

                            break;
                        }

                    case UIButtonAnimationType.Scale:
                        {
                            buttonSequence.Insert(
                                delay,
                                button.DOScale(Vector3.zero, buttonHideDuration)
                                    .SetEase(Ease.InQuad)
                            );

                            break;
                        }
                }
            }
        }

        private void CacheButtonTarget(RectTransform button)
        {
            if (button == null)
                return;

            if (!buttonTargetPos.ContainsKey(button))
                buttonTargetPos[button] = button.anchoredPosition;

            if (!buttonTargetScale.ContainsKey(button))
                buttonTargetScale[button] = button.localScale;

            if (buttonAnimationType == UIButtonAnimationType.Fade)
            {
                CanvasGroup cg = button.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = button.gameObject.AddComponent<CanvasGroup>();

                if (!buttonTargetAlpha.ContainsKey(button))
                    buttonTargetAlpha[button] = cg.alpha;
            }
        }

        private Vector2 GetButtonOffscreenPos(RectTransform button, UIButtonAnimationType type)
        {
            RectTransform canvasRect = GetRootCanvasRect();

            if (button == null || canvasRect == null)
                return button != null ? button.anchoredPosition : Vector2.zero;

            Vector2 pos = buttonTargetPos.ContainsKey(button)
                ? buttonTargetPos[button]
                : button.anchoredPosition;

            float canvasHalfWidth = canvasRect.rect.width * 0.5f;
            float buttonWidth = button.rect.width;
            float pivotX = button.pivot.x;

            switch (type)
            {
                case UIButtonAnimationType.SlideLeft:
                    pos.x = -canvasHalfWidth - buttonWidth * (1f - pivotX);
                    break;

                case UIButtonAnimationType.SlideRight:
                    pos.x = canvasHalfWidth + buttonWidth * pivotX;
                    break;
            }

            return pos;
        }

        private RectTransform GetRootCanvasRect()
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            return rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;
        }

        private void KillPanelTweens()
        {
            fadeTween?.Kill();
            scaleTween?.Kill();
            slideTween?.Kill();

            fadeTween = null;
            scaleTween = null;
            slideTween = null;
        }

        private void KillButtonTweens()
        {
            buttonSequence?.Kill();
            buttonSequence = null;

            if (animatedButtons == null)
                return;

            foreach (RectTransform button in animatedButtons)
            {
                if (button == null)
                    continue;

                button.DOKill();

                CanvasGroup cg = button.GetComponent<CanvasGroup>();
                if (cg != null)
                    cg.DOKill();
            }
        }

        private RectTransform GetPanelRect()
        {
            return panelRoot != null ? panelRoot : transform as RectTransform;
        }

        private void CacheShownPositionIfNeeded()
        {
            RectTransform rect = GetPanelRect();
            if (rect == null)
                return;

            if (hasCachedShownPos)
                return;

            shownAnchoredPos = rect.anchoredPosition;
            hasCachedShownPos = true;
        }

        private Vector2 GetHiddenPosition(RectTransform rect, Vector2 direction)
        {
            RectTransform canvasRect = GetRootCanvasRect();

            float offset = direction.x != 0
                ? canvasRect != null ? canvasRect.rect.width : Screen.width
                : canvasRect != null ? canvasRect.rect.height : Screen.height;

            return shownAnchoredPos + direction * offset;
        }
    }
}   