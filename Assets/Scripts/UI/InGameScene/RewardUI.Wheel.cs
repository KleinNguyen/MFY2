using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public partial class RewardUI
{
    // Lucky wheel
    public void OpenRewardWheel()
    {
        RunSystem run = RunSystem.Instance;

        if (run == null || run.IsRunFinished || !run.RewardsPrepared || run.RewardClaimed ||
            selectionFinished || wheelOpen || run.RewardWheelResult >= 0) return;

        if (luckyWheel == null || !luckyWheel.IsConfigured) return;

        wheelOpen = true;
        UpdateButtons();

        if (!luckyWheel.Show(RewardWheelLabels, ApplyWheelResult, OnWheelClosed,
            canCancel: true, closeCaption: "Back to rewards", character: run.HeroData.Character))
        {
            OnWheelClosed();
        }
    }

    private void ApplyWheelResult(int result)
    {
        RunSystem run = RunSystem.Instance;
        if (run == null) return;

        int outcome = result % WheelOutcomeCount;
        int cardIndex = -1;
        CardData replacement = null;

        if (outcome == 1) TryFindRarityReplacement(out cardIndex, out replacement);
        if (!run.ApplyRewardWheel(result, cardIndex, replacement)) return;

        bool cardChanged = replacement != null && cardIndex >= 0 &&
            cardIndex < run.RewardChoices.Count && run.RewardChoices[cardIndex] == replacement;

        RefreshRewardView(run);

        string message = outcome switch
        {
            0 => "Gold reward doubled!",
            1 => cardChanged
                ? "Higher rarity reward: " + replacement.name
                : "No higher rarity card available. Rewards unchanged.",
            _ => "All rewards lost. Press Continue."
        };

        if (outcome == 2)
        {
            FinishSelection(message);
        }
        else
        {
            messageText.text = message;
            UpdateButtons();
        }
    }

    private void OnWheelClosed()
    {
        wheelOpen = false;
        UpdateButtons();
    }

    // Choose only among cards that have a valid higher-rarity replacement.
    private bool TryFindRarityReplacement(out int cardIndex, out CardData replacement)
    {
        cardIndex = -1;
        replacement = null;
        List<int> eligibleIndices = new();

        for (int i = 0; i < currentChoices.Count; i++)
        {
            if (GetHigherRarityCards(currentChoices[i]).Count > 0) eligibleIndices.Add(i);
        }

        if (eligibleIndices.Count == 0) return false;
        cardIndex = eligibleIndices[Random.Range(0, eligibleIndices.Count)];
        List<CardData> candidates = GetHigherRarityCards(currentChoices[cardIndex]);
        replacement = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    private List<CardData> GetHigherRarityCards(CardData original)
    {
        List<CardData> candidates = new();
        if (original == null || rewardCards == null) return candidates;

        foreach (CardData card in rewardCards)
        {
            if (card == null || card.Character != original.Character ||
                currentChoices.Contains(card) || candidates.Contains(card))
                continue;
            if (IsHigherRarity(original.Rarity, card.Rarity)) candidates.Add(card);
        }

        return candidates;
    }

    private static bool IsHigherRarity(CardRarity original, CardRarity replacement)
    {
        return original switch
        {
            CardRarity.Common => replacement == CardRarity.Uncommon || replacement == CardRarity.Rare,
            CardRarity.Uncommon => replacement == CardRarity.Rare,
            _ => false
        };
    }
}
