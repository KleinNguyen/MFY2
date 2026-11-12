using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TacticalPressureEffect : Effect
{
    [SerializeField, Min(0)] private int blockAmount = 7;
    [SerializeField, Min(1)] private int requiredAttacks = 2;
    [SerializeField, Min(0)] private int damageAmount = 6;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        AddStatusEffectGA blockGA = new(
            StatusEffectType.ARMOR,
            blockAmount,
            targets ?? new List<CombatantView>()
        );
        blockGA.CardCaster = caster;

        ComboSystem combo = CardSystem.Instance != null ? CardSystem.Instance.Combo : null;

        if (caster == null || combo == null || combo.AttacksThisTurn < requiredAttacks ||
            EnemySystem.Instance == null)
        {
            return blockGA;
        }

        EnemyView lowestHealthEnemy = null;

        foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
        {
            if (enemy == null || enemy.CurrentHealth <= 0 || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (lowestHealthEnemy == null || enemy.CurrentHealth < lowestHealthEnemy.CurrentHealth)
            {
                lowestHealthEnemy = enemy;
            }
        }

        if (lowestHealthEnemy != null)
        {
            DealDamageGA damageGA = new(
                damageAmount,
                new List<CombatantView> { lowestHealthEnemy },
                caster
            );
            damageGA.CardCaster = caster;
            blockGA.PostReactions.Add(damageGA);
        }

        return blockGA;
    }
}
