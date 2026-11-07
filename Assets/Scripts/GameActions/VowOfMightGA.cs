using System.Collections.Generic;

public class VowOfMightGA : GameAction
{
    public int HealthAmount { get; private set; }

    public int StrengthAmount { get; private set; }

    public int WeakAmount { get; private set; }

    public List<CombatantView> Targets { get; private set; }

    public CombatantView Caster { get; private set; }

    public VowOfMightGA(
        int healthAmount,
        int strengthAmount,
        int weakAmount,
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        HealthAmount = healthAmount;
        StrengthAmount = strengthAmount;
        WeakAmount = weakAmount;

        Targets = new List<CombatantView>(targets);

        Caster = caster;
    }
}