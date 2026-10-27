using UnityEngine;
using System.Collections.Generic;

public class Card 
{
    public string Title => data.name;
    public string Description => data.Description;
    public Effect ManualTargetEffect => data.ManualTargetEffect;
    public List<AutoTargetEffect> OtherEffects => data.OtherEffects;
    public int Mana { get; private set; }
    public CardType Type => data.Type;
    public bool ExhaustOnPlay => data.ExhaustOnPlay;
    public bool IsSly => data.IsSly;

    private readonly CardData data;
    public Card(CardData cardData)
    {
        data = cardData;
        Mana = cardData.Mana;
    }

//     public void SetMana(int amount)
// {
//     if (amount < 0)
//     {
//         Mana = 0;
//     }
//     else
//     {
//         Mana = amount;
//     }
// }
}
