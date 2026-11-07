using System.Collections.Generic;

public class DamageThenStatusGA : GameAction, IHaveCaster
{
    public int DamageAmount { get; private set; }

    public StatusEffectType StatusEffectType { get; private set; }

    public int StackCount { get; private set; }

    public List<CombatantView> Targets { get; private set; }

    public CombatantView Caster { get; private set; }

    public DamageThenStatusGA(
        int damageAmount,
        StatusEffectType statusEffectType,
        int stackCount,
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        DamageAmount = damageAmount;
        StatusEffectType = statusEffectType;
        StackCount = stackCount;

        Targets = new List<CombatantView>(targets);

        Caster = caster;
    }
}