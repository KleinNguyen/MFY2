using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Slider masterSlider, musicSlider, soundEffectsSlider;
    [SerializeField] private TMP_Text masterValue, musicValue, soundEffectsValue;
    [SerializeField] private Button restartButton, leaveButton;
    [SerializeField] private TMP_Text messageText;

    public static bool IsOpen { get; private set; }

    private bool Busy => ActionSystem.Instance != null && ActionSystem.Instance.IsPerforming;
    private bool InBattle => SceneManager.GetActiveScene().name == "InGameScene";
    private bool BattleReady =>
        InBattle &&
        BattleSystem.Instance != null &&
        !BattleSystem.Instance.IsPreparingBattle &&
        !BattleSystem.Instance.IsBattleOver;
    private bool CanRestartHere => BattleReady;

    private void Awake()
    {
        if (closeButton == null || masterSlider == null ||
            musicSlider == null || soundEffectsSlider == null ||
            masterValue == null || musicValue == null || soundEffectsValue == null)
        {
            Debug.LogError("Settings panel UI references are not fully assigned.", this);
            return;
        }

        closeButton.onClick.AddListener(Close);
        if (restartButton != null) restartButton.onClick.AddListener(RestartBattle);
        if (leaveButton != null) leaveButton.onClick.AddListener(ReturnToMenu);

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

    private void OnEnable()
    {
        IsOpen = true;
        SetMessage("");
        UpdateButtons();
    }

    private void OnDisable() => IsOpen = false;
    private void Update() => UpdateButtons();

    private void UpdateButtons()
    {
        RunSystem run = RunSystem.Instance;

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(InBattle);
            restartButton.interactable = !Busy && CanRestartHere && run != null && run.CanRestartBattle;
        }

        if (leaveButton != null)
        {
            leaveButton.interactable = !Busy &&
                (!InBattle ||
                 BattleSystem.Instance != null &&
                 !BattleSystem.Instance.IsPreparingBattle);
        }
    }

    private void ExecuteRestartBattle()
    {
        if (Busy || !CanRestartHere || RunSystem.Instance == null) return;

        if (!RunSystem.Instance.RestartBattle())
        {
            SetMessage("Could not restart this battle. Check the save data.");
            return;
        }

        Close();
        SceneManager.LoadScene("InGameScene");
    }

    private void ExecuteReturnToMenu()
    {
        if (Busy) return;

        RunSystem run = RunSystem.Instance;
        if (run != null && !run.IsRunFinished)
        {
            if (!InBattle && run.SelectedMapPointId >= 0)
            {
                SetMessage("Finish Shop or Rest before leaving.");
                return;
            }

            if (InBattle && !BattleReady)
            {
                SetMessage("Finish rewards and return to the map first.");
                return;
            }

            if (!run.SaveGame())
            {
                SetMessage("Could not save. Please try again.");
                return;
            }
        }

        if (run != null) run.PauseRunTimer();
        Close();
        SceneManager.LoadScene("MenuScene");
    }
    // Confirm restart and menu navigation.
    public void RestartBattle()
    {
        RunSystem run = RunSystem.Instance;
        if (Busy || !CanRestartHere || run == null || run.IsRunFinished || !run.CanRestartBattle) return;

        ConfirmationDialogUI.Ask(this, "Do you want to restart this battle?", ExecuteRestartBattle,
            () => RunSystem.Instance == run && !run.IsRunFinished && CanRestartHere && run.CanRestartBattle);
    }

    public void ReturnToMenu()
    {
        if (Busy) return;

        RunSystem run = RunSystem.Instance;
        bool activeRun = run != null && !run.IsRunFinished;

        if (activeRun && !InBattle && run.SelectedMapPointId >= 0)
        {
            SetMessage("Finish Shop or Rest before leaving.");
            return;
        }

        if (activeRun && InBattle && !BattleReady)
        {
            SetMessage("Finish rewards and return to the map first.");
            return;
        }

        string question = activeRun
            ? "Do you want to save and return to the main menu?"
            : "Do you want to return to the main menu?";
        if (activeRun && InBattle) question += "\nContinuing will restart this battle.";

        ConfirmationDialogUI.Ask(this, question, ExecuteReturnToMenu,
            () => RunSystem.Instance == run);
    }

    private void Close() => gameObject.SetActive(false);
    private void SetMessage(string message) { if (messageText != null) messageText.text = message; }
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
    private void UpdateValue(float value, TMP_Text label) => label.text = Mathf.RoundToInt(value * 100f) + "%";

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (restartButton != null) restartButton.onClick.RemoveListener(RestartBattle);
        if (leaveButton != null) leaveButton.onClick.RemoveListener(ReturnToMenu);
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (soundEffectsSlider != null) soundEffectsSlider.onValueChanged.RemoveListener(SetSoundEffectsVolume);
    }
}