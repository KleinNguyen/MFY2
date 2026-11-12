public class IncreaseTurnManaGA : GameAction
{
    public int Amount { get; private set; }
    public bool NextTurnOnly { get; private set; }

    public IncreaseTurnManaGA(int amount, bool nextTurnOnly = false)
    {
        Amount = amount;
        NextTurnOnly = nextTurnOnly;
    }
}