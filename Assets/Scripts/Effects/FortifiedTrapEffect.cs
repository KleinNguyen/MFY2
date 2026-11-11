using System.Collections.Generic;

[System.Serializable]
public class FortifiedTrapEffect : Effect
{
    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new FortifiedTrapGA();
    }
}
