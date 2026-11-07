using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DamageThenStatusEffect : Effect
{
    [SerializeField] private int damageAmount;

    [SerializeField] private StatusEffectType statusEffectType;

    [SerializeField] private int stackCount;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        return new DamageThenStatusGA(
            damageAmount,
            statusEffectType,
            stackCount,
            targets,
            caster
        );
    }
}