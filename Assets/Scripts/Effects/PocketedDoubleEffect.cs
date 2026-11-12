using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PocketedDoubleEffect : Effect
{
    [SerializeField] private int copyAmount = 2;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new PocketedDoubleGA(copyAmount);
    }
}
