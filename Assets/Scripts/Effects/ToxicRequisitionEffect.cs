using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ToxicRequisitionEffect : Effect
{
    [SerializeField] private int maxDiscard = 3;
    [SerializeField] private int damagePerCard = 5;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new ToxicRequisitionGA(maxDiscard, damagePerCard, targets, caster);
    }
}
