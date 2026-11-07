using System.Collections.Generic;

public class BlockThenDamageGA : GameAction, IHaveCaster
{
    public int BlockAmount { get; private set; }

    public int DamageAmount { get; private set; }

    public List<CombatantView> Targets { get; private set; }

    public CombatantView Caster { get; private set; }

    public BlockThenDamageGA(
        int blockAmount,
        int damageAmount,
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        BlockAmount = blockAmount;
        DamageAmount = damageAmount;

        Targets = new List<CombatantView>(targets);

        Caster = caster;
    }
}