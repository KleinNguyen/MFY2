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
    public List<MapPoint> MapPoints { get; private set; } = new();

    public int CompletedMapPointId { get; private set; } = -1;

    public int SelectedMapPointId { get; private set; } = -1;
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
        CurrentActIndex = 0;
        HeroData = startingHero;
        Deck = new List<CardData>(startingHero.Deck);
        CurrentHealth = startingHero.Health;
        Gold = 0;
        HealthPotionCount = 0;
        ManaPotionCount = 0;
        CurrentStage = 0;
        CurrentEncounter = null;
        RewardClaimed = false;
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
        if (cardData == null) return false;
        if (!cardData.CanUpgrade) return false;
        if (!Deck.Contains(cardData)) return false;
        if (upgradedCards.Contains(cardData)) return false;

        upgradedCards.Add(cardData);

        return true;
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

    public void SelectMapPoint(int pointId)
    {
        SelectedMapPointId = pointId;
    }
}
