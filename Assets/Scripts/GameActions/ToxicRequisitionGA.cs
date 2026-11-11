using System.Collections.Generic;

public class ToxicRequisitionGA : GameAction, IHaveCaster
{
    public int MaxDiscard { get; private set; }
    public int DamagePerCard { get; private set; }
    public List<CombatantView> Targets { get; private set; }
    public CombatantView Caster { get; private set; }

    public ToxicRequisitionGA(int maxDiscard, int damagePerCard, List<CombatantView> targets, CombatantView caster)
    {
        MaxDiscard = maxDiscard;
        DamagePerCard = damagePerCard;
        Targets = new List<CombatantView>(targets);
        Caster = caster;
    }
}
