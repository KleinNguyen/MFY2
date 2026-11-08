using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReclaimingBlowEffect : Effect
{
    [SerializeField] private int damageAmount;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new ReclaimingBlowGA(damageAmount, targets, caster);
    }
}