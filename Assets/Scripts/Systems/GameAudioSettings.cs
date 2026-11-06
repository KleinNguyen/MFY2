using UnityEngine;

public static class GameAudioSettings
{
    private const string MasterVolumeKey = "Audio.MasterVolume";
    private const string MusicVolumeKey = "Audio.MusicVolume";
    private const string SoundEffectsVolumeKey = "Audio.SoundEffectsVolume";

    public static float MasterVolume { get; private set; }
    public static float MusicVolume { get; private set; }
    public static float SoundEffectsVolume { get; private set; }

    public static event System.Action Changed;

    static GameAudioSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SoundEffectsVolume = PlayerPrefs.GetFloat(SoundEffectsVolumeKey, 1f);
        AudioListener.volume = MasterVolume;
    }

    public static void SetMasterVolume(float volume)
    {
        MasterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
        AudioListener.volume = MasterVolume;
        Changed?.Invoke();
    }

    public static void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        Changed?.Invoke();
    }

    public static void SetSoundEffectsVolume(float volume)
    {
        SoundEffectsVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SoundEffectsVolumeKey, SoundEffectsVolume);
        ApplySoundEffectsVolume();
        Changed?.Invoke();
    }

    public static void ApplySoundEffectsVolume()
    {
        foreach (AudioSource source in Object.FindObjectsOfType<AudioSource>())
        {
            if (source != null && source.GetComponent<SceneMusicPlayer>() == null)
            {
                source.volume = SoundEffectsVolume;
            }
        }
    }
}
