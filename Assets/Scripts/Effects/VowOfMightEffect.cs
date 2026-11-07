using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VowOfMightEffect : Effect
{
    [SerializeField] private int healthAmount;

    [SerializeField] private int strengthAmount;

    [SerializeField] private int weakAmount;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        return new VowOfMightGA(
            healthAmount,
            strengthAmount,
            weakAmount,
            targets,
            caster
        );
    }
}