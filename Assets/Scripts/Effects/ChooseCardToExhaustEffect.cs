using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChooseCardToExhaustEffect : Effect, ICardPlayCondition
{
    [SerializeField] private int drawAmount;

    public bool CanPlay(int otherCardsInHand)
    {
        return otherCardsInHand > 0;
    }

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new ChooseCardToExhaustGA(drawAmount, 0, true);
    }
}