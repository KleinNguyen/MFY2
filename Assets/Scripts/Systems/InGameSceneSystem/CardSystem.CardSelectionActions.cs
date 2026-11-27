using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CardSystem
{
    private IEnumerator ExhaustRandomCardPerformer(
        ExhaustRandomCardGA action
    )
    {
        if (hand.Count == 0)
        {
            yield break;
        }

        int randomIndex = Random.Range(0, hand.Count);

        Card selectedCard = hand[randomIndex];
        CardView selectedCardView = handView.RemoveCard(selectedCard);

        if (selectedCardView == null)
        {
            Debug.LogError(
                "Cannot exhaust card: CardView was not found in hand."
            );

            yield break;
        }

        hand.RemoveAt(randomIndex);

        yield return ExhaustCard(selectedCardView);
    }

    private IEnumerator FocusedStudyPerformer(FocusedStudyGA action)
    {
        DrawCardsGA drawCardsGA = new DrawCardsGA(
            action.DrawAmount
        );

        ActionSystem.Instance.AddReaction(drawCardsGA);

        LockDrawGA lockDrawGA = new LockDrawGA();
        ActionSystem.Instance.AddReaction(lockDrawGA);

        yield return null;
    }

    private IEnumerator LockDrawPerformer(LockDrawGA action)
    {
        drawLocked = true;

        Debug.Log("Card drawing is locked for this turn.");

        yield return null;
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        activeAttackDamageMultiplier =
            nextTurnAttackDamageMultiplier;

        nextTurnAttackDamageMultiplier = 1;
        drawLocked = false;
        isPlayerTurn = true;

        if (pendingCopiesForNextTurn.Count > 0)
        {
            AddCardsToHandGA addCopiesGA = new AddCardsToHandGA(
                pendingCopiesForNextTurn
            );

            pendingCopiesForNextTurn.Clear();

            ActionSystem.Instance.AddReaction(addCopiesGA);
        }
    }

    private IEnumerator ExhaustHandAndDamagePerformer(
        ExhaustHandAndDamageGA action
    )
    {
        if (action.Targets == null || action.Targets.Count == 0)
        {
            yield break;
        }

        int exhaustedCount = 0;

        List<Card> cardsToExhaust = new List<Card>(hand);

        foreach (Card card in cardsToExhaust)
        {
            CardView cardView = handView.RemoveCard(card);

            if (cardView == null)
            {
                Debug.LogError(
                    "Cannot exhaust card: CardView was not found in hand."
                );

                continue;
            }

            hand.Remove(card);

            yield return ExhaustCard(cardView);

            exhaustedCount++;
        }

        for (int i = 0; i < exhaustedCount; i++)
        {
            DealDamageGA dealDamageGA = new DealDamageGA(
                action.DamagePerCard,
                action.Targets,
                action.Caster
            );

            ActionSystem.Instance.AddReaction(dealDamageGA);
        }
    }

    public void ChooseCardToExhaust(Card card)
    {
        if (!IsChoosingCardToExhaust)
        {
            return;
        }

        if (card == null || !hand.Contains(card))
        {
            return;
        }

        chosenCardToExhaust = card;
    }

    private IEnumerator ChooseCardToExhaustPerformer(
        ChooseCardToExhaustGA action
    )
    {
        if (action.RequireExhaust && hand.Count == 0)
        {
            yield break;
        }

        if (hand.Count > 0)
        {
            chosenCardToExhaust = null;
            IsChoosingCardToExhaust = true;

            Debug.Log("Choose a card in your hand to exhaust.");

            while (chosenCardToExhaust == null)
            {
                if (
                    BattleSystem.Instance != null &&
                    BattleSystem.Instance.IsBattleOver
                )
                {
                    IsChoosingCardToExhaust = false;
                    yield break;
                }

                yield return null;
            }

            IsChoosingCardToExhaust = false;

            Card selectedCard = chosenCardToExhaust;
            chosenCardToExhaust = null;

            CardView cardView = handView.RemoveCard(selectedCard);

            if (cardView == null)
            {
                Debug.LogError(
                    "Cannot exhaust selected card: CardView was not found."
                );

                yield break;
            }

            hand.Remove(selectedCard);

            yield return ExhaustCard(cardView);
        }

        if (action.DrawAmount > 0)
        {
            DrawCardsGA drawCardsGA = new DrawCardsGA(
                action.DrawAmount
            );

            drawCardsGA.CardCaster = action.CardCaster;
            drawCardsGA.SourceCard = action.SourceCard;

            ActionSystem.Instance.AddReaction(drawCardsGA);
        }

        if (action.BlockAmount > 0 && action.CardCaster != null)
        {
            AddStatusEffectGA blockGA = new AddStatusEffectGA(
                StatusEffectType.ARMOR,
                action.BlockAmount,
                new List<CombatantView>
                {
                    action.CardCaster
                }
            );

            blockGA.CardCaster = action.CardCaster;
            blockGA.SourceCard = action.SourceCard;

            ActionSystem.Instance.AddReaction(blockGA);
        }
    }
}
