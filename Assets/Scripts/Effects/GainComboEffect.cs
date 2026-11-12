using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GainComboEffect : Effect
{
    [SerializeField] private int amount = 2;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        List<CombatantView> comboTargets = new();
        if (caster != null) comboTargets.Add(caster);
        return new AddStatusEffectGA(StatusEffectType.COMBO, Mathf.Max(0, amount), comboTargets);
    }
}
