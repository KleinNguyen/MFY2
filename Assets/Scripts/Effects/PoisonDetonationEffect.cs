using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PoisonDetonationEffect : Effect
{
    [SerializeField, Min(0)] private int baseDamage = 10;
    [SerializeField, Min(1)] private int poisonDivisor = 2;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        List<CombatantView> validTargets = targets == null
            ? new List<CombatantView>()
            : targets.FindAll(target =>
                target != null &&
                target.gameObject.activeInHierarchy &&
                target.CurrentHealth > 0
            );

        DealDamageGA action = new(baseDamage, validTargets, caster);
        int divisor = Mathf.Max(1, poisonDivisor);

        foreach (CombatantView target in validTargets)
        {
            int poisonStacks = Mathf.Max(
                0,
                target.GetStatusEffectStacks(StatusEffectType.POISON)
            );

            action.SetBonusDamageForTarget(
                target,
                poisonStacks / divisor
            );
        }

        return action;
    }
}