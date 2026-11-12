public class GainComboGA : GameAction
{
    public int Amount { get; private set; }

    public GainComboGA(int amount)
    {
        Amount = amount;
    }
}
