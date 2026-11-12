using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FlowingGuardEffect : Effect
{
    [SerializeField, Min(0)] private int blockAmount = 9;
    [SerializeField, Min(0)] private int requiredCombo = 3;
    [SerializeField, Min(0)] private int drawAmount = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        AddStatusEffectGA blockGA = new(
            StatusEffectType.ARMOR,
            blockAmount,
            targets ?? new List<CombatantView>()
        );
        blockGA.CardCaster = caster;

        if (caster != null && drawAmount > 0 &&
            caster.GetStatusEffectStacks(StatusEffectType.COMBO) >= requiredCombo)
        {
            DrawCardsGA drawCardsGA = new(drawAmount);
            drawCardsGA.CardCaster = caster;
            blockGA.PostReactions.Add(drawCardsGA);
        }

        return blockGA;
    }
}
