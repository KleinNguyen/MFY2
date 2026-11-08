using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DiscardCardsEffect : Effect
{
    [SerializeField] private int discardAmount;
    [SerializeField] private int drawAfterDiscard;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new DiscardCardsGA(discardAmount, drawAfterDiscard);
    }
}