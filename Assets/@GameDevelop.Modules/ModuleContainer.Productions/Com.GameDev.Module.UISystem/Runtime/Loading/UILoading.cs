using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Com.GameDev.Module.UISystem
{
    public class UILoading : UIBase
    {
        [SerializeField] private Image loadingImage;
        [SerializeField] private float tweenDuration = 0.25f;
        [SerializeField] private float minimumLoadingTime = 0.5f;

        public async UniTask RunProgressRoutine(
            Func<Action<float>, UniTask> loadingOperation,
            Action<float> onProgressChanged)
        {
            if (loadingImage != null)
                loadingImage.fillAmount = 0f;

            UniTask loadingTask = loadingOperation.Invoke(progress =>
            {
                float value = Mathf.Clamp01(progress);
                onProgressChanged?.Invoke(value);
            });
            UniTask minDurationTask = UniTask.Delay(TimeSpan.FromSeconds(minimumLoadingTime));
            await UniTask.WhenAll(loadingTask, minDurationTask);

            onProgressChanged?.Invoke(1f);
        }

        public void UpdateProgressBarSmoothly(float progress)
        {
            if (loadingImage == null)
                return;

            loadingImage
                .DOFillAmount(Mathf.Clamp01(progress), tweenDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (loadingImage != null)
                DOTween.Kill(loadingImage);
        }
    }
}