public class FocusedStudyGA : GameAction
{
    public int DrawAmount { get; private set; }

    public FocusedStudyGA(int drawAmount)
    {
        DrawAmount = drawAmount;
    }
}