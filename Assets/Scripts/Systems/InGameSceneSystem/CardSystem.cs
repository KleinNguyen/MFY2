
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;
using UnityEngine.Rendering;

public class CardSystem : Singleton<CardSystem>
{
    private const float DiscardFlightDuration = 0.40f;
    private const float EndTurnDiscardDuration = 0.40f;
    private const float RefillFlightDuration = 0.50f;
    private const float RefillCardFlightScale = 0.08f;
    private const float DiscardArcHeight = 5.2f;
    private const float DiscardTrailLifetime = 0.3f;
    private const float DiscardSparkLifetime = 0.36f;

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
    private readonly Dictionary<Card, int> additionalManaCostsThisTurn = new();
    private readonly HashSet<Card> temporarySlyCards = new();
    private readonly List<Card> pendingCopiesForNextTurn = new();
    public int CardsDrawnThisCombat { get; private set; }
    public ComboSystem Combo { get; private set; }
    public bool IsChoosingCardToExhaust { get; private set; }
    public bool IsChoosingCardToUpgrade { get; private set; }
    public bool IsViewingPile
    {
        get
        {
            if (cardSelectionUI == null) return false;

            return cardSelectionUI.IsOpen && cardSelectionUI.IsReadOnly;
        }
    }
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
        if (BattleSystem.Instance == null || BattleSystem.Instance.IsBattleOver || BattleSystem.Instance.IsPreparingBattle) return;
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
    private bool nextCardFree;
    private int activeAttackDamageMultiplier = 1;
    private int nextTurnAttackDamageMultiplier = 1;

    public int AttackDamageMultiplier
    {
        get
        {
            if (!isPlayerTurn) return 1;

            return activeAttackDamageMultiplier;
        }
    }

    public bool CanPlayCard(Card card)
    {
        return card != null && !card.IsUnplayable && isPlayerTurn && hand.Contains(card) &&
            BattleSystem.Instance != null && !BattleSystem.Instance.IsBattleOver && !BattleSystem.Instance.IsPreparingBattle &&
            MeetsCardPlayConditions(card) && ManaSystem.Instance != null && ManaSystem.Instance.HasEnoughMana(GetManaCost(card));
    }

    public bool IsCardInHand(Card card)
    {
        return card != null && hand.Contains(card);
    }

    private bool MeetsCardPlayConditions(Card card)
    {
        int otherCardsInHand = hand.Count - (hand.Contains(card) ? 1 : 0);

        if (card.ManualTargetEffect is ICardPlayCondition manualCondition &&
            !manualCondition.CanPlay(otherCardsInHand))
        {
            return false;
        }

        if (card.OtherEffects != null)
        {
            foreach (AutoTargetEffect effectWrapper in card.OtherEffects)
            {
                if (effectWrapper != null && effectWrapper.Effect is ICardPlayCondition condition &&
                    !condition.CanPlay(otherCardsInHand))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public int GetManaCost(Card card)
    {
        if (card == null) return 0;
        if (nextCardFree && isPlayerTurn && hand.Contains(card)) return 0;

        additionalManaCostsThisTurn.TryGetValue(card, out int additionalManaCost);
        return Mathf.Max(0, card.Mana + additionalManaCost);
    }

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
        ActionSystem.AttachPerformer<ChooseCardToGrantSlyGA>(ChooseCardToGrantSlyPerformer);
        ActionSystem.AttachPerformer<PocketedDoubleGA>(PocketedDoublePerformer);
        ActionSystem.AttachPerformer<AddCardsToHandGA>(AddCardsToHandPerformer);
        ActionSystem.AttachPerformer<ChooseCardToUpgradeGA>(ChooseCardToUpgradePerformer);
        ActionSystem.AttachPerformer<ReclaimingBlowGA>(ReclaimingBlowPerformer);
        ActionSystem.AttachPerformer<RecoverDiscardCardGA>(RecoverDiscardCardPerformer);
        ActionSystem.AttachPerformer<RecoveredTechniqueGA>(RecoveredTechniquePerformer);
        ActionSystem.AttachPerformer<DiscardCardsGA>(DiscardCardsPerformer);
        ActionSystem.AttachPerformer<ToxicRequisitionGA>(ToxicRequisitionPerformer);
        ActionSystem.AttachPerformer<CleanSweepGA>(CleanSweepPerformer);
        ActionSystem.AttachPerformer<GrandDisappearanceGA>(GrandDisappearancePerformer);
        ActionSystem.AttachPerformer<NextCardFreeGA>(NextCardFreePerformer);
        ActionSystem.AttachPerformer<PlayDiscardedSlyCardsGA>(PlayDiscardedSlyCardsPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.AttachPerformer<ExhaustCardsForComboGA>(ExhaustCardsForComboPerformer);

        Combo = GetComponent<ComboSystem>();
        if (Combo == null) Combo = gameObject.AddComponent<ComboSystem>();
        Combo.enabled = true;
    }

    private void OnDisable()
    {
        if (Combo != null) Combo.enabled = false;
        activeAttackDamageMultiplier = 1;
        nextTurnAttackDamageMultiplier = 1;
        ResetTemporarySlyCards();
        pendingCopiesForNextTurn.Clear();
        nextCardFree = false;
        additionalManaCostsThisTurn.Clear();
        ActionSystem.DetachPerformer<DrawCardsGA>();
        ActionSystem.DetachPerformer<DiscardAllCardsGA>();
        ActionSystem.DetachPerformer<PlayCardGA>();
        ActionSystem.DetachPerformer<ExhaustRandomCardGA>();
        ActionSystem.DetachPerformer<FocusedStudyGA>();
        ActionSystem.DetachPerformer<LockDrawGA>();
        ActionSystem.DetachPerformer<ExhaustHandAndDamageGA>();
        ActionSystem.DetachPerformer<ChooseCardToExhaustGA>();
        ActionSystem.DetachPerformer<ChooseCardToGrantSlyGA>();
        ActionSystem.DetachPerformer<PocketedDoubleGA>();
        ActionSystem.DetachPerformer<AddCardsToHandGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.DetachPerformer<ChooseCardToUpgradeGA>();
        ActionSystem.DetachPerformer<ReclaimingBlowGA>();
        ActionSystem.DetachPerformer<RecoverDiscardCardGA>();
        ActionSystem.DetachPerformer<RecoveredTechniqueGA>();
        ActionSystem.DetachPerformer<DiscardCardsGA>();
        ActionSystem.DetachPerformer<ToxicRequisitionGA>();
        ActionSystem.DetachPerformer<CleanSweepGA>();
        ActionSystem.DetachPerformer<GrandDisappearanceGA>();
        ActionSystem.DetachPerformer<NextCardFreeGA>();
        ActionSystem.DetachPerformer<PlayDiscardedSlyCardsGA>();
        ActionSystem.DetachPerformer<ExhaustCardsForComboGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
    }
    //public
    public void Setup(List<CardData> deckData)
    {
        if (Combo != null) Combo.ResetCombat();
        activeAttackDamageMultiplier = 1;
        nextTurnAttackDamageMultiplier = 1;
        ResetTemporarySlyCards();
        pendingCopiesForNextTurn.Clear();
        CardsDrawnThisCombat = 0;
        nextCardFree = false;
        additionalManaCostsThisTurn.Clear();
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
    public void AddJunkCards(int amount, Sprite image)
    {
        if (amount <= 0) return;

        for (int i = 0; i < amount; i++) drawPile.Add(Card.CreateJunk(image));
        pileUI.UpdateDrawPileText(drawPile.Count);
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
                yield return RefillDeck();
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
            yield return DiscardCard(cardView, EndTurnDiscardDuration);
        }
        hand.Clear();
    }

    private IEnumerator PlayCardPerformer(PlayCardGA action)
    {
        action.ResetPlayResult();
        if (action.Card == null || action.Card.IsUnplayable || !isPlayerTurn || BattleSystem.Instance == null || BattleSystem.Instance.IsBattleOver || BattleSystem.Instance.IsPreparingBattle)
        {
            yield break;
        }

        bool cardIsInHand = hand.Contains(action.Card);
        int manaCost = GetManaCost(action.Card);

        if (action.IsSlyPlay)
        {

            if (!action.Card.IsSly || (!cardIsInHand && !discardPile.Contains(action.Card)) ||
                !MeetsCardPlayConditions(action.Card))
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
            if (!CanPlayCard(action.Card))
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

            if (!action.IsSlyPlay)
            {
                nextCardFree = false;
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

        if (Combo != null) Combo.RecordCardPlay(action);

        if (action.IsSlyPlay)
        {
            Debug.Log("Sly auto-play: " + action.Card.Title);
        }
        else
        {
            SpendManaGA spendManaGA = new(manaCost);
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
        CardsDrawnThisCombat++;
        SfxManager.PlaySfx(SfxId.CardDrawn);

        pileUI.UpdateDrawPileText(drawPile.Count);

        CardView cardView = CardViewCreator.Instance.CreateCardView(card, drawPilePoint.position, drawPilePoint.rotation);

        yield return handView.AddCard(cardView);
    }
    private IEnumerator RefillDeck()
    {
        if (discardPile.Count == 0) yield break;

        List<Card> cardsToRefill = new(discardPile);
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        pileUI.UpdateDrawPileText(drawPile.Count);
        pileUI.UpdateDiscardPileText(discardPile.Count);

        if (CardViewCreator.Instance == null || discardPilePoint == null || drawPilePoint == null)
        {
            Debug.LogError("Cannot animate the discard pile refill: a card view creator or pile point is missing.", this);
            yield break;
        }

        int remainingFlights = 0;
        float stagger = cardsToRefill.Count > 1
            ? Mathf.Min(0.06f, 0.3f / (cardsToRefill.Count - 1))
            : 0f;

        for (int i = 0; i < cardsToRefill.Count; i++)
        {
            float pileOffset = (i - (cardsToRefill.Count - 1) * 0.5f) * 0.08f;
            Vector3 startPosition = discardPilePoint.position + Vector3.right * pileOffset;
            CardView cardView = CardViewCreator.Instance.CreateCardView(
                cardsToRefill[i],
                startPosition,
                discardPilePoint.rotation
            );

            if (cardView == null)
            {
                Debug.LogError("Could not create a temporary card view for the discard pile refill.", this);
                continue;
            }

            cardView.transform.DOKill();
            cardView.transform.localScale = cardView.DisplayScale * RefillCardFlightScale;
            cardView.enabled = false;
            foreach (Collider cardCollider in cardView.GetComponentsInChildren<Collider>())
            {
                cardCollider.enabled = false;
            }

            remainingFlights++;
            StartCoroutine(AnimateCardToPile(
                cardView,
                drawPilePoint,
                RefillFlightDuration,
                stagger * i,
                () => remainingFlights--
            ));
        }

        while (remainingFlights > 0) yield return null;
        yield return null;
    }
    private IEnumerator DiscardCard(
        CardView cardView,
        float duration = DiscardFlightDuration
    )
    {
        if (cardView == null || cardView.Card == null)
        {
            Debug.LogError("Cannot discard card: CardView or its card is missing.", this);
            yield break;
        }

        discardPile.Add(cardView.Card);
        pileUI.UpdateDiscardPileText(discardPile.Count);

        yield return AnimateCardToPile(cardView, discardPilePoint, duration);
    }

    private IEnumerator AnimateCardToPile(
        CardView cardView,
        Transform destination,
        float duration,
        float startDelay = 0f,
        System.Action onComplete = null
    )
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        if (cardView == null || cardView.Card == null || destination == null)
        {
            Debug.LogError("Cannot animate a card to a missing pile or card view.", this);
            if (cardView != null) Destroy(cardView.gameObject);
            if (onComplete != null) onComplete();
            yield break;
        }

        Transform cardTransform = cardView.transform;
        Vector3 startPosition = cardTransform.position;
        Vector3 endPosition = destination.position;
        Vector3 startScale = cardTransform.localScale;
        Quaternion startRotation = cardTransform.rotation;
        Quaternion endRotation = destination.rotation;

        SortingGroup sortingGroup = cardView.GetComponent<SortingGroup>();
        int sortingLayerId = sortingGroup != null
            ? sortingGroup.sortingLayerID
            : 0;
        int trailSortingOrder = sortingGroup != null
            ? sortingGroup.sortingOrder - 1
            : -1;

        Shader trailShader = Shader.Find("Sprites/Default");
        Material trailMaterial = null;
        GameObject trailObject = null;
        ParticleSystem sparks = null;
        TrailRenderer glowTrail = null;
        TrailRenderer coreTrail = null;

        if (trailShader == null)
        {
            Debug.LogError("Cannot create the card pile flight trail: Sprites/Default shader was not found.", this);
        }
        else
        {
            trailMaterial = new Material(trailShader);
            trailObject = new GameObject("CardPileFlightTrail");
            trailObject.transform.position = startPosition;

            glowTrail = CreateDiscardTrail(
                trailObject.transform,
                "Glow",
                trailMaterial,
                new Color(1f, 0.72f, 0.08f),
                0.28f,
                0.62f,
                sortingLayerId,
                trailSortingOrder
            );
            coreTrail = CreateDiscardTrail(
                trailObject.transform,
                "Core",
                trailMaterial,
                new Color(1f, 0.96f, 0.62f),
                0.075f,
                0.95f,
                sortingLayerId,
                trailSortingOrder
            );
            sparks = CreateDiscardTrailSparks(
                trailObject,
                trailMaterial,
                sortingLayerId,
                trailSortingOrder
            );
        }

        Tween flightTween = DOTween.To(
            () => 0f,
            progress =>
            {
                Vector3 position = Vector3.Lerp(
                    startPosition,
                    endPosition,
                    progress
                );
                position += Vector3.up *
                    (DiscardArcHeight * 4f * progress * (1f - progress));
                cardTransform.position = position;

                float scaleProgress = progress <= 0.5f
                    ? Mathf.SmoothStep(0f, 1f, progress * 2f)
                    : Mathf.SmoothStep(0f, 1f, (progress - 0.5f) * 2f);
                float scaleMultiplier = progress <= 0.5f
                    ? Mathf.Lerp(1f, 1.45f, scaleProgress)
                    : Mathf.Lerp(1.45f, 0.015f, scaleProgress);
                cardTransform.localScale = startScale * scaleMultiplier;

                Quaternion rotation = Quaternion.Slerp(
                    startRotation,
                    endRotation,
                    progress
                );
                cardTransform.rotation =
                    rotation *
                    Quaternion.Euler(
                        0f,
                        0f,
                        Mathf.Sin(progress * Mathf.PI) * 10f
                    );

                if (trailObject != null)
                {
                    trailObject.transform.position = position;
                }
            },
            1f,
            duration
        ).SetEase(Ease.InOutSine);

        yield return flightTween.WaitForCompletion();

        if (glowTrail != null)
        {
            glowTrail.emitting = false;
            coreTrail.emitting = false;
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            float cleanupDelay = Mathf.Max(
                DiscardTrailLifetime,
                DiscardSparkLifetime
            );
            Destroy(trailObject, cleanupDelay);
            Destroy(trailMaterial, cleanupDelay);
        }

        Destroy(cardView.gameObject);
        if (onComplete != null) onComplete();
    }

    private static TrailRenderer CreateDiscardTrail(
        Transform parent,
        string objectName,
        Material material,
        Color color,
        float width,
        float alpha,
        int sortingLayerId,
        int sortingOrder
    )
    {
        GameObject trailObject = new(objectName);
        trailObject.transform.SetParent(parent, false);

        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.time = DiscardTrailLifetime;
        trail.startWidth = width;
        trail.endWidth = width * 0.04f;
        trail.minVertexDistance = 0.02f;
        trail.numCornerVertices = 5;
        trail.numCapVertices = 5;
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.autodestruct = false;
        trail.sharedMaterial = material;
        trail.sortingLayerID = sortingLayerId;
        trail.sortingOrder = sortingOrder;
        trail.colorGradient = CreateDiscardTrailGradient(color, alpha);
        trail.Clear();
        return trail;
    }

    private static Gradient CreateDiscardTrailGradient(Color color, float alpha)
    {
        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(alpha, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        return gradient;
    }

    private static ParticleSystem CreateDiscardTrailSparks(
        GameObject effectObject,
        Material material,
        int sortingLayerId,
        int sortingOrder
    )
    {
        ParticleSystem sparks = effectObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = sparks.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            0.16f,
            DiscardSparkLifetime
        );
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.075f);
        main.startColor = new Color(1f, 0.9f, 0.35f);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 48;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
            sparks.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            CreateDiscardTrailGradient(
                new Color(1f, 0.9f, 0.35f),
                1f
            )
        );

        ParticleSystem.EmissionModule emission = sparks.emission;
        emission.rateOverTime = 28f;

        ParticleSystem.ShapeModule shape = sparks.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        ParticleSystemRenderer renderer = sparks.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2f;
        renderer.sharedMaterial = material;
        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder;

        sparks.Play();
        return sparks;
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
        activeAttackDamageMultiplier = nextTurnAttackDamageMultiplier;
        nextTurnAttackDamageMultiplier = 1;
        drawLocked = false;
        isPlayerTurn = true;

        if (pendingCopiesForNextTurn.Count > 0)
        {
            AddCardsToHandGA addCopiesGA = new(pendingCopiesForNextTurn);
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
                if (BattleSystem.Instance != null && BattleSystem.Instance.IsBattleOver)
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
            DrawCardsGA drawCardsGA = new(action.DrawAmount);
            drawCardsGA.CardCaster = action.CardCaster;
            drawCardsGA.SourceCard = action.SourceCard;

            ActionSystem.Instance.AddReaction(drawCardsGA);
        }

        if (action.BlockAmount > 0 && action.CardCaster != null)
        {
            AddStatusEffectGA blockGA = new(
                StatusEffectType.ARMOR,
                action.BlockAmount,
                new List<CombatantView> { action.CardCaster }
            );
            blockGA.CardCaster = action.CardCaster;
            blockGA.SourceCard = action.SourceCard;
            ActionSystem.Instance.AddReaction(blockGA);
        }
    }
    private IEnumerator ChooseCardToGrantSlyPerformer(ChooseCardToGrantSlyGA action)
    {
        if (!isPlayerTurn || hand.Count == 0)
        {
            yield break;
        }

        if (cardSelectionUI == null)
        {
            Debug.LogError("Card Selection UI is not assigned in CardSystem.");
            yield break;
        }

        Card selectedCard = null;
        List<Card> choices = new(hand);

        bool opened = cardSelectionUI.Open(
            choices,
            "Choose a card to gain Sly this turn",
            card => selectedCard = card
        );

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

        selectedCard.GrantTemporarySly();
        temporarySlyCards.Add(selectedCard);
    }

    private IEnumerator PocketedDoublePerformer(PocketedDoubleGA action)
    {
        if (!isPlayerTurn || hand.Count == 0 || action.CopyAmount <= 0)
        {
            yield break;
        }

        if (cardSelectionUI == null)
        {
            Debug.LogError("Card Selection UI is not assigned in CardSystem.");
            yield break;
        }

        Card selectedCard = null;
        List<Card> choices = new(hand);

        bool opened = cardSelectionUI.Open(
            choices,
            "Choose a card to copy next turn",
            card => selectedCard = card
        );

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

        for (int i = 0; i < action.CopyAmount; i++)
        {
            pendingCopiesForNextTurn.Add(selectedCard.CreateCombatCopy());
        }
    }

    private IEnumerator AddCardsToHandPerformer(AddCardsToHandGA action)
    {
        if (!isPlayerTurn || action.Cards == null)
        {
            yield break;
        }

        foreach (Card card in action.Cards)
        {
            if (BattleSystem.Instance != null && BattleSystem.Instance.IsBattleOver)
            {
                yield break;
            }

            if (card == null)
            {
                continue;
            }

            hand.Add(card);

            CardView cardView = CardViewCreator.Instance.CreateCardView(
                card,
                drawPilePoint.position,
                drawPilePoint.rotation
            );

            yield return handView.AddCard(cardView);
        }
    }

    private void ResetTemporarySlyCards()
    {
        foreach (Card card in temporarySlyCards)
        {
            card.ClearTemporarySly();
        }

        temporarySlyCards.Clear();
    }

    // Select a card type to upgrade during this battle.
    public void ChooseCardToUpgrade(CardView cardView)
    {
        if (!IsChoosingCardToUpgrade || cardView == null) return;

        Card card = cardView.Card;
        if (card == null || !hand.Contains(card)) return;

        if (!card.CanUpgradeForCombat())
        {
            Debug.Log("This card cannot be upgraded.");
            return;
        }

        chosenCardViewToUpgrade = cardView;
    }

    // Upgrade matching cards without changing permanent run upgrades.
    private void UpgradeMatchingCards(IEnumerable<Card> cards, Card selectedCard)
    {
        foreach (Card card in cards)
        {
            if (card != null && card.IsSameType(selectedCard))
            {
                card.UpgradeForCombat();
            }
        }
    }

    private IEnumerator ChooseCardToUpgradePerformer(ChooseCardToUpgradeGA action)
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

        if (selectedView == null || selectedView.Card == null) yield break;

        Card selectedCard = selectedView.Card;
        if (!hand.Contains(selectedCard) || !selectedCard.CanUpgradeForCombat()) yield break;

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
    private IEnumerator RecoveredTechniquePerformer(RecoveredTechniqueGA action)
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

        Card recoveredCard = choices[Random.Range(0, choices.Count)];
        discardPile.Remove(recoveredCard);

        if (action.AdditionalManaCost > 0)
        {
            additionalManaCostsThisTurn.TryGetValue(recoveredCard, out int currentAdditionalCost);
            additionalManaCostsThisTurn[recoveredCard] = currentAdditionalCost + action.AdditionalManaCost;
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
    private IEnumerator ToxicRequisitionPerformer(ToxicRequisitionGA action)
    {
        if (!isPlayerTurn || action.Caster == null || action.Targets == null ||
            !action.Targets.Exists(target => target != null && target.CurrentHealth > 0))
        {
            yield break;
        }

        int discardLimit = Mathf.Clamp(action.MaxDiscard, 0, hand.Count);

        if (discardLimit > 0 && cardSelectionUI == null)
        {
            Debug.LogError("Card Selection UI is not assigned in CardSystem.");
            yield break;
        }

        List<Card> discardedCards = new();
        bool finished = false;

        for (int i = 0; i < discardLimit; i++)
        {
            Card selectedCard = null;
            List<Card> choices = new(hand);

            bool opened = cardSelectionUI.Open(
                choices,
                "Discard up to " + discardLimit + " cards (" + discardedCards.Count + " discarded). Done to finish.",
                card => selectedCard = card,
                onFinished: () => finished = true
            );

            if (!opened)
            {
                break;
            }

            while (!finished && selectedCard == null && cardSelectionUI != null && cardSelectionUI.IsOpen)
            {
                if (BattleSystem.Instance != null && BattleSystem.Instance.IsBattleOver)
                {
                    cardSelectionUI.Close();
                    yield break;
                }

                yield return null;
            }

            if (cardSelectionUI != null && cardSelectionUI.IsOpen)
            {
                cardSelectionUI.Close();
            }

            if (finished || selectedCard == null || !hand.Contains(selectedCard))
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
                Debug.LogError("Cannot discard card: CardView was not found in hand.");
                break;
            }

            hand.Remove(selectedCard);
            yield return DiscardCard(cardView);
            discardedCards.Add(selectedCard);
        }

        if (discardedCards.Count == 0 ||
            (BattleSystem.Instance != null && BattleSystem.Instance.IsBattleOver))
        {
            yield break;
        }

        DealDamageGA damageGA = new(
            Mathf.Max(0, action.DamagePerCard * discardedCards.Count),
            action.Targets,
            action.Caster
        );
        damageGA.CardCaster = action.CardCaster;
        damageGA.SourceCard = action.SourceCard;
        ActionSystem.Instance.AddReaction(damageGA);

        PlayDiscardedSlyCardsGA slyGA = new(discardedCards);
        slyGA.CardCaster = action.CardCaster;
        slyGA.SourceCard = action.SourceCard;
        ActionSystem.Instance.AddReaction(slyGA);
    }

    private IEnumerator GrandDisappearancePerformer(GrandDisappearanceGA action)
    {
        if (!isPlayerTurn || hand.Count < action.RequiredOtherCards)
        {
            yield break;
        }

        if (CardViewHoverSystem.Instance != null)
        {
            CardViewHoverSystem.Instance.Hide();
        }

        List<Card> cardsToDiscard = new(hand);
        List<Card> discardedCards = new();

        foreach (Card card in cardsToDiscard)
        {
            if (BattleSystem.Instance != null && BattleSystem.Instance.IsBattleOver)
            {
                yield break;
            }

            CardView cardView = handView.RemoveCard(card);

            if (cardView == null)
            {
                Debug.LogError("Cannot discard card: CardView was not found in hand.");
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

        if (discardedCards.Count > 0)
        {
            PlayDiscardedSlyCardsGA slyGA = new(discardedCards);
            slyGA.CardCaster = action.CardCaster;
            slyGA.SourceCard = action.SourceCard;
            ActionSystem.Instance.AddReaction(slyGA);
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

        List<Card> cardsToDiscard = new(hand);
        List<Card> discardedCards = new();

        foreach (Card card in cardsToDiscard)
        {
            if (BattleSystem.Instance != null && BattleSystem.Instance.IsBattleOver)
            {
                yield break;
            }

            CardView cardView = handView.RemoveCard(card);

            if (cardView == null)
            {
                Debug.LogError("Cannot discard card: CardView was not found in hand.");
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

        PlayDiscardedSlyCardsGA slyGA = new(discardedCards);
        slyGA.CardCaster = action.CardCaster;
        slyGA.SourceCard = action.SourceCard;
        ActionSystem.Instance.AddReaction(slyGA);

        DrawCardsGA drawCardsGA = new(discardedCards.Count);
        drawCardsGA.CardCaster = action.CardCaster;
        drawCardsGA.SourceCard = action.SourceCard;
        ActionSystem.Instance.AddReaction(drawCardsGA);
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
    private IEnumerator NextCardFreePerformer(NextCardFreeGA action)
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
        ResetTemporarySlyCards();
        nextCardFree = false;
        additionalManaCostsThisTurn.Clear();
        isPlayerTurn = false;
    }
    private IEnumerator ExhaustCardsForComboPerformer(
    ExhaustCardsForComboGA action
)
    {
        action.ActualExhausted = 0;

        if (!isPlayerTurn ||
            action.CardCaster == null ||
            action.CardCaster.CurrentHealth <= 0)
        {
            yield break;
        }

        int exhaustLimit = Mathf.Clamp(action.MaxExhaust, 0, hand.Count);

        if (exhaustLimit == 0)
        {
            yield break;
        }

        if (cardSelectionUI == null)
        {
            Debug.LogError("Card Selection UI is not assigned in CardSystem.");
            yield break;
        }

        bool finished = false;

        for (int i = 0; i < exhaustLimit; i++)
        {
            Card selectedCard = null;

            bool opened = cardSelectionUI.Open(
                new List<Card>(hand),
                "Exhaust up to " + exhaustLimit + " cards (" +
                    action.ActualExhausted + " exhausted). Done to finish.",
                card => selectedCard = card,
                onFinished: () => finished = true
            );

            if (!opened)
            {
                break;
            }

            while (!finished &&
                   selectedCard == null &&
                   cardSelectionUI != null &&
                   cardSelectionUI.IsOpen)
            {
                if (BattleSystem.Instance != null &&
                    BattleSystem.Instance.IsBattleOver)
                {
                    cardSelectionUI.Close();
                    yield break;
                }

                yield return null;
            }

            if (cardSelectionUI != null && cardSelectionUI.IsOpen)
            {
                cardSelectionUI.Close();
            }

            if (finished ||
                selectedCard == null ||
                !hand.Contains(selectedCard))
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

        if (action.ActualExhausted <= 0 ||
            action.ComboPerCard <= 0 ||
            (BattleSystem.Instance != null &&
             BattleSystem.Instance.IsBattleOver))
        {
            yield break;
        }

        AddStatusEffectGA gain = new(
            StatusEffectType.COMBO,
            action.ActualExhausted * action.ComboPerCard,
            new List<CombatantView> { action.CardCaster }
        );

        gain.CardCaster = action.CardCaster;
        gain.SourceCard = action.SourceCard;

        ActionSystem.Instance.AddReaction(gain);
    }
}
