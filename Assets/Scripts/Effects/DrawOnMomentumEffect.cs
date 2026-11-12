using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DrawOnMomentumEffect : Effect
{
    [SerializeField, Min(0)] private int maxComboSpend = 2;
    [SerializeField, Min(0)] private int drawPerCombo = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        SpendComboGA spendComboGA = new(Mathf.Max(0, maxComboSpend));
        spendComboGA.DrawPerComboSpent = Mathf.Max(0, drawPerCombo);
        return spendComboGA;
    }
}
