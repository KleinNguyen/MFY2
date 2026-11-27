using System.Collections;
using UnityEngine;

public class ManaSystem : Singleton<ManaSystem>
{
    [SerializeField] private ManaUI manaUI;

    private int maxMana, currentMana, turnManaLimit;
    private int bonusManaPerTurn, nextTurnBonusMana;

    public int CurrentMana => currentMana;
    public int MaxMana => turnManaLimit;

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<SpendManaGA>(SpendManaPerformer);
        ActionSystem.AttachPerformer<RefillManaGA>(RefillManaPerformer);
        ActionSystem.AttachPerformer<GainManaGA>(GainManaPerformer);
        ActionSystem.AttachPerformer<IncreaseTurnManaGA>(IncreaseTurnManaPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<SpendManaGA>();
        ActionSystem.DetachPerformer<RefillManaGA>();
        ActionSystem.DetachPerformer<GainManaGA>();
        ActionSystem.DetachPerformer<IncreaseTurnManaGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    private void Start()
    {
        if (RunSystem.Instance == null || RunSystem.Instance.HeroData == null)
        {
            Debug.LogError("Selected hero data is missing; mana cannot be initialized.");
            return;
        }

        bonusManaPerTurn = nextTurnBonusMana = 0;
        maxMana = Mathf.Max(0, RunSystem.Instance.HeroData.StartingMana);
        turnManaLimit = currentMana = maxMana;
        manaUI.UpdateManaText(currentMana);
    }

    public bool HasEnoughMana(int amount) => currentMana >= amount;

    public void RestoreMana(int amount)
    {
        if (amount <= 0 || currentMana >= MaxMana) return;

        currentMana = Mathf.Min(currentMana + amount, MaxMana);
        manaUI.UpdateManaText(currentMana);
    }

    public void SetBattleManaBonus(int amount)
    {
        // Adjust current mana by the same amount as the new battle maximum.
        int newMax = Mathf.Max(0, RunSystem.Instance.HeroData.StartingMana + Mathf.Max(0, amount));
        int difference = newMax - maxMana;

        maxMana = newMax;
        turnManaLimit = Mathf.Max(0, turnManaLimit + difference);
        currentMana = Mathf.Max(0, currentMana + difference);
        manaUI.UpdateManaText(currentMana);
    }

    public void GainMana(int amount)
    {
        if (amount <= 0) return;

        currentMana += amount;
        manaUI.UpdateManaText(currentMana);
    }

    private IEnumerator SpendManaPerformer(SpendManaGA action)
    {
        currentMana -= action.Amount;
        manaUI.UpdateManaText(currentMana);
        yield break;
    }

    private IEnumerator RefillManaPerformer(RefillManaGA action)
    {
        turnManaLimit = Mathf.Max(0, maxMana + bonusManaPerTurn + nextTurnBonusMana);
        currentMana = turnManaLimit;
        nextTurnBonusMana = 0;
        manaUI.UpdateManaText(currentMana);
        yield break;
    }

    private IEnumerator GainManaPerformer(GainManaGA action)
    {
        GainMana(action.Amount);
        yield break;
    }

    private IEnumerator IncreaseTurnManaPerformer(IncreaseTurnManaGA action)
    {
        if (action.Amount > 0)
        {
            if (action.NextTurnOnly) nextTurnBonusMana += action.Amount;
            else bonusManaPerTurn += action.Amount;
        }
        yield break;
    }

    private void EnemyTurnPostReaction(EnemyTurnGA action) => ActionSystem.Instance.AddReaction(new RefillManaGA());
}