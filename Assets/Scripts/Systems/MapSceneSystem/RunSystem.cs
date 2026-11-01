using UnityEngine;
using System.Collections.Generic;

public class RunSystem : PersistentSingleton<RunSystem>
{
    [SerializeField] private HeroData startingHero;
    public HeroData HeroData { get; private set; }
    public List<CardData> Deck { get; private set; }
    private readonly HashSet<CardData> upgradedCards = new();
    public int CurrentHealth { get; private set; }
    public int Gold { get; private set; }
    public int CurrentStage { get; private set; }
    public EncounterData CurrentEncounter { get; private set; }
    public bool RewardClaimed { get; private set; }
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
        HeroData = startingHero;
        Deck = new List<CardData>(startingHero.Deck);
        CurrentHealth = startingHero.Health;
        Gold = 0;
        CurrentStage = 0;
        CurrentEncounter = null;
        RewardClaimed = false;
        upgradedCards.Clear();
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
        CurrentStage++;
        CurrentEncounter = null;
        RewardClaimed = false;
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
}
