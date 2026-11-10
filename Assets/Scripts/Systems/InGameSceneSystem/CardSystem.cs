
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public class CardSystem : Singleton<CardSystem>
{
    [SerializeField] private HandView handView;
    [SerializeField] private Transform drawPilePoint;
    [SerializeField] private Transform discardPilePoint;
    [SerializeField] private PileUI pileUI;
    [SerializeField] private CardSelectionUI cardSelectionUI;

    private readonly Stack<Card> priorityDrawCards = new();
    private readonly List<Card> drawPile = new();
    private readonly List<Card> discardPile = new();
    private readonly List<Card> hand = new();
    private readonly List<Card> exhaustPile = new();
    public bool IsChoosingCardToExhaust { get; private set; }
    public bool IsChoosingCardToUpgrade { get; private set; }
    public bool IsViewingPile => cardSelectionUI != null &&
        cardSelectionUI.IsOpen && cardSelectionUI.IsReadOnly;

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
        if (!isPlayerTurn || IsChoosingCardToExhaust || IsChoosingCardToUpgrade) return;
        if (BattleSystem.Instance == null || BattleSystem.Instance.IsBattleOver) return;
        if (ActionSystem.Instance == null || ActionSystem.Instance.IsPerforming) return;
        if (InteractionSystem.Instance == null || InteractionSystem.Instance.PlayerIsDragging) return;

        if (cardSelectionUI == null)
        {
            Debug.LogError("Card Selection UI is not assigned in CardSystem.", this);
            return;
        }

        if (cardSelectionUI.IsOpen) return;

        List<Card> cards = pile.FindAll(card => card != null);
        cards.Sort((left, right) => string.Compare(
            left.Title, right.Title, System.StringComparison.OrdinalIgnoreCase));

        string message = title + " (" + cards.Count + ")";
        if (cards.Count == 0) message += "\nNo cards in this pile.";

        if (cardSelectionUI.Open(cards, message, null, readOnly: true))
        {
            if (CardViewHoverSystem.Instance != null) CardViewHoverSystem.Instance.Hide();
            if (ManualTargetSystem.Instance != null) ManualTargetSystem.Instance.CancelTargeting();
        }
    }

    private CardView chosenCardViewToUpgrade;
    private Card chosenCardToExhaust;
    private bool drawLocked;
    private bool isPlayerTurn = true;

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<DrawCardsGA>(DrawCardsPerformer);
        ActionSystem.AttachPerformer<DiscardAllCardsGA>(DiscardAllCardsPerformer);
        ActionSystem.AttachPerformer<PlayCardGA>(PlayCardPerformer);
        ActionSystem.AttachPerformer<ExhaustRandomCardGA>(ExhaustRandomCardPerformer);
        ActionSystem.AttachPerformer<FocusedStudyGA>(FocusedStudyPerformer);
        ActionSystem.AttachPerformer<LockDrawGA>(LockDrawPerformer);
        ActionSystem.AttachPerformer<ExhaustHandAndDamageGA>(ExhaustHandAndDamagePerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.AttachPerformer<ChooseCardToExhaustGA>(ChooseCardToExhaustPerformer);
        ActionSystem.AttachPerformer<ChooseCardToUpgradeGA>(ChooseCardToUpgradePerformer);
        ActionSystem.AttachPerformer<ReclaimingBlowGA>(ReclaimingBlowPerformer);
        ActionSystem.AttachPerformer<RecoverDiscardCardGA>(RecoverDiscardCardPerformer);
        ActionSystem.AttachPerformer<DiscardCardsGA>(DiscardCardsPerformer);
        ActionSystem.AttachPerformer<PlayDiscardedSlyCardsGA>(PlayDiscardedSlyCardsPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<DrawCardsGA>();
        ActionSystem.DetachPerformer<DiscardAllCardsGA>();
        ActionSystem.DetachPerformer<PlayCardGA>();
        ActionSystem.DetachPerformer<ExhaustRandomCardGA>();
        ActionSystem.DetachPerformer<FocusedStudyGA>();
        ActionSystem.DetachPerformer<LockDrawGA>();
        ActionSystem.DetachPerformer<ExhaustHandAndDamageGA>();
        ActionSystem.DetachPerformer<ChooseCardToExhaustGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.DetachPerformer<ChooseCardToUpgradeGA>();
        ActionSystem.DetachPerformer<ReclaimingBlowGA>();
        ActionSystem.DetachPerformer<RecoverDiscardCardGA>();
        ActionSystem.DetachPerformer<DiscardCardsGA>();
        ActionSystem.DetachPerformer<PlayDiscardedSlyCardsGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
    }
    //public
    public void Setup(List<CardData> deckData)
    {
        isPlayerTurn = true;
        priorityDrawCards.Clear();
        IsChoosingCardToUpgrade = false;
        chosenCardViewToUpgrade = null;
        IsChoosingCardToExhaust = false;
        chosenCardToExhaust = null;
        drawLocked = false;
        exhaustPile.Clear();
        foreach (var cardData in deckData)
        {
            Card card = new(cardData);
            drawPile.Add(card);
        }
        pileUI.UpdateDrawPileText(drawPile.Count);
        pileUI.UpdateDiscardPileText(discardPile.Count);
    }

    //performers
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
                RefillDeck();
            }
            if (drawPile.Count == 0)
            {
                yield break;
            }
            yield return DrawCard();
        }
    }


    private IEnumerator DiscardAllCardsPerformer(DiscardAllCardsGA discardAllCardsGA)
    {
        foreach (var card in hand)
        {
            //discardPile.Add(card);
            CardView cardView = handView.RemoveCard(card);
            yield return DiscardCard(cardView);
        }
        hand.Clear();
    }

    private IEnumerator PlayCardPerformer(PlayCardGA action)
    {
        if (action.Card == null || !isPlayerTurn)
        {
            yield break;
        }

        bool cardIsInHand = hand.Contains(action.Card);

        if (action.IsSlyPlay)
        {

            if (!action.Card.IsSly || (!cardIsInHand && !discardPile.Contains(action.Card)))
            {
                yield break;
            }

            if (action.Card.ManualTargetEffect != null)
            {
                List<EnemyView> validTargets = new();

                if (EnemySystem.Instance != null)
                {
                    foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
                    {
                        if (enemy != null && enemy.CurrentHealth > 0)
                        {
                            validTargets.Add(enemy);
                        }
                    }
                }
                if (validTargets.Count == 0)
                {
                    yield break;
                }

                int randomIndex = Random.Range(0, validTargets.Count);
                action.SetManualTarget(validTargets[randomIndex]);
            }
        }
        else
        {
            if (!cardIsInHand || !ManaSystem.Instance.HasEnoughMana(action.Card.Mana))
            {
                yield break;
            }
        }

        if (action.Card.ManualTargetEffect != null && (action.ManualTarget == null || action.ManualTarget.CurrentHealth <= 0))
        {
            yield break;
        }

        if (CardViewHoverSystem.Instance != null)
        {
            CardViewHoverSystem.Instance.Hide();
        }

        if (cardIsInHand)
        {
            CardView cardView = handView.RemoveCard(action.Card);

            if (cardView == null)
            {
                Debug.LogError("Cannot play card: CardView was not found in hand.");
                yield break;
            }

            hand.Remove(action.Card);

            if (action.Card.ExhaustOnPlay)
            {
                yield return ExhaustCard(cardView);
            }
            else
            {
                yield return DiscardCard(cardView);
            }
        }
        else
        {
            if (action.Card.ExhaustOnPlay)
            {
                discardPile.Remove(action.Card);
                exhaustPile.Add(action.Card);

                pileUI.UpdateDiscardPileText(discardPile.Count);
            }
        }

        if (action.IsSlyPlay)
        {
            Debug.Log("Sly auto-play: " + action.Card.Title);
        }
        else
        {
            SpendManaGA spendManaGA = new(action.Card.Mana);
            ActionSystem.Instance.AddReaction(spendManaGA);
        }

        if (action.Card.ManualTargetEffect != null)
        {
            PerformEffectGA manualEffectGA = new(action.Card.ManualTargetEffect, new List<CombatantView> { action.ManualTarget }, action.Card);
            ActionSystem.Instance.AddReaction(manualEffectGA);
        }

        if (action.Card.OtherEffects == null)
        {
            yield break;
        }

        foreach (AutoTargetEffect effectWrapper in action.Card.OtherEffects)
        {
            if (effectWrapper == null || effectWrapper.TargetMode == null || effectWrapper.Effect == null)
            {
                Debug.LogError("A card has an incomplete Other Effects entry: " + action.Card.Title);
                continue;
            }

            List<CombatantView> targets = effectWrapper.TargetMode.GetTargets();

            PerformEffectGA otherEffectGA = new(effectWrapper.Effect, targets, action.Card);
            ActionSystem.Instance.AddReaction(otherEffectGA);
        }
    }
    //reactions


    //helpers
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

        pileUI.UpdateDrawPileText(drawPile.Count);

        CardView cardView = CardViewCreator.Instance.CreateCardView(card, drawPilePoint.position, drawPilePoint.rotation);

        yield return handView.AddCard(cardView);
    }
    private void RefillDeck()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        pileUI.UpdateDrawPileText(drawPile.Count);
        pileUI.UpdateDiscardPileText(discardPile.Count);
    }
    private IEnumerator DiscardCard(CardView cardView)
    {
        discardPile.Add(cardView.Card);
        pileUI.UpdateDiscardPileText(discardPile.Count);
        cardView.transform.DOScale(Vector3.zero, 0.15f);
        Tween tween = cardView.transform.DOMove(discardPilePoint.position, 0.15f);
        yield return tween.WaitForCompletion();
        Destroy(cardView.gameObject);
    }
    private IEnumerator ExhaustCard(CardView cardView)
    {
        exhaustPile.Add(cardView.Card);

        Debug.Log(
            "Exhausted: " + cardView.Card.Title +
            " | Exhaust pile: " + exhaustPile.Count
        );
        cardView.transform.DOScale(Vector3.zero, 0.15f);

        Tween tween = cardView.transform.DOMove(
            discardPilePoint.position,
            0.15f
        );

        yield return tween.WaitForCompletion();

        Destroy(cardView.gameObject);
    }
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
        DrawCardsGA drawCardsGA = new(action.DrawAmount);

        ActionSystem.Instance.AddReaction(drawCardsGA);
        LockDrawGA lockDrawGA = new();

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
        drawLocked = false;
        isPlayerTurn = true;
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

        List<Card> cardsToExhaust = new(hand);

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
            DealDamageGA dealDamageGA = new(
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
        if (hand.Count > 0)
        {
            chosenCardToExhaust = null;
            IsChoosingCardToExhaust = true;

            Debug.Log("Choose a card in your hand to exhaust.");

            yield return new WaitUntil(
                () => chosenCardToExhaust != null
            );

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
            DrawCardsGA drawCardsGA = new(action.DrawAmount);

            ActionSystem.Instance.AddReaction(drawCardsGA);
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
    private IEnumerator ChooseCardToUpgradePerformer(
    ChooseCardToUpgradeGA action
)
    {
        bool hasUpgradeableCard = false;

        foreach (Card card in hand)
        {
            if (card.CanUpgradeForCombat())
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

        yield return new WaitUntil(
            () => chosenCardViewToUpgrade != null
        );

        IsChoosingCardToUpgrade = false;

        CardView cardView = chosenCardViewToUpgrade;

        chosenCardViewToUpgrade = null;

        Card selectedCard = cardView.Card;

        selectedCard.UpgradeForCombat();


        cardView.Setup(selectedCard);

        yield return null;
    }
    private IEnumerator ReclaimingBlowPerformer(ReclaimingBlowGA action)
    {
        if (action.Caster == null || action.Targets == null || action.Targets.Count == 0)
        {
            yield break;
        }

        DealDamageGA damageGA = new(action.DamageAmount, action.Targets, action.Caster);
        damageGA.CardCaster = action.CardCaster;
        damageGA.SourceCard = action.SourceCard;

        ActionSystem.Instance.AddReaction(damageGA);

        RecoverDiscardCardGA recoverGA = new(action.SourceCard);

        ActionSystem.Instance.AddReaction(recoverGA);

        yield return null;
    }
    private IEnumerator RecoverDiscardCardPerformer(RecoverDiscardCardGA action)
    {
        List<Card> choices = new();

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
            Debug.LogError("Card Selection UI is not assigned in CardSystem.");
            yield break;
        }

        Card selectedCard = null;

        bool opened = cardSelectionUI.Open(choices, "Choose a card to put on top of your Draw Pile", card => selectedCard = card);

        if (!opened)
        {
            yield break;
        }

        while (selectedCard == null && cardSelectionUI != null && cardSelectionUI.IsOpen)
        {
            if (BattleSystem.Instance != null && BattleSystem.Instance.IsBattleOver)
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

        if (selectedCard == null || !discardPile.Remove(selectedCard))
        {
            yield break;
        }

        drawPile.Add(selectedCard);
        priorityDrawCards.Push(selectedCard);

        pileUI.UpdateDrawPileText(drawPile.Count);
        pileUI.UpdateDiscardPileText(discardPile.Count);
    }
    private IEnumerator DiscardCardsPerformer(DiscardCardsGA action)
    {
        int discardCount = Mathf.Clamp(action.Amount, 0, hand.Count);

        if (discardCount > 0 && cardSelectionUI == null)
        {
            Debug.LogError("Card Selection UI is not assigned in CardSystem.");
            yield break;
        }

        List<Card> discardedCards = new();

        for (int i = 0; i < discardCount; i++)
        {
            List<Card> choices = new(hand);
            Card selectedCard = null;

            bool opened = cardSelectionUI.Open(choices, "Choose a card to discard", card => selectedCard = card);

            if (!opened)
            {
                yield break;
            }

            while (selectedCard == null && cardSelectionUI != null && cardSelectionUI.IsOpen)
            {
                if (BattleSystem.Instance != null && BattleSystem.Instance.IsBattleOver)
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

            if (CardViewHoverSystem.Instance != null)
            {
                CardViewHoverSystem.Instance.Hide();
            }

            CardView cardView = handView.RemoveCard(selectedCard);

            if (cardView == null)
            {
                Debug.LogError("Cannot discard card: CardView was not found in hand.");
                yield break;
            }

            hand.Remove(selectedCard);

            yield return DiscardCard(cardView);

            discardedCards.Add(selectedCard);
        }

        if (action.DrawAfterDiscard > 0)
        {
            DrawCardsGA drawCardsGA = new(action.DrawAfterDiscard);
            ActionSystem.Instance.AddReaction(drawCardsGA);
        }

        if (isPlayerTurn && discardedCards.Count > 0)
        {
            PlayDiscardedSlyCardsGA slyGA = new(discardedCards);
            ActionSystem.Instance.AddReaction(slyGA);
        }
    }
    private IEnumerator PlayDiscardedSlyCardsPerformer(PlayDiscardedSlyCardsGA action)
    {
        if (!isPlayerTurn || action.Cards == null)
        {
            yield break;
        }

        foreach (Card card in action.Cards)
        {
            if (card == null || !card.IsSly)
            {
                continue;
            }

            PlayCardGA playCardGA = new(card, null, true);

            ActionSystem.Instance.AddReaction(playCardGA);
        }

        yield return null;
    }
    private void EnemyTurnPreReaction(EnemyTurnGA action)
    {
        isPlayerTurn = false;
    }
}

