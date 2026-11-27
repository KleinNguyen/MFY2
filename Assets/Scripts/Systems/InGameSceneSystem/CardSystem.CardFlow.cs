using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CardSystem
{
    private IEnumerator DrawCardsPerformer(DrawCardsGA drawCardsGA)
    {
        if (drawLocked)
        {
            Debug.Log("Cannot draw more cards this turn.");
            yield break;
        }

        for (int i = 0; i < drawCardsGA.Amount; i++)
        {
            if (drawPile.Count == 0)
            {
                yield return RefillDeck();
            }

            if (drawPile.Count == 0)
            {
                yield break;
            }

            yield return DrawCard();
        }
    }

    private IEnumerator DiscardAllCardsPerformer(
        DiscardAllCardsGA discardAllCardsGA
    )
    {
        if (hand.Count == 0)
        {
            yield break;
        }

        handView.SetInteractionEnabled(false);

        List<Card> cardsToDiscard = new List<Card>(hand);

        float stagger = 0f;

        if (cardsToDiscard.Count > 1)
        {
            stagger = Mathf.Min(
                0.025f,
                0.08f / (cardsToDiscard.Count - 1)
            );
        }

        int remainingFlights = 0;

        for (int i = 0; i < cardsToDiscard.Count; i++)
        {
            CardView cardView = handView.RemoveCard(cardsToDiscard[i]);

            if (cardView == null)
            {
                continue;
            }

            discardPile.Add(cardView.Card);
            remainingFlights++;

            StartCoroutine(
                AnimateCardToPile(
                    cardView,
                    discardPilePoint,
                    EndTurnDiscardDuration,
                    stagger * i,
                    () => remainingFlights--
                )
            );
        }

        pileUI.UpdateDiscardPileText(discardPile.Count);

        while (remainingFlights > 0)
        {
            yield return null;
        }

        hand.Clear();
    }

    private IEnumerator PlayCardPerformer(PlayCardGA action)
    {
        action.ResetPlayResult();

        if (!CanPlayCard(action.Card))
        {
            yield break;
        }

        int manaCost = GetManaCost(action.Card);

        if (
            action.Card.ManualTargetEffect != null &&
            (
                action.ManualTarget == null ||
                action.ManualTarget.CurrentHealth <= 0
            )
        )
        {
            yield break;
        }

        if (CardViewHoverSystem.Instance != null)
        {
            CardViewHoverSystem.Instance.Hide();
        }

        CardView cardView = handView.RemoveCard(action.Card);

        if (cardView == null)
        {
            Debug.LogError(
                "Cannot play card: CardView was not found in hand."
            );

            yield break;
        }

        nextCardFree = false;
        hand.Remove(action.Card);

        if (action.Card.ExhaustOnPlay)
        {
            yield return ExhaustCard(cardView);
        }
        else
        {
            yield return DiscardCard(cardView);
        }

        if (Combo != null)
        {
            Combo.RecordCardPlay(action);
        }

        SpendManaGA spendManaGA = new SpendManaGA(manaCost);
        ActionSystem.Instance.AddReaction(spendManaGA);

        if (action.Card.ManualTargetEffect != null)
        {
            PerformEffectGA manualEffectGA = new PerformEffectGA(
                action.Card.ManualTargetEffect,
                new List<CombatantView>
                {
                    action.ManualTarget
                },
                action.Card
            );

            ActionSystem.Instance.AddReaction(manualEffectGA);
        }

        if (action.Card.OtherEffects == null)
        {
            yield break;
        }

        foreach (AutoTargetEffect effectWrapper in action.Card.OtherEffects)
        {
            if (
                effectWrapper == null ||
                effectWrapper.TargetMode == null ||
                effectWrapper.Effect == null
            )
            {
                Debug.LogError(
                    "A card has an incomplete Other Effects entry: " +
                    action.Card.Title
                );

                continue;
            }

            List<CombatantView> targets =
                effectWrapper.TargetMode.GetTargets();

            PerformEffectGA otherEffectGA = new PerformEffectGA(
                effectWrapper.Effect,
                targets,
                action.Card
            );

            ActionSystem.Instance.AddReaction(otherEffectGA);
        }
    }

    private IEnumerator DrawCard()
    {
        Card card = null;

        while (priorityDrawCards.Count > 0)
        {
            Card priorityCard = priorityDrawCards.Pop();

            if (drawPile.Remove(priorityCard))
            {
                card = priorityCard;
                break;
            }
        }

        if (card == null)
        {
            card = drawPile.Draw();
        }

        if (card == null)
        {
            yield break;
        }

        hand.Add(card);
        CardsDrawnThisCombat++;

        SfxManager.PlaySfx(SfxId.CardDrawn);

        pileUI.UpdateDrawPileText(drawPile.Count);

        CardView cardView = CardViewCreator.Instance.CreateCardView(
            card,
            drawPilePoint.position,
            drawPilePoint.rotation
        );

        yield return handView.AddCard(cardView);
    }

    private IEnumerator RefillDeck()
    {
        if (discardPile.Count == 0)
        {
            yield break;
        }

        List<Card> cardsToRefill = new List<Card>(discardPile);

        drawPile.AddRange(discardPile);
        discardPile.Clear();

        pileUI.UpdateDrawPileText(drawPile.Count);
        pileUI.UpdateDiscardPileText(discardPile.Count);

        if (
            CardViewCreator.Instance == null ||
            discardPilePoint == null ||
            drawPilePoint == null
        )
        {
            Debug.LogError(
                "Cannot animate the discard pile refill: a card view creator or pile point is missing.",
                this
            );

            yield break;
        }

        int remainingFlights = 0;
        float stagger = 0f;

        if (cardsToRefill.Count > 1)
        {
            stagger = Mathf.Min(
                0.06f,
                0.3f / (cardsToRefill.Count - 1)
            );
        }

        for (int i = 0; i < cardsToRefill.Count; i++)
        {
            float pileOffset =
                (i - (cardsToRefill.Count - 1) * 0.5f) * 0.08f;

            Vector3 startPosition =
                discardPilePoint.position + Vector3.right * pileOffset;

            CardView cardView = CardViewCreator.Instance.CreateCardView(
                cardsToRefill[i],
                startPosition,
                discardPilePoint.rotation
            );

            if (cardView == null)
            {
                Debug.LogError(
                    "Could not create a temporary card view for the discard pile refill.",
                    this
                );

                continue;
            }

            cardView.transform.DOKill();
            cardView.transform.localScale =
                cardView.DisplayScale * RefillCardFlightScale;

            cardView.enabled = false;

            foreach (
                Collider cardCollider in
                cardView.GetComponentsInChildren<Collider>()
            )
            {
                cardCollider.enabled = false;
            }

            remainingFlights++;

            StartCoroutine(
                AnimateCardToPile(
                    cardView,
                    drawPilePoint,
                    RefillFlightDuration,
                    stagger * i,
                    () => remainingFlights--
                )
            );
        }

        while (remainingFlights > 0)
        {
            yield return null;
        }

        yield return null;
    }

    private IEnumerator DiscardCard(
        CardView cardView,
        float duration = DiscardFlightDuration
    )
    {
        if (cardView == null || cardView.Card == null)
        {
            Debug.LogError(
                "Cannot discard card: CardView or its card is missing.",
                this
            );

            yield break;
        }

        discardPile.Add(cardView.Card);
        pileUI.UpdateDiscardPileText(discardPile.Count);

        yield return AnimateCardToPile(
            cardView,
            discardPilePoint,
            duration
        );
    }

    private IEnumerator ExhaustCard(CardView cardView)
    {
        exhaustPile.Add(cardView.Card);

        Debug.Log(
            "Exhausted: " + cardView.Card.Title +
            " | Exhaust pile: " + exhaustPile.Count
        );

        cardView.transform.DOScale(
            Vector3.zero,
            0.15f
        );

        Tween tween = cardView.transform.DOMove(
            discardPilePoint.position,
            0.15f
        );

        yield return tween.WaitForCompletion();

        Destroy(cardView.gameObject);
    }
}
