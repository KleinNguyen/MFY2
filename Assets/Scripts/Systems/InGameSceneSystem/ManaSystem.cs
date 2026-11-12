using System.Collections;
using UnityEngine;

public class ManaSystem : Singleton<ManaSystem>
{
    [SerializeField] private ManaUI manaUI;
    private int maxMana;
    private int currentMana;
    public int CurrentMana => currentMana;
    private int bonusManaPerTurn;
    private int nextTurnBonusMana;
    public int MaxMana => maxMana;
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
    public bool HasEnoughMana(int mana)
    {
        return currentMana >= mana;
    }
    public void RestoreMana(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        int restoreLimit = Mathf.Max(maxMana, currentMana);

        currentMana = Mathf.Min(
            currentMana + amount,
            restoreLimit
        );

        manaUI.UpdateManaText(currentMana);
    }
    private void Start()
    {
        if (RunSystem.Instance == null || RunSystem.Instance.HeroData == null)
        {
            Debug.LogError("Selected hero data is missing; mana cannot be initialized.");
            return;
        }

        maxMana = RunSystem.Instance.HeroData.StartingMana;
        currentMana = maxMana;
        manaUI.UpdateManaText(currentMana);
    }
    private IEnumerator SpendManaPerformer(SpendManaGA spendManaGA)
    {
        currentMana -= spendManaGA.Amount;
        manaUI.UpdateManaText(currentMana);
        yield return null;
    }
    private IEnumerator RefillManaPerformer(RefillManaGA refillManaGA)
    {
        currentMana = maxMana + bonusManaPerTurn + nextTurnBonusMana;
        nextTurnBonusMana = 0;

        manaUI.UpdateManaText(currentMana);

        yield return null;
    }
    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        RefillManaGA refillManaGA = new();
        ActionSystem.Instance.AddReaction(refillManaGA);
    }
    public void GainMana(int amount)
    {
        if (amount <= 0) return;

        currentMana += amount;
        manaUI.UpdateManaText(currentMana);
    }
    private IEnumerator GainManaPerformer(GainManaGA action)
    {
        GainMana(action.Amount);
        yield return null;
    }
    private IEnumerator IncreaseTurnManaPerformer(IncreaseTurnManaGA action)
    {
        if (action.Amount > 0)
        {
            if (action.NextTurnOnly)
            {
                nextTurnBonusMana += action.Amount;
            }
            else
            {
                bonusManaPerTurn += action.Amount;
            }
        }

        yield return null;
    }
}
