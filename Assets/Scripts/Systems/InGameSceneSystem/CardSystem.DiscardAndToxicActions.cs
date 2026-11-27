using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CardSystem
{
    private IEnumerator DiscardCardsPerformer(
        DiscardCardsGA action
    )
    {
        int discardCount = Mathf.Clamp(
            action.Amount,
            0,
            hand.Count
        );

        if (discardCount > 0 && cardSelectionUI == null)
        {
            Debug.LogError(
                "Card Selection UI is not assigned in CardSystem."
            );

            yield break;
        }

        for (int i = 0; i < discardCount; i++)
        {
            List<Card> choices = new List<Card>(hand);
            Card selectedCard = null;

            bool opened = cardSelectionUI.Open(
                choices,
                "Choose a card to discard",
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
                !hand.Contains(selectedCard)
            )
            {
                yield break;
            }

            if (CardViewHoverSystem.Instance != null)
            {
                CardViewHoverSystem.Instance.Hide();
            }

            CardView cardView = handView.RemoveCard(selectedCard);

            if (cardView == null)
            {
                Debug.LogError(
                    "Cannot discard card: CardView was not found in hand."
                );

                yield break;
            }

            hand.Remove(selectedCard);

            yield return DiscardCard(cardView);
        }

        if (action.DrawAfterDiscard > 0)
        {
            DrawCardsGA drawCardsGA = new DrawCardsGA(
                action.DrawAfterDiscard
            );

            ActionSystem.Instance.AddReaction(drawCardsGA);
        }
    }

    private IEnumerator ToxicRequisitionPerformer(
        ToxicRequisitionGA action
    )
    {
        if (
            !isPlayerTurn ||
            action.Caster == null ||
            action.Targets == null ||
            !action.Targets.Exists(
                target => target != null && target.CurrentHealth > 0
            )
        )
        {
            yield break;
        }

        int discardLimit = Mathf.Clamp(
            action.MaxDiscard,
            0,
            hand.Count
        );

        if (discardLimit > 0 && cardSelectionUI == null)
        {
            Debug.LogError(
                "Card Selection UI is not assigned in CardSystem."
            );

            yield break;
        }

        List<Card> discardedCards = new List<Card>();
        bool finished = false;

        for (int i = 0; i < discardLimit; i++)
        {
            Card selectedCard = null;
            List<Card> choices = new List<Card>(hand);

            bool opened = cardSelectionUI.Open(
                choices,
                "Discard up to " + discardLimit +
                " cards (" + discardedCards.Count +
                " discarded). Done to finish.",
                card => selectedCard = card,
                onFinished: () => finished = true
            );

            if (!opened)
            {
                break;
            }

            while (
                !finished &&
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

            if (
                cardSelectionUI != null &&
                cardSelectionUI.IsOpen
            )
            {
                cardSelectionUI.Close();
            }

            if (
                finished ||
                selectedCard == null ||
                !hand.Contains(selectedCard)
            )
            {
                break;
            }

            if (CardViewHoverSystem.Instance != null)
            {
                CardViewHoverSystem.Instance.Hide();
            }

            CardView cardView = handView.RemoveCard(selectedCard);

            if (cardView == null)
            {
                Debug.LogError(
                    "Cannot discard card: CardView was not found in hand."
                );

                break;
            }

            hand.Remove(selectedCard);

            yield return DiscardCard(cardView);

            discardedCards.Add(selectedCard);
        }

        if (
            discardedCards.Count == 0 ||
            (
                BattleSystem.Instance != null &&
                BattleSystem.Instance.IsBattleOver
            )
        )
        {
            yield break;
        }

        int damage = Mathf.Max(
            0,
            action.DamagePerCard * discardedCards.Count
        );

        DealDamageGA damageGA = new DealDamageGA(
            damage,
            action.Targets,
            action.Caster
        );

        damageGA.CardCaster = action.CardCaster;
        damageGA.SourceCard = action.SourceCard;

        ActionSystem.Instance.AddReaction(damageGA);
    }
}
