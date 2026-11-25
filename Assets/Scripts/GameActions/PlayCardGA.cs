public class PlayCardGA : GameAction
{
    public EnemyView ManualTarget { get; private set; }

    public Card Card { get; set; }

    public bool WasPlayed { get; private set; }

    public bool WasAttackCard { get; private set; }

    public int ComboBeforePlay { get; private set; }

    public int AttacksBeforePlay { get; private set; }

    public int AttackOrdinal { get; private set; }

    public PlayCardGA(Card card, EnemyView target = null)
    {
        Card = card;
        ManualTarget = target;
        SourceCard = card;
    }

    public void ResetPlayResult()
    {
        WasPlayed = false;
        WasAttackCard = false;
        ComboBeforePlay = 0;
        AttacksBeforePlay = 0;
        AttackOrdinal = 0;
    }

    public void MarkPlayed(
        bool wasAttackCard,
        int comboBeforePlay,
        int attacksBeforePlay,
        int attackOrdinal
    )
    {
        WasPlayed = true;
        WasAttackCard = wasAttackCard;
        ComboBeforePlay = comboBeforePlay;
        AttacksBeforePlay = attacksBeforePlay;
        AttackOrdinal = attackOrdinal;
    }

    public void SetManualTarget(EnemyView target)
    {
        ManualTarget = target;
    }
}