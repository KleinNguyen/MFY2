using UnityEngine;
using System.Collections.Generic;
public class DealDamageEffect : Effect
{
    [SerializeField] private int damageAmount;
    public override GameAction GetGameAction(List<CombatantView> targets)
    {
        DealDamageGA dealDamageGA = new(damageAmount, targets);
        return dealDamageGA;
    }

}
