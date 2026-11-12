public class PlayCardGA : GameAction
{
    public EnemyView ManualTarget { get; private set; }
    public Card Card { get; set; }
    public bool IsSlyPlay { get; private set; }
    public bool WasPlayed { get; private set; }
    public bool WasAttackCard { get; private set; }
    public int ComboBeforePlay { get; private set; }
    public int AttacksBeforePlay { get; private set; }
    public int AttackOrdinal { get; private set; }

    public void ResetPlayResult()
    {
        WasPlayed = false;
        WasAttackCard = false;
        ComboBeforePlay = 0;
        AttacksBeforePlay = 0;
        AttackOrdinal = 0;
    }

    public void MarkPlayed(bool wasAttackCard, int comboBeforePlay, int attacksBeforePlay, int attackOrdinal)
    {
        WasPlayed = true;
        WasAttackCard = wasAttackCard;
        ComboBeforePlay = comboBeforePlay;
        AttacksBeforePlay = attacksBeforePlay;
        AttackOrdinal = attackOrdinal;
    }

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
