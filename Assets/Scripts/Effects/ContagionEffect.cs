using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ContagionEffect : Effect
{
    [SerializeField, Min(1)] private int poisonDivisor = 2;

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

        CombatantView source = null;
        int highestPoison = 0;

        foreach (CombatantView target in livingTargets)
        {
            int poison = target.GetStatusEffectStacks(StatusEffectType.POISON);

            if (poison > highestPoison)
            {
                highestPoison = poison;
                source = target;
            }
        }

        int spreadAmount = highestPoison / Mathf.Max(1, poisonDivisor);
        List<CombatantView> spreadTargets = new();

        if (spreadAmount > 0)
        {
            foreach (CombatantView target in livingTargets)
            {
                if (target != source)
                {
                    spreadTargets.Add(target);
                }
            }
        }

        return new AddStatusEffectGA(
            StatusEffectType.POISON,
            spreadAmount,
            spreadTargets
        );
    }
}