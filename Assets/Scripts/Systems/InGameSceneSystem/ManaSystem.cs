using System.Collections;
using UnityEngine;

public class ManaSystem : Singleton<ManaSystem>
{
    [SerializeField] private ManaUI manaUI;
    private int maxMana;
    private int currentMana;
    public int CurrentMana => currentMana;
    public int MaxMana => maxMana;
    private void OnEnable()
    {
        ActionSystem.AttachPerformer<SpendManaGA>(SpendManaPerformer);
        ActionSystem.AttachPerformer<RefillManaGA>(RefillManaPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }
    private void OnDisable()
    {
        ActionSystem.DetachPerformer<SpendManaGA>();
        ActionSystem.DetachPerformer<RefillManaGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);

    }
    public bool HasEnoughMana(int mana)
    {
        return currentMana >= mana;
    }
    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;

        currentMana = Mathf.Min(
            currentMana + amount,
            maxMana
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
        currentMana = maxMana;
        manaUI.UpdateManaText(currentMana);
        yield return null;
    }
    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        RefillManaGA refillManaGA = new();
        ActionSystem.Instance.AddReaction(refillManaGA);
    }
}
