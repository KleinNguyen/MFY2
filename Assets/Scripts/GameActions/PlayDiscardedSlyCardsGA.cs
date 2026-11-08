using System.Collections.Generic;

public class PlayDiscardedSlyCardsGA : GameAction
{
    public List<Card> Cards { get; private set; }

    public PlayDiscardedSlyCardsGA(List<Card> cards)
    {
        Cards = new List<Card>(cards);
    }
}