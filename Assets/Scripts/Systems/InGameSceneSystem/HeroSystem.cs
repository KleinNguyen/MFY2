using UnityEngine;
using System.Collections;

public class HeroSystem : Singleton<HeroSystem>
{
    [field: SerializeField] public HeroView HeroView { get; private set; }
    private void OnEnable()
    {
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.AttachPerformer<HealGA>(HealPerformer);
        ActionSystem.AttachPerformer<LoseHealthGA>(LoseHealthPerformer);
        ActionSystem.AttachPerformer<LoseHealthThenStatusGA>(LoseHealthThenStatusPerformer);
    }
    private void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.DetachPerformer<HealGA>();
        ActionSystem.DetachPerformer<LoseHealthGA>();
        ActionSystem.DetachPerformer<LoseHealthThenStatusGA>();
    }
    public void Setup(HeroData heroData)
    {
        HeroView.Setup(heroData);
    }
    private void EnemyTurnPreReaction(EnemyTurnGA enemyTurnGA)
    {
        DiscardAllCardsGA discardAllCardsGA = new();
        ActionSystem.Instance.AddReaction(discardAllCardsGA);
    }
    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        int poisonStacks = HeroView.GetStatusEffectStacks(StatusEffectType.POISON);
        if(poisonStacks > 0)
        {
            ApplyPoisonGA applyPoisonGA = new(poisonStacks, HeroView);
            ActionSystem.Instance.AddReaction(applyPoisonGA);
        }
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.AddReaction(drawCardsGA);
    }
    private IEnumerator HealPerformer(HealGA healGA)
    {
        if (healGA.Amount <= 0)
        {
            yield break;
        }

        foreach (CombatantView target in healGA.Targets)
        {
            if (target == null || target.CurrentHealth <= 0)
            {
                continue;
            }

            int newHealth = target.CurrentHealth + healGA.Amount;

            target.SetCurrentHealth(newHealth);

            if (target is HeroView && RunSystem.Instance != null)
            {
                RunSystem.Instance.SaveHealth(target.CurrentHealth);
            }

            yield return null;
        }
    }
    private IEnumerator LoseHealthPerformer(LoseHealthGA action)
    {
        if (action.Amount <= 0 || HeroView == null)
        {
            yield break;
        }

        if (HeroView.CurrentHealth <= 0)
        {
            yield break;
        }

        int newHealth = HeroView.CurrentHealth - action.Amount;

        HeroView.SetCurrentHealth(newHealth);

        if (RunSystem.Instance != null)
        {
            RunSystem.Instance.SaveHealth(HeroView.CurrentHealth);
        }

        if (HeroView.CurrentHealth <= 0 && BattleSystem.Instance != null)
        {
            BattleSystem.Instance.Defeat();
        }

        yield return null;
    }
    private IEnumerator LoseHealthThenStatusPerformer(
    LoseHealthThenStatusGA action
)
    {
        if (action.Targets == null || action.Targets.Count == 0)
        {
            yield break;
        }

        LoseHealthGA loseHealthGA = new(
            action.HealthAmount,
            true
        );

        ActionSystem.Instance.AddReaction(loseHealthGA);

        AddStatusEffectGA addStatusEffectGA = new(
            action.StatusEffectType,
            action.StackCount,
            action.Targets
        );

        ActionSystem.Instance.AddReaction(addStatusEffectGA);

        yield return null;
    }
}
