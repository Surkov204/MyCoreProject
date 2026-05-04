using UnityEngine;

public class AudioPlayerRouter : MonoBehaviour
{
    [SerializeField] private AudioPlayer musicPlayer;
    [SerializeField] private AudioPlayer uiPlayer;
    [SerializeField] private AudioPlayer sfxPlayer;
    [SerializeField] private AudioPlayer ambiencePlayer;
    [SerializeField] private AudioPlayer voicePlayer;

    public AudioPlayer GetPlayer(AudioType type)
    {
        return type switch
        {
            AudioType.Music => musicPlayer,
            AudioType.UI => uiPlayer,
            AudioType.SFX => sfxPlayer,
            AudioType.Ambience => ambiencePlayer,
            AudioType.VoiceOver => voicePlayer,
            _ => sfxPlayer
        };
    }

    public void StopAll()
    {
        musicPlayer?.StopAll();
        uiPlayer?.StopAll();
        sfxPlayer?.StopAll();
        ambiencePlayer?.StopAll();
        voicePlayer?.StopAll();
    }
}