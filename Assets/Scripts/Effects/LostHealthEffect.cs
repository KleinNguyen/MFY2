using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LoseHealthEffect : Effect
{
    [SerializeField] private int healthAmount;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        return new LoseHealthGA(healthAmount);
    }
}