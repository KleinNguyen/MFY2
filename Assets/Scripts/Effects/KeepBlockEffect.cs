using System.Collections.Generic;

[System.Serializable]
public class KeepBlockEffect : Effect
{
    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        return new KeepBlockGA();
    }
}