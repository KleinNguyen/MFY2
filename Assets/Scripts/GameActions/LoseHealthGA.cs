public class LoseHealthGA : GameAction
{
    public int Amount { get; private set; }

    public LoseHealthGA(int amount)
    {
        Amount = amount;
    }
}