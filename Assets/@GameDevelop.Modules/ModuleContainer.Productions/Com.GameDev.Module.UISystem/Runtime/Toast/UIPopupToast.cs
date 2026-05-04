using TMPro;
using UnityEngine;

namespace Com.GameDev.Module.UISystem
{
    public class UIPopupToast : MonoBehaviour
    {
        [field: SerializeField] public CanvasGroup CanvasGroup { get; private set; }
        [field: SerializeField] public TextMeshProUGUI Text { get; private set; }
        [field: SerializeField] public RectTransform RectTransform { get; private set; }
    }
}