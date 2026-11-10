using System.Collections.Generic;

[System.Serializable]
public class LowestHealthEnemyTM : TargetMode
{
    public override List<CombatantView> GetTargets()
    {
        List<CombatantView> targets = new();

        if (EnemySystem.Instance == null ||
            EnemySystem.Instance.Enemies == null)
        {
            return targets;
        }

        EnemyView selectedEnemy = null;

        foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
        {
            if (enemy == null ||
                !enemy.gameObject.activeInHierarchy ||
                enemy.CurrentHealth <= 0)
            {
                continue;
            }

            if (selectedEnemy == null ||
                enemy.CurrentHealth < selectedEnemy.CurrentHealth)
            {
                selectedEnemy = enemy;
            }
        }

        if (selectedEnemy != null)
        {
            targets.Add(selectedEnemy);
        }

        return targets;
    }
}