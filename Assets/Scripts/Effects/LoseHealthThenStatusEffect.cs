using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LoseHealthThenStatusEffect : Effect
{
    [SerializeField] private int healthAmount;

    [SerializeField] private StatusEffectType statusEffectType;

    [SerializeField] private int stackCount;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        return new LoseHealthThenStatusGA(
            healthAmount,
            statusEffectType,
            stackCount,
            targets
        );
    }
}