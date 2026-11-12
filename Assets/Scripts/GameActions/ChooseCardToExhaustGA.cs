public class ChooseCardToExhaustGA : GameAction
{
    public int DrawAmount { get; private set; }
    public int BlockAmount { get; private set; }
    public bool RequireExhaust { get; private set; }

    public ChooseCardToExhaustGA(int drawAmount, int blockAmount = 0, bool requireExhaust = false)
    {
        DrawAmount = drawAmount;
        BlockAmount = blockAmount;
        RequireExhaust = requireExhaust;
    }
}
