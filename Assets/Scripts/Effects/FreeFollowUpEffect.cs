using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FreeFollowUpEffect : Effect
{
    [SerializeField] private int damageAmount = 12;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        DealDamageGA damageGA = new(damageAmount, targets, caster);
        damageGA.PostReactions.Add(new NextCardFreeGA());
        return damageGA;
    }
}
