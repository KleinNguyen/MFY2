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
        if (settingsPanel == null) return;
        if (ActionSystem.Instance != null && ActionSystem.Instance.IsPerforming) return;
        if (InteractionSystem.Instance != null && InteractionSystem.Instance.PlayerIsDragging) return;
        if (BattleSystem.Instance != null && BattleSystem.Instance.IsPreparingBattle) return;

        if (ManualTargetSystem.Instance != null) ManualTargetSystem.Instance.CancelTargeting();
        if (CardViewHoverSystem.Instance != null) CardViewHoverSystem.Instance.Hide();

        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();
    }
}
