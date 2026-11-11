using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GrandDisappearanceEffect : Effect, ICardPlayCondition
{
    [SerializeField] private int requiredOtherCards = 4;

    public bool CanPlay(int otherCardsInHand)
    {
        return otherCardsInHand >= requiredOtherCards;
    }

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new GrandDisappearanceGA(requiredOtherCards);
    }
}
