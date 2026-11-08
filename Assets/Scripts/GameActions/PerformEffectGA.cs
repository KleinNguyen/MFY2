using System.Collections.Generic;

public class PerformEffectGA : GameAction
{
    public Effect Effect { get; set; }
    public List<CombatantView> Targets { get; set; }

    public PerformEffectGA(Effect effect, List<CombatantView> targets, Card sourceCard = null)
    {
        Effect = effect;
        Targets = targets == null ? null : new List<CombatantView>(targets);
        SourceCard = sourceCard;
    }
}