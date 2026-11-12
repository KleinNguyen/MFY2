using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MarkedOpeningEffect : Effect
{
    [SerializeField, Min(0)] private int damageAmount = 7;
    [SerializeField, Min(0)] private int comboGain = 2;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        List<CombatantView> validTargets = new();
        CombatantView target = targets != null && targets.Count > 0 ? targets[0] : null;
        bool targetHasExposed = false;

        if (target != null && target.CurrentHealth > 0 && target.gameObject.activeInHierarchy)
        {
            validTargets.Add(target);
            targetHasExposed = target.GetStatusEffectStacks(StatusEffectType.EXPOSED) > 0;
        }

        DealDamageGA damageGA = new(damageAmount, validTargets, caster);

        if (targetHasExposed && caster != null && comboGain > 0)
        {
            AddStatusEffectGA gainComboGA = new(
                StatusEffectType.COMBO,
                comboGain,
                new List<CombatantView> { caster }
            );
            gainComboGA.CardCaster = caster;
            damageGA.PostReactions.Add(gainComboGA);
        }

        return damageGA;
    }
}
