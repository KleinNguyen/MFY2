public class ExhaustCardsForComboGA : GameAction
{
    public int MaxExhaust { get; private set; }
    public int ComboPerCard { get; private set; }
    public int ActualExhausted { get; set; }

    public ExhaustCardsForComboGA(int maxExhaust, int comboPerCard)
    {
        MaxExhaust = maxExhaust;
        ComboPerCard = comboPerCard;
    }
}