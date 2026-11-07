using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockThenDamageEffect : Effect
{
    [SerializeField] private int blockAmount;

    [SerializeField] private int damageAmount;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        return new BlockThenDamageGA(
            blockAmount,
            damageAmount,
            targets,
            caster
        );
    }
}