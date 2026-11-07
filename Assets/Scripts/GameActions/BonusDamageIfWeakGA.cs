using System.Collections.Generic;

public class BonusDamageIfWeakGA : GameAction, IHaveCaster
{
    public int BonusDamage { get; private set; }

    public List<CombatantView> Targets { get; private set; }

    public CombatantView Caster { get; private set; }

    public BonusDamageIfWeakGA(
        int bonusDamage,
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        BonusDamage = bonusDamage;
        Targets = new List<CombatantView>(targets);
        Caster = caster;
    }
}