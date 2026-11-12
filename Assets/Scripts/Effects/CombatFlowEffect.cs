using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CombatFlowEffect : Effect
{
    [SerializeField, Min(0)] private int blockAmount = 7;
    [SerializeField, Min(1)] private int requiredAttacks = 1;
    [SerializeField, Min(0)] private int comboGain = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        AddStatusEffectGA blockGA = new(
            StatusEffectType.ARMOR,
            blockAmount,
            targets ?? new List<CombatantView>()
        );
        blockGA.CardCaster = caster;

        ComboSystem combo = CardSystem.Instance != null ? CardSystem.Instance.Combo : null;

        if (caster != null && combo != null && comboGain > 0 &&
            combo.AttacksThisTurn >= requiredAttacks)
        {
            AddStatusEffectGA gainComboGA = new(
                StatusEffectType.COMBO,
                comboGain,
                new List<CombatantView> { caster }
            );
            gainComboGA.CardCaster = caster;
            blockGA.PostReactions.Add(gainComboGA);
        }

        return blockGA;
    }
}
