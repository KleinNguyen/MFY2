using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExposedFlurryEffect : Effect
{
    [SerializeField, Min(1)] private int hitCount = 4;
    [SerializeField, Min(0)] private int damagePerHit = 4;
    [SerializeField, Min(0)] private int bonusDamageIfExposed = 2;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        List<CombatantView> validTargets = new();

        if (targets != null)
        {
            foreach (CombatantView target in targets)
            {
                if (target != null && target.CurrentHealth > 0 && target.gameObject.activeInHierarchy)
                {
                    validTargets.Add(target);
                }
            }
        }

        DealDamageGA firstHit = CreateHit(validTargets, caster);

        for (int i = 1; i < Mathf.Max(1, hitCount); i++)
        {
            firstHit.PostReactions.Add(CreateHit(validTargets, caster));
        }

        return firstHit;
    }

    private DealDamageGA CreateHit(List<CombatantView> targets, CombatantView caster)
    {
        DealDamageGA hit = new(damagePerHit, targets, caster);
        hit.CardCaster = caster;

        foreach (CombatantView target in targets)
        {
            if (target.GetStatusEffectStacks(StatusEffectType.EXPOSED) > 0)
            {
                hit.SetBonusDamageForTarget(target, bonusDamageIfExposed);
            }
        }

        return hit;
    }
}
