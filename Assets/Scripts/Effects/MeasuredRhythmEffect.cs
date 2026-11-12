using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MeasuredRhythmEffect : Effect
{
    [SerializeField] private int baseDamage = 5;
    [SerializeField] private int damagePerCombo = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        int combo = caster != null
            ? Mathf.Max(0, caster.GetStatusEffectStacks(StatusEffectType.COMBO))
            : 0;

        int damage = baseDamage + combo * damagePerCombo;
        return new DealDamageGA(damage, targets ?? new List<CombatantView>(), caster);
    }
}
