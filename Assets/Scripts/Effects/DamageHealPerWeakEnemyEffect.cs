using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DamageHealPerWeakEnemyEffect : Effect
{
    [SerializeField] private int damageAmount;

    [SerializeField] private int healPerWeakEnemy;

    [SerializeField] private int maxHeal;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        int weakEnemyCount = 0;

        if (EnemySystem.Instance != null)
        {
            foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
            {
                if (enemy == null || enemy.CurrentHealth <= 0)
                {
                    continue;
                }

                int weakStacks = enemy.GetStatusEffectStacks(
                    StatusEffectType.WEAK
                );

                if (weakStacks > 0)
                {
                    weakEnemyCount++;
                }
            }
        }

        int healAmount = Mathf.Min(
            weakEnemyCount * healPerWeakEnemy,
            maxHeal
        );

        DealDamageGA dealDamageGA = new(
            damageAmount,
            targets,
            caster
        );

        dealDamageGA.HealAfterDamage = healAmount;

        return dealDamageGA;
    }
}