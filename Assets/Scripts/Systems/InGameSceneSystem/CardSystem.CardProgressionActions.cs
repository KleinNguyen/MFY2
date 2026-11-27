using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CardSystem
{
    private IEnumerator PocketedDoublePerformer(
        PocketedDoubleGA action
    )
    {
        if (
            !isPlayerTurn ||
            hand.Count == 0 ||
            action.CopyAmount <= 0
        )
        {
            yield break;
        }

        if (cardSelectionUI == null)
        {
            Debug.LogError(
                "Card Selection UI is not assigned in CardSystem."
            );

            yield break;
        }

        Card selectedCard = null;
        List<Card> choices = new List<Card>(hand);

        bool opened = cardSelectionUI.Open(
            choices,
            "Choose a card to copy next turn",
            card => selectedCard = card
        );

        if (!opened)
        {
            yield break;
        }

        while (
            selectedCard == null &&
            cardSelectionUI != null &&
            cardSelectionUI.IsOpen
        )
        {
            if (
                BattleSystem.Instance != null &&
                BattleSystem.Instance.IsBattleOver
            )
            {
                cardSelectionUI.Close();
                yield break;
            }

            yield return null;
        }

        if (cardSelectionUI != null)
        {
            cardSelectionUI.Close();
        }

        if (selectedCard == null || !hand.Contains(selectedCard))
        {
            yield break;
        }

        for (int i = 0; i < action.CopyAmount; i++)
        {
            pendingCopiesForNextTurn.Add(
                selectedCard.CreateCombatCopy()
            );
        }
    }

    private IEnumerator AddCardsToHandPerformer(
        AddCardsToHandGA action
    )
    {
        if (!isPlayerTurn || action.Cards == null)
        {
            yield break;
        }

        foreach (Card card in action.Cards)
        {
            if (
                BattleSystem.Instance != null &&
                BattleSystem.Instance.IsBattleOver
            )
            {
                yield break;
            }

            if (card == null)
            {
                continue;
            }

            hand.Add(card);

            CardView cardView =
                CardViewCreator.Instance.CreateCardView(
                    card,
                    drawPilePoint.position,
                    drawPilePoint.rotation
                );

            yield return handView.AddCard(cardView);
        }
    }

    public void ChooseCardToUpgrade(CardView cardView)
    {
        if (!IsChoosingCardToUpgrade || cardView == null)
        {
            return;
        }

        Card card = cardView.Card;

        if (card == null || !hand.Contains(card))
        {
            return;
        }

        if (!card.CanUpgradeForCombat())
        {
            Debug.Log("This card cannot be upgraded.");
            return;
        }

        chosenCardViewToUpgrade = cardView;
    }

    private void UpgradeMatchingCards(
        IEnumerable<Card> cards,
        Card selectedCard
    )
    {
        foreach (Card card in cards)
        {
            if (card != null && card.IsSameType(selectedCard))
            {
                card.UpgradeForCombat();
            }
        }
    }

    private IEnumerator ChooseCardToUpgradePerformer(
        ChooseCardToUpgradeGA action
    )
    {
        bool hasUpgradeableCard = false;

        foreach (Card card in hand)
        {
            if (card != null && card.CanUpgradeForCombat())
            {
                hasUpgradeableCard = true;
                break;
            }
        }

        if (!hasUpgradeableCard)
        {
            Debug.Log("There are no cards to upgrade.");
            yield break;
        }

        chosenCardViewToUpgrade = null;
        IsChoosingCardToUpgrade = true;

        Debug.Log("Choose a card in your hand to upgrade.");

        while (chosenCardViewToUpgrade == null)
        {
            yield return null;
        }

        IsChoosingCardToUpgrade = false;

        CardView selectedView = chosenCardViewToUpgrade;
        chosenCardViewToUpgrade = null;

        if (selectedView == null || selectedView.Card == null)
        {
            yield break;
        }

        Card selectedCard = selectedView.Card;

        if (
            !hand.Contains(selectedCard) ||
            !selectedCard.CanUpgradeForCombat()
        )
        {
            yield break;
        }

        UpgradeMatchingCards(hand, selectedCard);
        UpgradeMatchingCards(drawPile, selectedCard);
        UpgradeMatchingCards(discardPile, selectedCard);
        UpgradeMatchingCards(exhaustPile, selectedCard);
        UpgradeMatchingCards(priorityDrawCards, selectedCard);
        UpgradeMatchingCards(pendingCopiesForNextTurn, selectedCard);

        handView.RefreshCards();

        if (CardViewHoverSystem.Instance != null)
        {
            CardViewHoverSystem.Instance.Hide();
        }

        yield return null;
    }

    private IEnumerator ReclaimingBlowPerformer(
        ReclaimingBlowGA action
    )
    {
        if (
            action.Caster == null ||
            action.Targets == null ||
            action.Targets.Count == 0
        )
        {
            yield break;
        }

        DealDamageGA damageGA = new DealDamageGA(
            action.DamageAmount,
            action.Targets,
            action.Caster
        );

        damageGA.CardCaster = action.CardCaster;
        damageGA.SourceCard = action.SourceCard;

        ActionSystem.Instance.AddReaction(damageGA);

        RecoverDiscardCardGA recoverGA = new RecoverDiscardCardGA(
            action.SourceCard
        );

        ActionSystem.Instance.AddReaction(recoverGA);

        yield return null;
    }

    private IEnumerator RecoveredTechniquePerformer(
        RecoveredTechniqueGA action
    )
    {
        if (!isPlayerTurn)
        {
            yield break;
        }

        List<Card> choices = discardPile.FindAll(
            card => card != null && card != action.SourceCard
        );

        if (choices.Count == 0)
        {
            yield break;
        }

        Card recoveredCard = choices[
            Random.Range(0, choices.Count)
        ];

        discardPile.Remove(recoveredCard);

        if (action.AdditionalManaCost > 0)
        {
            additionalManaCostsThisTurn.TryGetValue(
                recoveredCard,
                out int currentAdditionalCost
            );

            additionalManaCostsThisTurn[recoveredCard] =
                currentAdditionalCost + action.AdditionalManaCost;
        }

        hand.Add(recoveredCard);
        pileUI.UpdateDiscardPileText(discardPile.Count);

        CardView cardView = CardViewCreator.Instance.CreateCardView(
            recoveredCard,
            discardPilePoint.position,
            discardPilePoint.rotation
        );

        yield return handView.AddCard(cardView);
    }

    private IEnumerator RecoverDiscardCardPerformer(
        RecoverDiscardCardGA action
    )
    {
        List<Card> choices = new List<Card>();

        foreach (Card card in discardPile)
        {
            if (card != null && card != action.ExcludedCard)
            {
                choices.Add(card);
            }
        }

        if (choices.Count == 0)
        {
            yield break;
        }

        if (cardSelectionUI == null)
        {
            Debug.LogError(
                "Card Selection UI is not assigned in CardSystem."
            );

            yield break;
        }

        Card selectedCard = null;

        bool opened = cardSelectionUI.Open(
            choices,
            "Choose a card to put on top of your Draw Pile",
            card => selectedCard = card
        );

        if (!opened)
        {
            yield break;
        }

        while (
            selectedCard == null &&
            cardSelectionUI != null &&
            cardSelectionUI.IsOpen
        )
        {
            if (
                BattleSystem.Instance != null &&
                BattleSystem.Instance.IsBattleOver
            )
            {
                cardSelectionUI.Close();
                yield break;
            }

            yield return null;
        }

        if (cardSelectionUI != null)
        {
            cardSelectionUI.Close();
        }

        if (
            selectedCard == null ||
            !discardPile.Remove(selectedCard)
        )
        {
            yield break;
        }

        drawPile.Add(selectedCard);
        priorityDrawCards.Push(selectedCard);

        pileUI.UpdateDrawPileText(drawPile.Count);
        pileUI.UpdateDiscardPileText(discardPile.Count);
    }
}
