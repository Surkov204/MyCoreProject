using UnityEngine;
using Zenject;

public class FireAudioEmitter : MonoBehaviour
{
    [SerializeField] private AudioEventRef fireLoopSound;
    [SerializeField] private bool playOnStart = true;

    private IAudioService audioService;
    private AudioHandle handle;

    [Inject]
    private void Construct(IAudioService audioService)
    {
        this.audioService = audioService;
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        Stop();
    }

    private void Play()
    {
        if (audioService == null)
        {
            Debug.LogWarning("[FireAudioEmitter] AudioService is null.", this);
            return;
        }

        if (!fireLoopSound.IsValid)
        {
            Debug.LogWarning("[FireAudioEmitter] Fire sound is invalid.", this);
            return;
        }

        handle = audioService.PlayAttached(fireLoopSound, transform);
    }

    private void Stop()
    {
        if (audioService == null)
            return;

        if (!handle.IsValid)
            return;

        audioService.Stop(handle);
        handle = AudioHandle.Invalid;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 8f);
    }
#endif
}