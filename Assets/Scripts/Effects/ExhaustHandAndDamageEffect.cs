using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExhaustHandAndDamageEffect : Effect
{
    [SerializeField] private int damagePerCard;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        return new ExhaustHandAndDamageGA(
            damagePerCard,
            targets,
            caster
        );
    }
}