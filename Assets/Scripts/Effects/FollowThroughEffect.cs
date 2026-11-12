using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FollowThroughEffect : Effect
{
    [SerializeField] private int damageAmount = 6;
    [SerializeField] private int requiredCombo = 3;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        List<CombatantView> damageTargets = targets ?? new List<CombatantView>();
        DealDamageGA damageGA = new(damageAmount, damageTargets, caster);

        int combo = caster != null
            ? Mathf.Max(0, caster.GetStatusEffectStacks(StatusEffectType.COMBO))
            : 0;

        if (combo >= requiredCombo)
        {
            DealDamageGA followUpGA = new(damageAmount, damageTargets, caster);
            followUpGA.CardCaster = caster;
            damageGA.PostReactions.Add(followUpGA);
        }

        return damageGA;
    }
}
