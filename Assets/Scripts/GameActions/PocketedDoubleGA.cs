public class PocketedDoubleGA : GameAction
{
    public int CopyAmount { get; private set; }

    public PocketedDoubleGA(int copyAmount)
    {
        CopyAmount = copyAmount;
    }
}
