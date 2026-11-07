using System.Collections.Generic;

public class LoseHealthThenStatusGA : GameAction
{
    public int HealthAmount { get; private set; }

    public StatusEffectType StatusEffectType { get; private set; }

    public int StackCount { get; private set; }

    public List<CombatantView> Targets { get; private set; }

    public LoseHealthThenStatusGA(
        int healthAmount,
        StatusEffectType statusEffectType,
        int stackCount,
        List<CombatantView> targets
    )
    {
        HealthAmount = healthAmount;
        StatusEffectType = statusEffectType;
        StackCount = stackCount;

        Targets = new List<CombatantView>(targets);
    }
}