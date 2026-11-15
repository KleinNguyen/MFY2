using System.Collections.Generic;

public class EnemyMoveGA : GameAction, IHaveCaster
{
    public EnemyView Enemy { get; }
    public CombatantView Caster => Enemy;
    public EnemyMoveData Move { get; set; }
    public EnemyMoveType MoveType { get; set; }
    public bool WasExecuted { get; set; }
    public List<AttackHeroGA> Attacks { get; } = new();

    public EnemyMoveGA(EnemyView enemy)
    {
        Enemy = enemy;
    }
}