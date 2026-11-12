using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CorrosiveSchemeEffect : Effect
{
    [SerializeField, Min(0)] private int baseArmorBreak = 2;
    [SerializeField, Min(0)] private int bonusArmorBreak = 1;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        List<CombatantView> livingTargets = targets == null
            ? new List<CombatantView>()
            : targets.FindAll(target =>
                target != null &&
                target.gameObject.activeInHierarchy &&
                target.CurrentHealth > 0
            );

        List<CombatantView> poisonedTargets = livingTargets.FindAll(
            target => target.GetStatusEffectStacks(StatusEffectType.POISON) > 0
        );

        AddStatusEffectGA action = new(
            StatusEffectType.ARMOR_BREAK,
            baseArmorBreak,
            livingTargets
        );

        if (bonusArmorBreak > 0 && poisonedTargets.Count > 0)
        {
            AddStatusEffectGA bonusAction = new(
                StatusEffectType.ARMOR_BREAK,
                bonusArmorBreak,
                poisonedTargets
            );

            bonusAction.CardCaster = caster;
            action.PostReactions.Add(bonusAction);
        }

        return action;
    }
}