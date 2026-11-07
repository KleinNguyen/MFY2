public class GainAdversityPowerGA : GameAction
{
    public int Amount { get; private set; }

    public GainAdversityPowerGA(int amount)
    {
        Amount = amount;
    }
}