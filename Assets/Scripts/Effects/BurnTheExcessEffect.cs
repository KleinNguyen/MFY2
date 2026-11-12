using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BurnTheExcessEffect : Effect
{
    [SerializeField, Min(0)] private int maxExhaust = 2;
    [SerializeField, Min(0)] private int comboPerCard = 2;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        return new ExhaustCardsForComboGA(
            Mathf.Max(0, maxExhaust),
            Mathf.Max(0, comboPerCard)
        );
    }
}