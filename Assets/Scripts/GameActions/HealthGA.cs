using System.Collections.Generic;

public class HealGA : GameAction, IHaveCaster
{
    public int Amount { get; private set; }

    public List<CombatantView> Targets { get; private set; }

    public CombatantView Caster { get; private set; }

    public HealGA(
        int amount,
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        Amount = amount;
        Targets = new List<CombatantView>(targets);
        Caster = caster;
    }
}