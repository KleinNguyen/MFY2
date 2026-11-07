public class ChooseCardToExhaustGA : GameAction
{
    public int DrawAmount { get; private set; }

    public ChooseCardToExhaustGA(int drawAmount)
    {
        DrawAmount = drawAmount;
    }
}