using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DamageIfPoisonedEffect : Effect
{
    [SerializeField, Min(0)] private int baseDamage = 7;
    [SerializeField, Min(0)] private int bonusDamage = 4;
    [SerializeField, Min(1)] private int requiredPoisonStacks = 1;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        List<CombatantView> validTargets = new();
        int damage = baseDamage;

        CombatantView target = targets != null && targets.Count > 0
            ? targets[0]
            : null;

        if (target != null &&
            target.gameObject.activeInHierarchy &&
            target.CurrentHealth > 0)
        {
            validTargets.Add(target);

            int poisonStacks = target.GetStatusEffectStacks(
                StatusEffectType.POISON
            );

            if (poisonStacks >= requiredPoisonStacks)
            {
                damage += bonusDamage;
            }
        }

        return new DealDamageGA(damage, validTargets, caster);
    }
}