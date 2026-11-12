using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WhirlwindPayoffEffect : Effect
{
    [SerializeField, Min(0)] private int damagePerCombo = 3;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        SpendComboGA spendComboGA = new(0, true);
        spendComboGA.DamagePerComboSpent = Mathf.Max(0, damagePerCombo);
        spendComboGA.DamageTargets = targets != null
            ? new List<CombatantView>(targets)
            : new List<CombatantView>();
        return spendComboGA;
    }
}
