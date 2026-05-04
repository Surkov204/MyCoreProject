using Com.GameDev.Module.UISystem;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class TestSecondPopup : UIBase
{
    [SerializeField] private Button exitButton;

    private IUIService uiService;

    [Inject]
    private void Construct(IUIService uiService)
    {
        this.uiService = uiService;
    }

    private void OnEnable()
    {
        if (exitButton == null)
            return;

        exitButton.onClick.RemoveListener(OnExitClicked);
        exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnDisable()
    {
        if (exitButton == null)
            return;

        exitButton.onClick.RemoveListener(OnExitClicked);
    }

    private void OnExitClicked()
    {
        BlockMultiClick();

        if (uiService == null)
        {
            Debug.LogError("[TestSecondPopup] IUIService is null.");
            return;
        }

        uiService.Hide<TestSecondPopup>();
    }
}