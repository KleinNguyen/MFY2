public class GainThornsGA : GameAction
{
    public int Amount { get; private set; }

    public GainThornsGA(int amount)
    {
        Amount = amount;
    }
}