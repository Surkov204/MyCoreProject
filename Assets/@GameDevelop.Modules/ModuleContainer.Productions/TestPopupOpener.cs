using Com.GameDev.Module.UISystem;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class TestPopupOpener : MonoBehaviour
{
    [SerializeField] private Button openPopupButton;

    private IUIService uiService;

    [Inject]
    private void Construct(IUIService uiService)
    {
        this.uiService = uiService;
    }

    private void Start()
    {
        if (openPopupButton != null)
        {
            openPopupButton.onClick.RemoveListener(OnOpenPopupClicked);
            openPopupButton.onClick.AddListener(OnOpenPopupClicked);
        }
    }

    private void OnOpenPopupClicked()
    {
        uiService.Show<TestPopup>();
    }

    private void OnDestroy()
    {
        if (openPopupButton != null)
        {
            openPopupButton.onClick.RemoveListener(OnOpenPopupClicked);
        }
    }
}