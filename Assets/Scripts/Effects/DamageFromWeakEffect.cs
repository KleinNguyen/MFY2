using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DamageFromWeakEffect : Effect
{
    [SerializeField] private int baseDamage = 6;

    [SerializeField] private int damagePerWeak = 3;

    [SerializeField] private int maxBonusDamage = 12;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        int weakStacks = 0;
        if (targets != null &&
            targets.Count > 0 &&
            targets[0] != null)
        {
            weakStacks = targets[0].GetStatusEffectStacks(
                StatusEffectType.WEAK
            );
        }

        int bonusDamage = Mathf.Min(
            weakStacks * damagePerWeak,
            maxBonusDamage
        );

        int totalDamage = baseDamage + bonusDamage;
        List<CombatantView> safeTargets =
            targets ?? new List<CombatantView>();

        return new DealDamageGA(
            totalDamage,
            safeTargets,
            caster
        );
    }
}