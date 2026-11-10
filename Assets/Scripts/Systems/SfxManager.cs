using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum SfxId
{
    ButtonClick,
    CardDrawn,
    CardPlayed,
    CardUpgraded,
    HeroAttack,
    EnemyAttack,
    Rest,
    ItemPurchased,
    HeroAttackRed,
    HeroAttackGreen,
    HeroAttackYellow,
    ArmorGained,
    WeakApplied,
    PoisonApplied,
    DebuffApplied,
    BuffApplied
}

public class SfxManager : MonoBehaviour
{
    [Serializable]
    private class SoundEntry
    {
        public SfxId id;
        public AudioClip clip;
    }

    public static SfxManager Instance { get; private set; }

    [SerializeField] private bool addClickSoundToAllButtons = true;
    [SerializeField] private List<SoundEntry> sounds = new();

    private readonly Dictionary<SfxId, AudioClip> clips = new();
    private readonly HashSet<SfxId> reportedMissingIds = new();
    private readonly HashSet<Button> hookedButtons = new();
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        ApplyVolume();
        LoadClips();

        GameAudioSettings.Changed += ApplyVolume;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshButtons();
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        GameAudioSettings.Changed -= ApplyVolume;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        foreach (Button button in hookedButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayButtonClick);
            }
        }

        hookedButtons.Clear();
        Instance = null;
    }

    public static void PlaySfx(SfxId id)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.Play(id);
    }

    public static void PlayCombatEffectSfx(SfxId id)
    {
        if (Instance == null || Instance.audioSource == null)
        {
            return;
        }

        if (
            !Instance.clips.TryGetValue(id, out AudioClip clip) ||
            clip == null
        )
        {
            Instance.clips.TryGetValue(SfxId.HeroAttackRed, out clip);
        }

        if (clip != null)
        {
            Instance.audioSource.PlayOneShot(clip);
        }
    }

    public static void RefreshSceneButtons()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.RefreshButtons();
    }

    public void Play(SfxId id)
    {
        if (!clips.TryGetValue(id, out AudioClip clip) || clip == null)
        {
            if (reportedMissingIds.Add(id))
            {
                Debug.LogWarning("No AudioClip is configured for SFX ID '" + id + "'.", this);
            }

            return;
        }

        audioSource.PlayOneShot(clip);
    }

    private void PlayButtonClick()
    {
        Play(SfxId.ButtonClick);
    }

    private void ApplyVolume()
    {
        if (audioSource != null)
        {
            audioSource.volume = GameAudioSettings.SoundEffectsVolume;
        }
    }

    private void LoadClips()
    {
        clips.Clear();

        foreach (SoundEntry sound in sounds)
        {
            if (sound == null)
            {
                Debug.LogError("SfxManager contains a null sound entry.", this);
                continue;
            }

            if (sound.clip == null)
            {
                Debug.LogWarning("No AudioClip is assigned for SFX ID '" + sound.id + "'.", this);
                continue;
            }

            if (!clips.TryAdd(sound.id, sound.clip))
            {
                Debug.LogError("Duplicate SFX ID '" + sound.id + "' in SfxManager.", this);
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (!addClickSoundToAllButtons)
        {
            return;
        }

        hookedButtons.RemoveWhere(button => button == null);

        foreach (Button button in FindObjectsOfType<Button>(true))
        {
            if (button != null && hookedButtons.Add(button))
            {
                button.onClick.AddListener(PlayButtonClick);
            }
        }
    }
}
