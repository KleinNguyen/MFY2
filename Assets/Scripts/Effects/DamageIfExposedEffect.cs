using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DamageIfExposedEffect : Effect
{
    [SerializeField, Min(0)] private int baseDamage = 10;
    [SerializeField, Min(0)] private int bonusDamage = 5;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        List<CombatantView> validTargets = new();

        if (targets != null)
        {
            foreach (CombatantView target in targets)
            {
                if (target != null && target.CurrentHealth > 0 && target.gameObject.activeInHierarchy)
                {
                    validTargets.Add(target);
                }
            }
        }

        DealDamageGA damageGA = new(baseDamage, validTargets, caster);

        foreach (CombatantView target in validTargets)
        {
            if (target.GetStatusEffectStacks(StatusEffectType.EXPOSED) > 0)
            {
                damageGA.SetBonusDamageForTarget(target, bonusDamage);
            }
        }

        return damageGA;
    }
}
