public class IncreaseTurnManaGA : GameAction
{
    public int Amount { get; private set; }

    public IncreaseTurnManaGA(int amount)
    {
        Amount = amount;
    }
}