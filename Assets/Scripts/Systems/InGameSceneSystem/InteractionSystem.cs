using UnityEngine;

public class InteractionSystem : Singleton<InteractionSystem>
{
    public bool PlayerIsDragging { get; set; }

    public bool PlayerCanInteract()
    {
        if (SettingsPanelUI.IsOpen) return false;
        if (CardSystem.Instance != null && CardSystem.Instance.IsViewingPile) return false;
        if (BattleSystem.Instance == null || BattleSystem.Instance.IsBattleOver || BattleSystem.Instance.IsPreparingBattle) return false;
        if (!ActionSystem.Instance.IsPerforming) return true;
        return false;
    }

    public bool PlayerCanHover()
    {
        if (SettingsPanelUI.IsOpen) return false;
        if (CardSystem.Instance != null && CardSystem.Instance.IsViewingPile) return false;
        if (BattleSystem.Instance == null || BattleSystem.Instance.IsBattleOver || BattleSystem.Instance.IsPreparingBattle) return false;
        if (PlayerIsDragging) return false;
        return true;
    }
}