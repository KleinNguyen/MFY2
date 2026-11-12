public class SpendComboGA : GameAction
{
    public int Amount { get; private set; }
    public bool SpendAll { get; private set; }
    public int ActualSpent { get; set; }

    public SpendComboGA(int amount, bool spendAll = false)
    {
        Amount = amount;
        SpendAll = spendAll;
    }
}
