using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DamageFromBlockEffect : Effect
{
    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        int blockAmount = 0;

        if (caster != null)
        {
            blockAmount = caster.GetStatusEffectStacks(
                StatusEffectType.ARMOR
            );
        }
        else
        {
            Debug.LogError(
                "DamageFromBlockEffect has no caster."
            );
        }
        return new DealDamageGA(
            blockAmount,
            targets,
            caster
        );
    }
}