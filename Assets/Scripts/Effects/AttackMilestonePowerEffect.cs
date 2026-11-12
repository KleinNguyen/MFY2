using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttackMilestonePowerEffect : Effect
{
    [SerializeField, Min(1)] private int requiredAttacks = 3;
    [SerializeField, Min(0)] private int comboGain = 2;
    [SerializeField, Min(0)] private int drawAmount = 0;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new GainAttackMilestonePowerGA(
            Mathf.Max(1, requiredAttacks),
            Mathf.Max(0, comboGain),
            Mathf.Max(0, drawAmount)
        );
    }
}
