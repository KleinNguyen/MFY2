using UnityEngine;
using UnityEngine.UI;

public class SettingsButtonUI : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;

    private void Awake()
    {
        if (settingsPanel == null)
        {
            Debug.LogError("Settings panel is not assigned.", this);
            return;
        }

        GameAudioSettings.ApplySoundEffectsVolume();
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }
}
