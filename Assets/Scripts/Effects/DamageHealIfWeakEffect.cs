using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DamageHealIfWeakEffect : Effect
{
    [SerializeField] private int damageAmount;

    [SerializeField] private int healAmount;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        DealDamageGA dealDamageGA = new(
            damageAmount,
            targets,
            caster
        );

        dealDamageGA.HealIfTargetWeak = healAmount;

        return dealDamageGA;
    }
}