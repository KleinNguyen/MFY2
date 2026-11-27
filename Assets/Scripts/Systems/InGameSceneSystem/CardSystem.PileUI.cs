using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CardSystem
{
    public void OpenDrawPile()
    {
        OpenPile(drawPile, "Draw Pile");
    }

    public void OpenDiscardPile()
    {
        OpenPile(discardPile, "Discard Pile");
    }

    private void OpenPile(List<Card> pile, string title)
    {
        if (
            !isPlayerTurn ||
            IsChoosingCardToExhaust ||
            IsChoosingCardToUpgrade
        )
        {
            return;
        }

        if (
            BattleSystem.Instance == null ||
            BattleSystem.Instance.IsBattleOver ||
            BattleSystem.Instance.IsPreparingBattle
        )
        {
            return;
        }

        if (
            ActionSystem.Instance == null ||
            ActionSystem.Instance.IsPerforming
        )
        {
            return;
        }

        if (
            InteractionSystem.Instance == null ||
            InteractionSystem.Instance.PlayerIsDragging
        )
        {
            return;
        }

        if (cardSelectionUI == null)
        {
            Debug.LogError(
                "Card Selection UI is not assigned in CardSystem.",
                this
            );

            return;
        }

        if (cardSelectionUI.IsOpen)
        {
            return;
        }

        List<Card> cards = pile.FindAll(card => card != null);

        cards.Sort(
            (left, right) => string.Compare(
                left.Title,
                right.Title,
                System.StringComparison.OrdinalIgnoreCase
            )
        );

        string message = title + " (" + cards.Count + ")";

        if (cards.Count == 0)
        {
            message += "\nNo cards in this pile.";
        }

        bool opened = cardSelectionUI.Open(
            cards,
            message,
            null,
            readOnly: true
        );

        if (opened)
        {
            if (CardViewHoverSystem.Instance != null)
            {
                CardViewHoverSystem.Instance.Hide();
            }

            if (ManualTargetSystem.Instance != null)
            {
                ManualTargetSystem.Instance.CancelTargeting();
            }
        }
    }
}
