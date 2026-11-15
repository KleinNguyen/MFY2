using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ToxicExchangeEffect : Effect, ICardPlayCondition
{
    [SerializeField] private int drawAmount = 2;
    [SerializeField] private int blockAmount = 6;

    public bool CanPlay(int otherCardsInHand)
    {
        return otherCardsInHand > 0;
    }

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new ChooseCardToExhaustGA(drawAmount, blockAmount, true);
    }
}