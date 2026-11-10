using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BreachAndVenomEffect : Effect
{
    [SerializeField, Min(0)] private int damageAmount = 7;
    [SerializeField, Min(0)] private int armorBreakStacks = 2;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        CombatantView target = targets != null && targets.Count > 0
            ? targets[0]
            : null;

        List<CombatantView> validTargets = new();

        if (target != null &&
            target.gameObject.activeInHierarchy &&
            target.CurrentHealth > 0)
        {
            validTargets.Add(target);

            if (target.GetStatusEffectStacks(StatusEffectType.POISON) > 0)
            {
                return new DamageThenStatusGA(
                    damageAmount,
                    StatusEffectType.ARMOR_BREAK,
                    armorBreakStacks,
                    validTargets,
                    caster
                );
            }
        }

        return new DealDamageGA(damageAmount, validTargets, caster);
    }
}