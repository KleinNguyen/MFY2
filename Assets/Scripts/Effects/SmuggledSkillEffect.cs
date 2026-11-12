using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SmuggledSkillEffect : Effect
{
    [SerializeField] private int blockAmount = 7;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        AddStatusEffectGA blockGA = new(StatusEffectType.ARMOR, blockAmount, targets);
        blockGA.PostReactions.Add(new ChooseCardToGrantSlyGA());
        return blockGA;
    }
}
