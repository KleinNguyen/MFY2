using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ToxicExchangeEffect : Effect
{
    [SerializeField] private int drawAmount = 2;
    [SerializeField] private int blockAmount = 6;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new ChooseCardToExhaustGA(drawAmount, blockAmount, true);
    }
}
