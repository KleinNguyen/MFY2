using System.Collections.Generic;

public class AddCardsToHandGA : GameAction
{
    public List<Card> Cards { get; private set; }

    public AddCardsToHandGA(List<Card> cards)
    {
        Cards = new List<Card>(cards);
    }
}
