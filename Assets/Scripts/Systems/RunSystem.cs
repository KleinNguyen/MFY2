using UnityEngine;
using System.Collections.Generic;

public class RunSystem : PersistentSingleton<RunSystem>
{
    [SerializeField] private HeroData startingHero;
    public HeroData HeroData { get; private set; }
    public List<CardData> Deck { get; private set; }
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
}
