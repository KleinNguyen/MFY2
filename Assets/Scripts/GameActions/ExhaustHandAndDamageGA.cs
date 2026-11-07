using System.Collections.Generic;

public class ExhaustHandAndDamageGA : GameAction, IHaveCaster
{
    public int DamagePerCard { get; private set; }

    public List<CombatantView> Targets { get; private set; }

    public CombatantView Caster { get; private set; }

    public ExhaustHandAndDamageGA(
        int damagePerCard,
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        DamagePerCard = damagePerCard;
        Targets = new List<CombatantView>(targets);
        Caster = caster;
    }
}