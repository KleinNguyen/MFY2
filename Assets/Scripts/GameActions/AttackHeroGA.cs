public class AttackHeroGA : GameAction, IHaveCaster
{
    public EnemyView Attacker { get; }
    public CombatantView Caster => Attacker;
    public bool WasExecuted { get; set; }

    public AttackHeroGA(EnemyView attacker)
    {
        Attacker = attacker;
    }
}