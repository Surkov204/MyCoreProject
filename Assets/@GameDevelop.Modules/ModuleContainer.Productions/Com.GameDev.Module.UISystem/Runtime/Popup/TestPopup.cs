using Com.GameDev.Module.UISystem;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class TestPopup : UIBase
{
    [SerializeField] private Button exitButton;
    [SerializeField] private Button openSecondPopupButton;

    private IUIService uiService;

    [Inject]
    private void Construct(IUIService uiService)
    {
        this.uiService = uiService;
    }

    private void OnEnable()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitClicked);
            exitButton.onClick.AddListener(OnExitClicked);
        }

        if (openSecondPopupButton != null)
        {
            openSecondPopupButton.onClick.RemoveListener(OnOpenSecondPopupClicked);
            openSecondPopupButton.onClick.AddListener(OnOpenSecondPopupClicked);
        }
    }

    private void OnDisable()
    {
        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExitClicked);

        if (openSecondPopupButton != null)
            openSecondPopupButton.onClick.RemoveListener(OnOpenSecondPopupClicked);
    }

    private void OnExitClicked()
    {
        BlockMultiClick();

        if (uiService == null)
        {
            Debug.LogError("[TestPopup] IUIService is null.");
            return;
        }

        uiService.Hide<TestPopup>();
    }

    private void OnOpenSecondPopupClicked()
    {
        BlockMultiClick();

        if (uiService == null)
        {
            Debug.LogError("[TestPopup] IUIService is null.");
            return;
        }
        uiService.Show<TestSecondPopup>();
    }
}