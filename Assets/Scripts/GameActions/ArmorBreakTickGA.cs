public class ArmorBreakTickGA : GameAction
{
    public CombatantView Target { get; }

    public ArmorBreakTickGA(CombatantView target)
    {
        Target = target;
    }
}