using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DamageThenWeakBonusEffect : Effect
{
    [SerializeField] private int baseDamage = 7;

    [SerializeField] private int bonusDamage = 4;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        return new DamageThenWeakBonusGA(
            baseDamage,
            bonusDamage,
            targets,
            caster
        );
    }
}