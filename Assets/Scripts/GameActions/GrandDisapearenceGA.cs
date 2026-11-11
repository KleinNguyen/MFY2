public class GrandDisappearanceGA : GameAction
{
    public int RequiredOtherCards { get; private set; }

    public GrandDisappearanceGA(int requiredOtherCards)
    {
        RequiredOtherCards = requiredOtherCards;
    }
}
