using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GainManaEffect : Effect
{
    [SerializeField] private int manaAmount;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        return new GainManaGA(manaAmount);
    }
}