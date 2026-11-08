public class RecoverDiscardCardGA : GameAction
{
    public Card ExcludedCard { get; private set; }

    public RecoverDiscardCardGA(Card excludedCard)
    {
        ExcludedCard = excludedCard;
    }
}