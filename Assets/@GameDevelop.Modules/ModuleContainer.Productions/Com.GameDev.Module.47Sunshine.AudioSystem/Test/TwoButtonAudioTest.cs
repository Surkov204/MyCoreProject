using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class TwoButtonAudioTest : MonoBehaviour
{
    [SerializeField] private Button firstButton;
    [SerializeField] private Button secondButton;

    [SerializeField] private AudioEventRef firstSound;
    [SerializeField] private AudioEventRef secondSound;

    private IAudioService audioService;

    [Inject]
    private void Construct(IAudioService audioService)
    {
        this.audioService = audioService;
    }

    private void Awake()
    {
        firstButton.onClick.AddListener(PlayFirstSound);
        secondButton.onClick.AddListener(PlaySecondSound);
    }

    private void OnDestroy()
    {
        firstButton.onClick.RemoveListener(PlayFirstSound);
        secondButton.onClick.RemoveListener(PlaySecondSound);
    }

    private void PlayFirstSound()
    {
        audioService.Play(firstSound);
    }

    private void PlaySecondSound()
    {
        audioService.Play(secondSound);
    }
}