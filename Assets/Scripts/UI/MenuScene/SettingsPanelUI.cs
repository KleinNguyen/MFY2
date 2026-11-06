using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundEffectsSlider;
    [SerializeField] private TMP_Text masterValue;
    [SerializeField] private TMP_Text musicValue;
    [SerializeField] private TMP_Text soundEffectsValue;

    private void Awake()
    {
        if (closeButton == null || masterSlider == null || musicSlider == null ||
            soundEffectsSlider == null || masterValue == null || musicValue == null ||
            soundEffectsValue == null)
        {
            Debug.LogError("Settings panel UI references are not fully assigned.", this);
            return;
        }

        closeButton.onClick.AddListener(Close);

        masterSlider.SetValueWithoutNotify(GameAudioSettings.MasterVolume);
        musicSlider.SetValueWithoutNotify(GameAudioSettings.MusicVolume);
        soundEffectsSlider.SetValueWithoutNotify(GameAudioSettings.SoundEffectsVolume);
        UpdateValue(masterSlider.value, masterValue);
        UpdateValue(musicSlider.value, musicValue);
        UpdateValue(soundEffectsSlider.value, soundEffectsValue);

        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        soundEffectsSlider.onValueChanged.AddListener(SetSoundEffectsVolume);
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private void SetMasterVolume(float value)
    {
        GameAudioSettings.SetMasterVolume(value);
        UpdateValue(value, masterValue);
    }

    private void SetMusicVolume(float value)
    {
        GameAudioSettings.SetMusicVolume(value);
        UpdateValue(value, musicValue);
    }

    private void SetSoundEffectsVolume(float value)
    {
        GameAudioSettings.SetSoundEffectsVolume(value);
        UpdateValue(value, soundEffectsValue);
    }

    private void UpdateValue(float value, TMP_Text label)
    {
        label.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
