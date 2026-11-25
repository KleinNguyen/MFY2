using UnityEngine;

public class EndTurnButtonUI : MonoBehaviour
{
    public void OnClick()
    {
        if (NewRunTutorialUI.IsOpen) return;
        if (SettingsPanelUI.IsOpen) return;
        if (BattleSystem.Instance == null || BattleSystem.Instance.IsBattleOver || BattleSystem.Instance.IsPreparingBattle) return;
        if (CardSystem.Instance != null && CardSystem.Instance.IsViewingPile) return;
        if (ActionSystem.Instance == null || ActionSystem.Instance.IsPerforming) return;

        ActionSystem.Instance.Perform(new EnemyTurnGA());
    }
}