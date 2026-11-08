using System.Collections.Generic;

public class ReclaimingBlowGA : GameAction, IHaveCaster
{
    public int DamageAmount { get; private set; }
    public List<CombatantView> Targets { get; private set; }
    public CombatantView Caster { get; private set; }

    public ReclaimingBlowGA(int damageAmount, List<CombatantView> targets, CombatantView caster)
    {
        DamageAmount = damageAmount;
        Targets = new List<CombatantView>(targets);
        Caster = caster;
    }
}