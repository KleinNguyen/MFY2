using UnityEngine;

public class InteractionSystem : Singleton<InteractionSystem>
{
    public bool PlayerIsDragging { get; set; }

    public bool PlayerCanInteract()
    {
        // Modal UI and active battle actions temporarily own player input.
        if (NewRunTutorialUI.IsOpen) return false;
        if (ConfirmationDialogUI.IsOpen) return false;
        if (SettingsPanelUI.IsOpen) return false;
        if (CardSystem.Instance != null && CardSystem.Instance.IsViewingPile) return false;
        if (BattleSystem.Instance == null || BattleSystem.Instance.IsBattleOver ||
            BattleSystem.Instance.IsPreparingBattle)
            return false;
        if (!ActionSystem.Instance.IsPerforming) return true;

        return false;
    }

    public bool PlayerCanHover()
    {
        // Hover effects are also disabled while the player is dragging a card.
        if (NewRunTutorialUI.IsOpen) return false;
        if (ConfirmationDialogUI.IsOpen) return false;
        if (SettingsPanelUI.IsOpen) return false;
        if (CardSystem.Instance != null && CardSystem.Instance.IsViewingPile) return false;
        if (ActionSystem.Instance != null && ActionSystem.Instance.IsPerforming) return false;
        if (BattleSystem.Instance == null || BattleSystem.Instance.IsBattleOver ||
            BattleSystem.Instance.IsPreparingBattle)
            return false;
        if (PlayerIsDragging) return false;

        return true;
    }
}