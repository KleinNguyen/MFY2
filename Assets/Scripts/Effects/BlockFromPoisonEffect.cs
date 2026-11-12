using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockFromPoisonEffect : Effect
{
    [SerializeField, Min(0)] private int maxBlock = 30;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        int blockAmount = 0;
        int blockLimit = Mathf.Max(0, maxBlock);

        if (EnemySystem.Instance != null &&
            EnemySystem.Instance.Enemies != null)
        {
            foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
            {
                if (enemy == null ||
                    !enemy.gameObject.activeInHierarchy ||
                    enemy.CurrentHealth <= 0)
                {
                    continue;
                }

                int poisonStacks = Mathf.Max(
                    0,
                    enemy.GetStatusEffectStacks(StatusEffectType.POISON)
                );

                blockAmount += Mathf.Min(
                    poisonStacks,
                    blockLimit - blockAmount
                );

                if (blockAmount >= blockLimit) break;
            }
        }

        return new AddStatusEffectGA(
            StatusEffectType.ARMOR,
            blockAmount,
            targets ?? new List<CombatantView>()
        );
    }
}