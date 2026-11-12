using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ComboBreakerEffect : Effect
{
    [SerializeField] private int baseDamage = 7;
    [SerializeField] private int requiredCombo = 3;
    [SerializeField] private int bonusDamage = 5;

    public override GameAction GetGameAction(
        List<CombatantView> targets, CombatantView caster)
    {
        int combo = caster != null
            ? caster.GetStatusEffectStacks(StatusEffectType.COMBO)
            : 0;

        int damage = baseDamage;
        if (combo >= requiredCombo)
            damage += bonusDamage;

        return new DealDamageGA(damage, targets, caster);
    }
}