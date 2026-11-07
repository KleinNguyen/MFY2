using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HealEffect : Effect
{
    [SerializeField] private int healAmount;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        return new HealGA(
            healAmount,
            targets,
            caster
        );
    }
}