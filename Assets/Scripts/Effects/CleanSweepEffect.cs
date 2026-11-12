using System.Collections.Generic;

[System.Serializable]
public class CleanSweepEffect : Effect
{
    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new CleanSweepGA();
    }
}
