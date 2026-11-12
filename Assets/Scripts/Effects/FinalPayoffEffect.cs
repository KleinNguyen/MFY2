using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FinalPayoffEffect : Effect
{
    [SerializeField, Min(0)] private int baseDamage = 10;
    [SerializeField, Min(0)] private int damagePerCombo = 4;
    [SerializeField, Min(0)] private int comboOnKill = 2;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        SpendComboGA spendComboGA = new(0, true);

        spendComboGA.BaseDamageAfterSpend = Mathf.Max(0, baseDamage);
        spendComboGA.DamagePerComboSpent = Mathf.Max(0, damagePerCombo);
        spendComboGA.ComboOnKill = Mathf.Max(0, comboOnKill);

        spendComboGA.DamageTargets = targets != null
            ? new List<CombatantView>(targets)
            : new List<CombatantView>();

        return spendComboGA;
    }
}