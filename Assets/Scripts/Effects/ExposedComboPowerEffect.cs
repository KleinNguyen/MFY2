using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExposedComboPowerEffect : Effect
{
    [SerializeField, Min(0)] private int comboGain = 2;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new GainExposedComboPowerGA(Mathf.Max(0, comboGain));
    }
}
