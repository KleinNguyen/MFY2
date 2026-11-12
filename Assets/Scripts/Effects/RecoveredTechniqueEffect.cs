using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RecoveredTechniqueEffect : Effect
{
    [SerializeField] private int additionalManaCost = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new RecoveredTechniqueGA(additionalManaCost);
    }
}
