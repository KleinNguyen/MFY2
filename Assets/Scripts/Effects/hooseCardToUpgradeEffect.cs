using System.Collections.Generic;

[System.Serializable]
public class ChooseCardToUpgradeEffect : Effect
{
    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        return new ChooseCardToUpgradeGA();
    }
}