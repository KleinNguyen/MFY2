public class DiscardCardsGA : GameAction
{
    public int Amount { get; private set; }
    public int DrawAfterDiscard { get; private set; }

    public DiscardCardsGA(int amount, int drawAfterDiscard = 0)
    {
        Amount = amount;
        DrawAfterDiscard = drawAfterDiscard;
    }
}