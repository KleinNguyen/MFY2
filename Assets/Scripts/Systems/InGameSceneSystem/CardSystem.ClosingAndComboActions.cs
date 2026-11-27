using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CardSystem
{
    private IEnumerator GrandDisappearancePerformer(
        GrandDisappearanceGA action
    )
    {
        if (
            !isPlayerTurn ||
            hand.Count < action.RequiredOtherCards
        )
        {
            yield break;
        }

        if (CardViewHoverSystem.Instance != null)
        {
            CardViewHoverSystem.Instance.Hide();
        }

        List<Card> cardsToDiscard = new List<Card>(hand);
        List<Card> discardedCards = new List<Card>();

        foreach (Card card in cardsToDiscard)
        {
            if (
                BattleSystem.Instance != null &&
                BattleSystem.Instance.IsBattleOver
            )
            {
                yield break;
            }

            CardView cardView = handView.RemoveCard(card);

            if (cardView == null)
            {
                Debug.LogError(
                    "Cannot discard card: CardView was not found in hand."
                );

                continue;
            }

            hand.Remove(card);

            yield return DiscardCard(cardView);

            discardedCards.Add(card);
        }

        if (discardedCards.Count >= action.RequiredOtherCards)
        {
            nextTurnAttackDamageMultiplier *= 2;
        }
    }

    private IEnumerator CleanSweepPerformer(CleanSweepGA action)
    {
        if (!isPlayerTurn)
        {
            yield break;
        }

        if (CardViewHoverSystem.Instance != null)
        {
            CardViewHoverSystem.Instance.Hide();
        }

        List<Card> cardsToDiscard = new List<Card>(hand);
        List<Card> discardedCards = new List<Card>();

        foreach (Card card in cardsToDiscard)
        {
            if (
                BattleSystem.Instance != null &&
                BattleSystem.Instance.IsBattleOver
            )
            {
                yield break;
            }

            CardView cardView = handView.RemoveCard(card);

            if (cardView == null)
            {
                Debug.LogError(
                    "Cannot discard card: CardView was not found in hand."
                );

                continue;
            }

            hand.Remove(card);

            yield return DiscardCard(cardView);

            discardedCards.Add(card);
        }

        if (discardedCards.Count == 0)
        {
            yield break;
        }

        DrawCardsGA drawCardsGA = new DrawCardsGA(
            discardedCards.Count
        );

        drawCardsGA.CardCaster = action.CardCaster;
        drawCardsGA.SourceCard = action.SourceCard;

        ActionSystem.Instance.AddReaction(drawCardsGA);
    }

    private IEnumerator NextCardFreePerformer(
        NextCardFreeGA action
    )
    {
        if (isPlayerTurn)
        {
            nextCardFree = true;
        }

        yield return null;
    }

    private void EnemyTurnPreReaction(EnemyTurnGA action)
    {
        activeAttackDamageMultiplier = 1;
        nextCardFree = false;
        additionalManaCostsThisTurn.Clear();
        isPlayerTurn = false;
    }

    private IEnumerator ExhaustCardsForComboPerformer(
        ExhaustCardsForComboGA action
    )
    {
        action.ActualExhausted = 0;

        if (
            !isPlayerTurn ||
            action.CardCaster == null ||
            action.CardCaster.CurrentHealth <= 0
        )
        {
            yield break;
        }

        int exhaustLimit = Mathf.Clamp(
            action.MaxExhaust,
            0,
            hand.Count
        );

        if (exhaustLimit == 0)
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

        bool finished = false;

        for (int i = 0; i < exhaustLimit; i++)
        {
            Card selectedCard = null;

            bool opened = cardSelectionUI.Open(
                new List<Card>(hand),
                "Exhaust up to " + exhaustLimit +
                " cards (" + action.ActualExhausted +
                " exhausted). Done to finish.",
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
                    "Cannot exhaust selected card: CardView was not found in hand."
                );

                break;
            }

            hand.Remove(selectedCard);

            yield return ExhaustCard(cardView);

            action.ActualExhausted++;
        }

        if (
            action.ActualExhausted <= 0 ||
            action.ComboPerCard <= 0 ||
            (
                BattleSystem.Instance != null &&
                BattleSystem.Instance.IsBattleOver
            )
        )
        {
            yield break;
        }

        AddStatusEffectGA gain = new AddStatusEffectGA(
            StatusEffectType.COMBO,
            action.ActualExhausted * action.ComboPerCard,
            new List<CombatantView>
            {
                action.CardCaster
            }
        );

        gain.CardCaster = action.CardCaster;
        gain.SourceCard = action.SourceCard;

        ActionSystem.Instance.AddReaction(gain);
    }
}
