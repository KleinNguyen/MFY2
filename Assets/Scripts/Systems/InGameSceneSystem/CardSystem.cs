using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CardSystem : Singleton<CardSystem>
{
    private const float DiscardFlightDuration = 0.40f;
    private const float EndTurnDiscardDuration = 0.38f;
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

    private readonly Stack<Card> priorityDrawCards = new Stack<Card>();

    private readonly List<Card> drawPile = new List<Card>();
    private readonly List<Card> discardPile = new List<Card>();
    private readonly List<Card> hand = new List<Card>();
    private readonly List<Card> exhaustPile = new List<Card>();

    private readonly Dictionary<Card, int> additionalManaCostsThisTurn =
        new Dictionary<Card, int>();

    private readonly List<Card> pendingCopiesForNextTurn =
        new List<Card>();

    private CardView chosenCardViewToUpgrade;
    private Card chosenCardToExhaust;

    private bool drawLocked;
    private bool isPlayerTurn = true;
    private bool nextCardFree;

    private int activeAttackDamageMultiplier = 1;
    private int nextTurnAttackDamageMultiplier = 1;

    public int CardsDrawnThisCombat { get; private set; }

    public ComboSystem Combo { get; private set; }

    public bool IsChoosingCardToExhaust { get; private set; }

    public bool IsChoosingCardToUpgrade { get; private set; }

    public bool IsViewingPile
    {
        get
        {
            if (cardSelectionUI == null)
            {
                return false;
            }

            return cardSelectionUI.IsOpen &&
                   cardSelectionUI.IsReadOnly;
        }
    }

    public int AttackDamageMultiplier
    {
        get
        {
            if (!isPlayerTurn)
            {
                return 1;
            }

            return activeAttackDamageMultiplier;
        }
    }

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<DrawCardsGA>(
            DrawCardsPerformer
        );

        ActionSystem.AttachPerformer<DiscardAllCardsGA>(
            DiscardAllCardsPerformer
        );

        ActionSystem.AttachPerformer<PlayCardGA>(
            PlayCardPerformer
        );

        ActionSystem.AttachPerformer<ExhaustRandomCardGA>(
            ExhaustRandomCardPerformer
        );

        ActionSystem.AttachPerformer<FocusedStudyGA>(
            FocusedStudyPerformer
        );

        ActionSystem.AttachPerformer<LockDrawGA>(
            LockDrawPerformer
        );

        ActionSystem.AttachPerformer<ExhaustHandAndDamageGA>(
            ExhaustHandAndDamagePerformer
        );

        ActionSystem.SubscribeReaction<EnemyTurnGA>(
            EnemyTurnPostReaction,
            ReactionTiming.POST
        );

        ActionSystem.AttachPerformer<ChooseCardToExhaustGA>(
            ChooseCardToExhaustPerformer
        );

        ActionSystem.AttachPerformer<PocketedDoubleGA>(
            PocketedDoublePerformer
        );

        ActionSystem.AttachPerformer<AddCardsToHandGA>(
            AddCardsToHandPerformer
        );

        ActionSystem.AttachPerformer<ChooseCardToUpgradeGA>(
            ChooseCardToUpgradePerformer
        );

        ActionSystem.AttachPerformer<ReclaimingBlowGA>(
            ReclaimingBlowPerformer
        );

        ActionSystem.AttachPerformer<RecoverDiscardCardGA>(
            RecoverDiscardCardPerformer
        );

        ActionSystem.AttachPerformer<RecoveredTechniqueGA>(
            RecoveredTechniquePerformer
        );

        ActionSystem.AttachPerformer<DiscardCardsGA>(
            DiscardCardsPerformer
        );

        ActionSystem.AttachPerformer<ToxicRequisitionGA>(
            ToxicRequisitionPerformer
        );

        ActionSystem.AttachPerformer<CleanSweepGA>(
            CleanSweepPerformer
        );

        ActionSystem.AttachPerformer<GrandDisappearanceGA>(
            GrandDisappearancePerformer
        );

        ActionSystem.AttachPerformer<NextCardFreeGA>(
            NextCardFreePerformer
        );

        ActionSystem.SubscribeReaction<EnemyTurnGA>(
            EnemyTurnPreReaction,
            ReactionTiming.PRE
        );

        ActionSystem.AttachPerformer<ExhaustCardsForComboGA>(
            ExhaustCardsForComboPerformer
        );

        Combo = GetComponent<ComboSystem>();

        if (Combo == null)
        {
            Combo = gameObject.AddComponent<ComboSystem>();
        }

        Combo.enabled = true;
    }

    private void OnDisable()
    {
        if (Combo != null)
        {
            Combo.enabled = false;
        }

        activeAttackDamageMultiplier = 1;
        nextTurnAttackDamageMultiplier = 1;
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
        ActionSystem.DetachPerformer<PocketedDoubleGA>();
        ActionSystem.DetachPerformer<AddCardsToHandGA>();
        ActionSystem.DetachPerformer<ChooseCardToUpgradeGA>();
        ActionSystem.DetachPerformer<ReclaimingBlowGA>();
        ActionSystem.DetachPerformer<RecoverDiscardCardGA>();
        ActionSystem.DetachPerformer<RecoveredTechniqueGA>();
        ActionSystem.DetachPerformer<DiscardCardsGA>();
        ActionSystem.DetachPerformer<ToxicRequisitionGA>();
        ActionSystem.DetachPerformer<CleanSweepGA>();
        ActionSystem.DetachPerformer<GrandDisappearanceGA>();
        ActionSystem.DetachPerformer<NextCardFreeGA>();
        ActionSystem.DetachPerformer<ExhaustCardsForComboGA>();

        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(
            EnemyTurnPostReaction,
            ReactionTiming.POST
        );

        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(
            EnemyTurnPreReaction,
            ReactionTiming.PRE
        );
    }

    public void Setup(List<CardData> deckData)
    {
        if (Combo != null)
        {
            Combo.ResetCombat();
        }

        activeAttackDamageMultiplier = 1;
        nextTurnAttackDamageMultiplier = 1;
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

        foreach (CardData cardData in deckData)
        {
            Card card = new Card(cardData);
            drawPile.Add(card);
        }

        pileUI.UpdateDrawPileText(drawPile.Count);
        pileUI.UpdateDiscardPileText(discardPile.Count);
    }







    public bool CanPlayCard(Card card)
    {
        return card != null &&
               !card.IsUnplayable &&
               isPlayerTurn &&
               hand.Contains(card) &&
               BattleSystem.Instance != null &&
               !BattleSystem.Instance.IsBattleOver &&
               !BattleSystem.Instance.IsPreparingBattle &&
               MeetsCardPlayConditions(card) &&
               ManaSystem.Instance != null &&
               ManaSystem.Instance.HasEnoughMana(GetManaCost(card));
    }

    public bool IsCardInHand(Card card)
    {
        return card != null && hand.Contains(card);
    }

    private bool MeetsCardPlayConditions(Card card)
    {
        int otherCardsInHand = hand.Count;

        if (hand.Contains(card))
        {
            otherCardsInHand--;
        }

        if (
            card.ManualTargetEffect is ICardPlayCondition manualCondition &&
            !manualCondition.CanPlay(otherCardsInHand)
        )
        {
            return false;
        }

        if (card.OtherEffects != null)
        {
            foreach (AutoTargetEffect effectWrapper in card.OtherEffects)
            {
                if (
                    effectWrapper != null &&
                    effectWrapper.Effect is ICardPlayCondition condition &&
                    !condition.CanPlay(otherCardsInHand)
                )
                {
                    return false;
                }
            }
        }

        return true;
    }

    public int GetManaCost(Card card)
    {
        if (card == null)
        {
            return 0;
        }

        if (nextCardFree && isPlayerTurn && hand.Contains(card))
        {
            return 0;
        }

        additionalManaCostsThisTurn.TryGetValue(
            card,
            out int additionalManaCost
        );

        return Mathf.Max(
            0,
            card.Mana + additionalManaCost
        );
    }

    public void AddJunkCards(int amount, Sprite image)
    {
        if (amount <= 0)
        {
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            drawPile.Add(Card.CreateJunk(image));
        }

        pileUI.UpdateDrawPileText(drawPile.Count);
    }


































































}