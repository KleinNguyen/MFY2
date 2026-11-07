using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FocusedStudyEffect : Effect
{
    [SerializeField] private int drawAmount;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        return new FocusedStudyGA(drawAmount);
    }
}