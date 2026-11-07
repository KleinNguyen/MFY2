using System.Collections.Generic;

public class DamageThenWeakBonusGA : GameAction, IHaveCaster
{
    public int BaseDamage { get; private set; }

    public int BonusDamage { get; private set; }

    public List<CombatantView> Targets { get; private set; }

    public CombatantView Caster { get; private set; }

    public DamageThenWeakBonusGA(
        int baseDamage,
        int bonusDamage,
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        BaseDamage = baseDamage;
        BonusDamage = bonusDamage;
        Targets = new List<CombatantView>(targets);
        Caster = caster;
    }
}