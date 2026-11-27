using System.Collections.Generic;
using UnityEngine;

public static class CardRaritySelector
{
    public static CardData TakeRewardCard(List<CardData> availableCards, EncounterType type)
    {
        return type switch
        {
            EncounterType.Elite => TakeRandomCard(availableCards, 20, 50, 30),
            EncounterType.Boss => TakeRandomCard(availableCards, 0, 0, 100),
            _ => TakeRandomCard(availableCards, 60, 30, 10)
        };
    }

    public static CardData TakeRandomCard(
        List<CardData> availableCards,
        int commonWeight = 60,
        int uncommonWeight = 30,
        int rareWeight = 10
    )
    {
        if (availableCards == null || availableCards.Count == 0) return null;

        List<CardData> common = new();
        List<CardData> uncommon = new();
        List<CardData> rare = new();

        foreach (CardData card in availableCards)
        {
            if (card == null) continue;

            switch (card.Rarity)
            {
                case CardRarity.Common: common.Add(card); break;
                case CardRarity.Uncommon: uncommon.Add(card); break;
                case CardRarity.Rare: rare.Add(card); break;
            }
        }

        commonWeight = common.Count > 0 ? Mathf.Max(0, commonWeight) : 0;
        uncommonWeight = uncommon.Count > 0 ? Mathf.Max(0, uncommonWeight) : 0;
        rareWeight = rare.Count > 0 ? Mathf.Max(0, rareWeight) : 0;
        int total = commonWeight + uncommonWeight + rareWeight;
        List<CardData> selectedGroup;

        if (total == 0)
        {
            // Fall back to the highest available rarity if all configured weights are zero.
            selectedGroup = rare.Count > 0 ? rare : uncommon.Count > 0 ? uncommon : common;
            if (selectedGroup.Count == 0) return null;
        }
        else
        {
            int roll = Random.Range(0, total);
            selectedGroup = roll < commonWeight
                ? common
                : roll < commonWeight + uncommonWeight
                    ? uncommon
                    : rare;
        }

        CardData selected = selectedGroup[Random.Range(0, selectedGroup.Count)];
        // Remove the pick so a reward set cannot offer the same card twice.
        availableCards.Remove(selected);
        return selected;
    }
}