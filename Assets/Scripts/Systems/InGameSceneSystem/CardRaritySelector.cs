using System.Collections.Generic;
using UnityEngine;

public static class CardRaritySelector
{
    private const int COMMON_WEIGHT = 60;
    private const int UNCOMMON_WEIGHT = 30;
    private const int RARE_WEIGHT = 10;

    public static CardData TakeRandomCard(List<CardData> availableCards)
    {
        List<CardData> commonCards = new();
        List<CardData> uncommonCards = new();
        List<CardData> rareCards = new();

        foreach (CardData card in availableCards)
        {
            if (card == null) continue;

            if (card.Rarity == CardRarity.Common)
            {
                commonCards.Add(card);
            }
            else if (card.Rarity == CardRarity.Uncommon)
            {
                uncommonCards.Add(card);
            }
            else if (card.Rarity == CardRarity.Rare)
            {
                rareCards.Add(card);
            }
        }

        int commonWeight = commonCards.Count > 0 ? COMMON_WEIGHT : 0;
        int uncommonWeight = uncommonCards.Count > 0 ? UNCOMMON_WEIGHT : 0;
        int rareWeight = rareCards.Count > 0 ? RARE_WEIGHT : 0;

        int totalWeight = commonWeight + uncommonWeight + rareWeight;

        if (totalWeight == 0)
        {
            return null;
        }

        int randomValue = Random.Range(0, totalWeight);

        List<CardData> selectedGroup;

        if (randomValue < commonWeight)
        {
            selectedGroup = commonCards;
        }
        else if (randomValue < commonWeight + uncommonWeight)
        {
            selectedGroup = uncommonCards;
        }
        else
        {
            selectedGroup = rareCards;
        }

        int randomIndex = Random.Range(0, selectedGroup.Count);
        CardData selectedCard = selectedGroup[randomIndex];

        availableCards.Remove(selectedCard);

        return selectedCard;
    }
}