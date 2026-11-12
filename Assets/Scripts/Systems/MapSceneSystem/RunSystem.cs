using UnityEngine;
using System.Collections.Generic;

public class RunSystem : PersistentSingleton<RunSystem>
{
    [SerializeField] private HeroData startingHero;
    [SerializeField] private ActData[] acts;

    public int CurrentActIndex { get; private set; }

    public ActData CurrentAct
    {
        get
        {
            if (acts == null) return null;

            if (CurrentActIndex < 0 || CurrentActIndex >= acts.Length)
            {
                return null;
            }

            return acts[CurrentActIndex];
        }
    }
    public HeroData HeroData { get; private set; }
    public List<CardData> Deck { get; private set; }
    private readonly HashSet<CardData> upgradedCards = new();
    public int CurrentHealth { get; private set; }
    public int Gold { get; private set; }
    public int HealthPotionCount { get; private set; }
    public int ManaPotionCount { get; private set; }
    public int CurrentStage { get; private set; }
    public EncounterData CurrentEncounter { get; private set; }
    public bool RewardClaimed { get; private set; }
    public float ElapsedRunTime { get; private set; }
    public List<MapPoint> MapPoints { get; private set; } = new();
    private readonly List<CardData> runtimeUpgradedCards = new();

    public int CompletedMapPointId { get; private set; } = -1;

    public int SelectedMapPointId { get; private set; } = -1;

    private bool isRunTimerRunning;
    public RunResult Result { get; private set; }

    public bool IsRunFinished => Result != RunResult.None;

    public bool IsLastAct =>
        acts != null &&
        acts.Length > 0 &&
        CurrentAct != null &&
        CurrentActIndex == acts.Length - 1;

    public bool CanMoveToNextAct =>
        acts != null &&
        CurrentAct != null &&
        CurrentActIndex + 1 < acts.Length &&
        acts[CurrentActIndex + 1] != null;

    public void FinishRun(RunResult result)
    {
        if (result == RunResult.None || IsRunFinished)
        {
            return;
        }

        Result = result;
        isRunTimerRunning = false;
    }

    protected override void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        base.Awake();
        StartNewRun();
    }
    public void StartNewRun()
    {
        Result = RunResult.None;
        foreach (CardData cardData in runtimeUpgradedCards)
        {
            if (cardData != null) Destroy(cardData);
        }
        runtimeUpgradedCards.Clear();
        CurrentActIndex = 0;
        HeroData = startingHero;
        Deck = new List<CardData>(startingHero.Deck);
        CurrentHealth = startingHero.Health;
        Gold = 200;
        HealthPotionCount = 0;
        ManaPotionCount = 0;
        CurrentStage = 0;
        CurrentEncounter = null;
        RewardClaimed = false;
        ElapsedRunTime = 0f;
        isRunTimerRunning = false;
        upgradedCards.Clear();
        MapPoints.Clear();
        CompletedMapPointId = -1;
        SelectedMapPointId = -1;
    }
    public void StartNewRun(HeroData selectedHero)
    {
        if (selectedHero == null)
        {
            Debug.LogError("Selected HeroData is missing.");
            return;
        }

        startingHero = selectedHero;
        StartNewRun();
    }

    public void StartRunTimer()
    {
        if (IsRunFinished) return;

        if (HeroData == null)
        {
            Debug.LogError(
                "Cannot start the run timer before a hero has been selected."
            );
            return;
        }

        isRunTimerRunning = true;
    }

    private void Update()
    {
        if (isRunTimerRunning)
        {
            ElapsedRunTime += Time.unscaledDeltaTime;
        }
    }

    public void SetEncounter(EncounterData encounter)
    {
        CurrentEncounter = encounter;
    }

    public void SaveHealth(int health)
    {
        CurrentHealth = health;
    }
    public void ClaimReward(int goldAmount, CardData card)
    {
        if (RewardClaimed) return;
        Gold += goldAmount;
        if(card != null)
        {
            Deck.Add(card);
        }
        RewardClaimed = true;
    }
    public void CompleteStage()
    {
        if (!RewardClaimed) return;
        if (SelectedMapPointId < 0) return;

        CompletedMapPointId = SelectedMapPointId;
        SelectedMapPointId = -1;

        CurrentStage++;
        CurrentEncounter = null;
        RewardClaimed = false;
    }
    public bool MoveToNextAct()
    {
        if (acts == null || acts.Length == 0)
        {
            Debug.LogError("No Acts are assigned in RunSystem.");
            return false;
        }

        int nextActIndex = CurrentActIndex + 1;

        if (nextActIndex >= acts.Length)
        {
            return false;
        }

        if (acts[nextActIndex] == null)
        {
            Debug.LogError(
                "Act at index " + nextActIndex + " is not assigned in RunSystem."
            );

            return false;
        }

        CurrentActIndex = nextActIndex;

        MapPoints.Clear();

        CompletedMapPointId = -1;
        SelectedMapPointId = -1;

        CurrentStage = 0;
        CurrentEncounter = null;
        RewardClaimed = false;

        return true;
    }
    public bool BuyCard(CardData card, int price)
    {
        if (card == null) return false;
        if (price < 0) return false;
        if (Gold < price) return false;

        Gold -= price;
        Deck.Add(card);

        return true;
    }
    public bool RemoveCardFromDeck(int cardIndex, int price)
    {
        if (price < 0)
        {
            return false;
        }

        if (Deck == null)
        {
            return false;
        }

        if (cardIndex < 0 || cardIndex >= Deck.Count)
        {
            return false;
        }

        if (Gold < price)
        {
            return false;
        }

        Gold -= price;
        Deck.RemoveAt(cardIndex);

        return true;
    }
    public bool BuyHealthPotion(int price)
    {
        if (price < 0) return false;

        if (Gold < price) return false;

        Gold -= price;
        HealthPotionCount++;

        return true;
    }

    public bool BuyManaPotion(int price)
    {
        if (price < 0) return false;

        if (Gold < price) return false;

        Gold -= price;
        ManaPotionCount++;

        return true;
    }
    public bool TryConsumeHealthPotion()
    {
        if (HealthPotionCount <= 0)
        {
            return false;
        }

        HealthPotionCount--;

        return true;
    }

    public bool TryConsumeManaPotion()
    {
        if (ManaPotionCount <= 0)
        {
            return false;
        }

        ManaPotionCount--;

        return true;
    }

    public void Heal(int amount)
    {
        CurrentHealth += amount;

        if (CurrentHealth > HeroData.Health)
        {
            CurrentHealth = HeroData.Health;
        }
    }

    public bool UpgradeCard(int cardIndex, CardData upgradedCard)
    {
        if (cardIndex < 0 || cardIndex >= Deck.Count) return false;
        if (upgradedCard == null) return false;

        Deck[cardIndex] = upgradedCard;

        return true;
    }

    public void CompleteMapPoint()
    {
        if (SelectedMapPointId < 0) return;

        CompletedMapPointId = SelectedMapPointId;
        SelectedMapPointId = -1;

        CurrentStage++;
        CurrentEncounter = null;
    }
    public bool IsCardUpgraded(CardData cardData)
    {
        return upgradedCards.Contains(cardData);
    }

    public bool UpgradeCard(CardData cardData)
    {
        if (cardData == null || Deck == null) return false;

        int cardIndex = Deck.IndexOf(cardData);

        return UpgradeDeckCard(cardIndex);
    }
    public void GenerateMap()
    {
        if (MapPoints.Count > 0) return;

        if (CurrentAct == null)
        {
            Debug.LogError(
                "Current Act is missing. Assign Acts in RunSystem."
            );

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

        if (point == null) return false;
        if (SelectedMapPointId != -1) return false;

        if (CompletedMapPointId == -1)
        {
            return point.Floor == 0;
        }

        MapPoint completedPoint = GetMapPoint(CompletedMapPointId);

        if (completedPoint == null) return false;

        return completedPoint.NextPoints.Contains(pointId);
    }
    public bool UpgradeDeckCard(int cardIndex)
    {
        if (Deck == null) return false;
        if (cardIndex < 0 || cardIndex >= Deck.Count) return false;

        CardData originalCard = Deck[cardIndex];

        if (originalCard == null) return false;
        if (!originalCard.CanUpgrade) return false;
        if (IsCardUpgraded(originalCard)) return false;

        CardData upgradedInstance = Instantiate(originalCard);
        upgradedInstance.name = originalCard.name;

        runtimeUpgradedCards.Add(upgradedInstance);
        upgradedCards.Add(upgradedInstance);

        Deck[cardIndex] = upgradedInstance;

        return true;
    }

    public void SelectMapPoint(int pointId)
    {
        SelectedMapPointId = pointId;
    }
    public bool HasCardInDeck(CardData cardData)
    {
        if (cardData == null || Deck == null) return false;

        foreach (CardData ownedCard in Deck)
        {
            if (ownedCard == null) continue;

            if (ownedCard == cardData)
            {
                return true;
            }

            if (ownedCard.Character == cardData.Character &&
                ownedCard.name == cardData.name)
            {
                return true;
            }
        }

        return false;
    }
}
