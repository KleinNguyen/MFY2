public class PlayCardGA : GameAction
{
    public EnemyView ManualTarget { get; private set; }
    public Card Card { get; set; }
    public bool IsSlyPlay { get; private set; }

    public PlayCardGA(Card card, EnemyView target = null, bool isSlyPlay = false)
    {
        Card = card;
        ManualTarget = target;
        IsSlyPlay = isSlyPlay;
        SourceCard = card;
    }

    public void SetManualTarget(EnemyView target)
    {
        ManualTarget = target;
    }
}