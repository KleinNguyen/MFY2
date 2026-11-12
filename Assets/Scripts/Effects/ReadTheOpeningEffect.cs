using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReadTheOpeningEffect : Effect
{
    [SerializeField, Min(0)] private int comboGain = 2;
    [SerializeField, Min(0)] private int drawAmount = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        List<CombatantView> comboTargets = new();

        if (caster != null)
        {
            comboTargets.Add(caster);
        }

        AddStatusEffectGA gainComboGA = new(StatusEffectType.COMBO, comboGain, comboTargets);
        gainComboGA.CardCaster = caster;

        CombatantView target = targets != null && targets.Count > 0 ? targets[0] : null;

        if (caster != null && target != null && target.CurrentHealth > 0 &&
            target.gameObject.activeInHierarchy && drawAmount > 0 &&
            target.GetStatusEffectStacks(StatusEffectType.EXPOSED) > 0)
        {
            DrawCardsGA drawCardsGA = new(drawAmount);
            drawCardsGA.CardCaster = caster;
            gainComboGA.PostReactions.Add(drawCardsGA);
        }

        return gainComboGA;
    }
}
