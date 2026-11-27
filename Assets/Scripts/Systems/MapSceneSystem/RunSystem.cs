using System.Collections.Generic;
using UnityEngine;

public partial class RunSystem : PersistentSingleton<RunSystem>
{
    private const int StartingGold = 200;
    private const int StartingCardRemovalPrice = 75;
    private const int WheelSliceCount = 6;
    private const int WheelOutcomeCount = 3;

    [SerializeField] private HeroData startingHero;
    [SerializeField] private ActData[] acts;

    // Permanent upgrades unlocked during the current run.
    private readonly HashSet<string> upgradedCardIds = new();
    private readonly List<CardData> preparedRewardChoices = new();
    private bool isRunTimerRunning;

    // Hero and inventory
    public HeroData HeroData { get; private set; }
    public List<CardData> Deck { get; private set; }
    public int CurrentHealth { get; private set; }
    public int Gold { get; private set; }
    public int CardRemovalPrice { get; private set; }
    public int HealthPotionCount { get; private set; }
    public int ManaPotionCount { get; private set; }

    // Run and map
    public RunResult Result { get; private set; }
    public float ElapsedRunTime { get; private set; }
    public int CurrentActIndex { get; private set; }
    public int CurrentStage { get; private set; }
    public List<MapPoint> MapPoints { get; private set; } = new();
    public int CompletedMapPointId { get; private set; } = -1;
    public int SelectedMapPointId { get; private set; } = -1;
    public EncounterData CurrentEncounter { get; private set; }
    public EncounterData LastNormalEncounter { get; private set; }
    public EncounterData LastEliteEncounter { get; private set; }

    // Rewards
    public IReadOnlyList<CardData> RewardChoices => preparedRewardChoices;
    public bool RewardClaimed { get; private set; }
    public bool RewardsPrepared { get; private set; }
    public int PendingRewardGold { get; private set; }
    public int RewardWheelResult { get; private set; } = -1;

    public ActData CurrentAct
    {
        get
        {
            if (acts == null || CurrentActIndex < 0 || CurrentActIndex >= acts.Length) return null;

            return acts[CurrentActIndex];
        }
    }

    public bool IsRunFinished
    {
        get
        {
            return Result != RunResult.None;
        }
    }

    public bool IsLastAct
    {
        get
        {
            if (CurrentAct == null) return false;

            return CurrentActIndex == acts.Length - 1;
        }
    }

    public bool CanMoveToNextAct
    {
        get
        {
            if (CurrentAct == null) return false;

            int nextActIndex = CurrentActIndex + 1;
            if (nextActIndex >= acts.Length) return false;

            return acts[nextActIndex] != null;
        }
    }

    // Unity
    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        base.Awake();
        StartNewRun();
    }

    private void Update()
    {
        if (isRunTimerRunning && !NewRunTutorialUI.IsOpen)
        {
            ElapsedRunTime += Time.unscaledDeltaTime;
        }
    }

    // Run lifecycle
    public void StartNewRun()
    {
        if (startingHero == null || startingHero.Deck == null)
        {
            Debug.LogError("Assign a starting hero and starting deck in RunSystem.", this);
            return;
        }

        ClearRuntimeUpgrades();
        HeroData = startingHero;
        Deck = new List<CardData>(HeroData.Deck);
        CurrentHealth = HeroData.Health;
        Gold = StartingGold;
        CardRemovalPrice = StartingCardRemovalPrice;
        HealthPotionCount = 0;
        ManaPotionCount = 0;
        CurrentActIndex = 0;
        Result = RunResult.None;
        ElapsedRunTime = 0f;
        isRunTimerRunning = false;
        ResetMapProgress();
        ResetRewards();
    }

    public void StartNewRun(HeroData selectedHero)
    {
        if (selectedHero == null || selectedHero.Deck == null)
        {
            Debug.LogError("Selected hero data or deck is missing.");
            return;
        }

        startingHero = selectedHero;
        StartNewRun();
        RunSaveSystem.Delete();
    }

    public void StartRunTimer()
    {
        if (IsRunFinished) return;

        if (HeroData == null)
        {
            Debug.LogError("Cannot start the run timer before a hero has been selected.");
            return;
        }

        isRunTimerRunning = true;
    }

    public void FinishRun(RunResult result)
    {
        if (result == RunResult.None || IsRunFinished) return;

        Result = result;
        isRunTimerRunning = false;
        RunSaveSystem.Delete();
    }

    // Act and map
    public bool MoveToNextAct()
    {
        if (acts == null || acts.Length == 0)
        {
            Debug.LogError("No Acts are assigned in RunSystem.");
            return false;
        }

        int nextActIndex = CurrentActIndex + 1;
        if (nextActIndex >= acts.Length) return false;

        if (acts[nextActIndex] == null)
        {
            Debug.LogError("Act at index " + nextActIndex + " is not assigned in RunSystem.");
            return false;
        }

        CurrentActIndex = nextActIndex;
        CurrentHealth = HeroData.Health;
        ResetMapProgress();
        ResetRewards();
        GenerateMap();
        SaveCheckpoint();
        ActTransitionBanner.RequestCurrentActIntro();
        return true;
    }

    public void GenerateMap()
    {
        if (MapPoints.Count > 0) return;

        if (CurrentAct == null)
        {
            Debug.LogError("Current Act is missing. Assign Acts in RunSystem.");
            return;
        }

        MapPoints = MapGeneratorSystem.GenerateMap(CurrentAct);
    }

    public MapPoint GetMapPoint(int pointId)
    {
        foreach (MapPoint point in MapPoints)
        {
            if (point.Id == pointId) return point;
        }

        return null;
    }

    public bool CanSelectMapPoint(int pointId)
    {
        if (IsRunFinished) return false;

        MapPoint point = GetMapPoint(pointId);
        if (point == null || SelectedMapPointId != -1) return false;
        if (CompletedMapPointId == -1) return point.Floor == 0;

        MapPoint completedPoint = GetMapPoint(CompletedMapPointId);
        return completedPoint != null && completedPoint.NextPoints.Contains(pointId);
    }

    public void SelectMapPoint(int pointId)
    {
        SaveCheckpoint();
        SelectedMapPointId = pointId;
    }

    public void SetEncounter(EncounterData encounter)
    {
        CurrentEncounter = encounter;
        if (encounter == null) return;

        if (encounter.Type == EncounterType.Normal) LastNormalEncounter = encounter;
        else if (encounter.Type == EncounterType.Elite) LastEliteEncounter = encounter;
    }

    public void CompleteStage()
    {
        if (!RewardClaimed || SelectedMapPointId < 0) return;

        CompleteMapPoint();
        ResetRewards();
    }

    public void CompleteMapPoint()
    {
        if (SelectedMapPointId < 0) return;

        CompletedMapPointId = SelectedMapPointId;
        SelectedMapPointId = -1;
        CurrentStage++;
        CurrentEncounter = null;
        battleStartSave = null;
        SaveCheckpoint();
    }

    // Rewards
    public void PrepareRewards(int gold, List<CardData> choices)
    {
        if (RewardsPrepared || RewardClaimed || IsRunFinished || choices == null) return;

        preparedRewardChoices.Clear();
        preparedRewardChoices.AddRange(choices);
        PendingRewardGold = Mathf.Max(0, gold);
        RewardWheelResult = -1;
        RewardsPrepared = true;
    }

    public bool ApplyRewardWheel(int result, int cardIndex = -1, CardData replacement = null)
    {
        if (!RewardsPrepared || RewardClaimed || IsRunFinished ||
            RewardWheelResult >= 0 || result < 0 || result >= WheelSliceCount)
            return false;

        RewardWheelResult = result;

        if (result % WheelOutcomeCount == 0)
        {
            PendingRewardGold *= 2;
        }
        else if (result % WheelOutcomeCount == 1)
        {
            ReplaceRewardCard(cardIndex, replacement);
        }
        else
        {
            PendingRewardGold = 0;
            preparedRewardChoices.Clear();
            RewardClaimed = true;
        }

        return true;
    }

    public void ClaimReward(int goldAmount, CardData card)
    {
        if (RewardClaimed || IsRunFinished || Deck == null) return;
        if (RewardsPrepared && card != null && !preparedRewardChoices.Contains(card)) return;

        Gold += RewardsPrepared ? PendingRewardGold : Mathf.Max(0, goldAmount);
        if (card != null) Deck.Add(card);
        RewardClaimed = true;
    }

    // Health
    public void SaveHealth(int health) => CurrentHealth = health;

    public void Heal(int amount)
    {
        CurrentHealth += amount;
        if (CurrentHealth > HeroData.Health) CurrentHealth = HeroData.Health;
    }

    // Shop and potions
    public bool BuyCard(CardData card, int price)
    {
        if (card == null || price < 0 || Gold < price) return false;

        Gold -= price;
        Deck.Add(card);
        return true;
    }

    public bool RemoveCardFromDeck(int cardIndex, int price)
    {
        if (IsRunFinished || price < 0 || price != CardRemovalPrice || Deck == null ||
            cardIndex < 0 || cardIndex >= Deck.Count || Gold < price) return false;

        Gold -= price;
        Deck.RemoveAt(cardIndex);

        // Increase by 20%, rounded up, without floating-point rounding errors.
        CardRemovalPrice = (int)System.Math.Min(int.MaxValue, ((long)price * 120 + 99) / 100);
        return true;
    }

    public bool BuyHealthPotion(int price)
    {
        if (price < 0 || Gold < price) return false;

        Gold -= price;
        HealthPotionCount++;
        return true;
    }

    public bool BuyManaPotion(int price)
    {
        if (price < 0 || Gold < price) return false;

        Gold -= price;
        ManaPotionCount++;
        return true;
    }

    public bool TryConsumeHealthPotion()
    {
        if (HealthPotionCount <= 0) return false;

        HealthPotionCount--;
        return true;
    }

    public bool TryConsumeManaPotion()
    {
        if (ManaPotionCount <= 0) return false;

        ManaPotionCount--;
        return true;
    }

    // Deck
    public bool HasCardInDeck(CardData cardData)
    {
        if (cardData == null || Deck == null) return false;

        foreach (CardData ownedCard in Deck)
        {
            if (ownedCard == null) continue;

            // Treat matching character/name assets as the same card even if references differ.
            if (ownedCard == cardData ||
                (ownedCard.Character == cardData.Character &&
                 ownedCard.name == cardData.name))
                return true;
        }

        return false;
    }

    public bool IsCardUpgraded(CardData cardData)
    {
        if (cardData == null || !cardData.CanUpgrade) return false;
        if (string.IsNullOrWhiteSpace(cardData.SaveId)) return false;

        return upgradedCardIds.Contains(cardData.SaveId);
    }

    public bool UpgradeCard(int cardIndex, CardData upgradedCard)
    {
        if (Deck == null || cardIndex < 0 || cardIndex >= Deck.Count || upgradedCard == null) return false;

        Deck[cardIndex] = upgradedCard;
        return true;
    }

    public bool UpgradeCard(CardData cardData)
    {
        if (cardData == null || Deck == null) return false;

        int cardIndex = Deck.IndexOf(cardData);
        return UpgradeDeckCard(cardIndex);
    }

    public bool UpgradeDeckCard(int cardIndex)
    {
        if (IsRunFinished || Deck == null || cardIndex < 0 || cardIndex >= Deck.Count) return false;

        CardData card = Deck[cardIndex];
        if (card == null || !card.CanUpgrade || IsCardUpgraded(card)) return false;

        if (string.IsNullOrWhiteSpace(card.SaveId))
        {
            Debug.LogError("Card SaveId is missing. Rebuild Save Catalog before upgrading.");
            return false;
        }
        return upgradedCardIds.Add(card.SaveId);
    }

    // Internal helpers
    private void ClearRuntimeUpgrades()
    {
        upgradedCardIds.Clear();
    }

    private void ResetMapProgress()
    {
        battleStartSave = null;
        MapPoints.Clear();
        CompletedMapPointId = -1;
        SelectedMapPointId = -1;
        CurrentStage = 0;
        CurrentEncounter = null;
        LastNormalEncounter = null;
        LastEliteEncounter = null;
    }

    private void ReplaceRewardCard(int cardIndex, CardData replacement)
    {
        if (HeroData == null || replacement == null ||
            cardIndex < 0 || cardIndex >= preparedRewardChoices.Count)
            return;
        if (replacement.Character != HeroData.Character || preparedRewardChoices.Contains(replacement)) return;

        CardData original = preparedRewardChoices[cardIndex];
        if (original != null && IsHigherRarity(original.Rarity, replacement.Rarity))
            preparedRewardChoices[cardIndex] = replacement;
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

    private void ResetRewards()
    {
        preparedRewardChoices.Clear();
        RewardClaimed = false;
        RewardsPrepared = false;
        PendingRewardGold = 0;
        RewardWheelResult = -1;
    }
}