public class LoseHealthGA : GameAction
{
    public int Amount { get; private set; }

    public bool IsFromCard { get; private set; }

    public LoseHealthGA(int amount, bool isFromCard = false)
    {
        Amount = amount;
        IsFromCard = isFromCard;
    }
}