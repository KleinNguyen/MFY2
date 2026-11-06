using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMusicPlayer : MonoBehaviour
{
    private static SceneMusicPlayer instance;

    private AudioSource audioSource;
    private SceneMusicSettings settings;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateMusicPlayer()
    {
        if (instance == null)
        {
            new GameObject(nameof(SceneMusicPlayer)).AddComponent<SceneMusicPlayer>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = GameAudioSettings.MusicVolume;
        GameAudioSettings.Changed += ApplyMusicVolume;

        settings = Resources.Load<SceneMusicSettings>("SceneMusicSettings");
        if (settings == null)
        {
            Debug.LogError(
                "SceneMusicSettings was not found. Create it under Assets/Resources " +
                "and assign music for each scene."
            );
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isLoaded)
        {
            PlaySceneMusic(activeScene);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            GameAudioSettings.Changed -= ApplyMusicVolume;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void ApplyMusicVolume()
    {
        if (audioSource != null)
        {
            audioSource.volume = GameAudioSettings.MusicVolume;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneMusic(scene);
    }

    private void PlaySceneMusic(Scene scene)
    {
        if (!settings.TryGetTrack(scene.name, out SceneMusicTrack track))
        {
            StopMusic();
            Debug.LogWarning("No music track is configured for scene '" + scene.name + "'.");
            return;
        }

        if (track.ContinueCurrentTrack)
        {
            if (audioSource.clip != null)
            {
                audioSource.loop = track.Loop;
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }

                return;
            }

            if (track.Clip == null)
            {
                Debug.LogWarning(
                    "Scene '" + scene.name + "' is set to continue music, but no track is " +
                    "playing and no fallback clip is assigned."
                );
                return;
            }

            PlayTrack(track);
            return;
        }

        if (track.Clip == null)
        {
            StopMusic();
            Debug.LogWarning("No music clip is assigned for scene '" + scene.name + "'.");
            return;
        }

        PlayTrack(track);
    }

    private void PlayTrack(SceneMusicTrack track)
    {
        audioSource.loop = track.Loop;
        if (audioSource.clip == track.Clip)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }

            return;
        }

        audioSource.Stop();
        audioSource.clip = track.Clip;
        audioSource.Play();
    }

    private void StopMusic()
    {
        audioSource.Stop();
        audioSource.clip = null;
    }
}
