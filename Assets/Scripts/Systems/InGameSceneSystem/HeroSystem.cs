using UnityEngine;
using System.Collections;

public class HeroSystem : Singleton<HeroSystem>
{
    [field: SerializeField] public HeroView HeroView { get; private set; }
    public bool LostHealthFromCardThisTurn { get; private set; }
    private int adversityPower;

    private bool adversityTriggeredThisTurn;

    private int temporaryAdversityStats;
    private bool keepBlockNextTurn;
    private void OnEnable()
    {
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.AttachPerformer<HealGA>(HealPerformer);
        ActionSystem.AttachPerformer<LoseHealthGA>(LoseHealthPerformer);
        ActionSystem.AttachPerformer<LoseHealthThenStatusGA>(LoseHealthThenStatusPerformer);
        ActionSystem.AttachPerformer<KeepBlockGA>(KeepBlockPerformer);
        ActionSystem.AttachPerformer<VowOfMightGA>(VowOfMightPerformer);
        ActionSystem.AttachPerformer<GainAdversityPowerGA>(GainAdversityPowerPerformer);

        ActionSystem.SubscribeReaction<LoseHealthGA>(LoseHealthPostReaction,ReactionTiming.POST);
    }
    private void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.DetachPerformer<HealGA>();
        ActionSystem.DetachPerformer<LoseHealthGA>();
        ActionSystem.DetachPerformer<LoseHealthThenStatusGA>();
        ActionSystem.DetachPerformer<KeepBlockGA>();
        ActionSystem.DetachPerformer<VowOfMightGA>();
        ActionSystem.DetachPerformer<GainAdversityPowerGA>();

        ActionSystem.UnsubscribeReaction<LoseHealthGA>(LoseHealthPostReaction,ReactionTiming.POST);
    }
    public void Setup(HeroData heroData)
    {
        adversityPower = 0;
        adversityTriggeredThisTurn = false;
        temporaryAdversityStats = 0;
        keepBlockNextTurn = false;
        LostHealthFromCardThisTurn = false;
        HeroView.Setup(heroData);
    }
    private void EnemyTurnPreReaction(EnemyTurnGA enemyTurnGA)
    {
        if (temporaryAdversityStats > 0)
        {
            var targets = new System.Collections.Generic.List<CombatantView>
            {
                HeroView
            };

            RemoveStatusEffectGA removeStrengthGA = new(
                StatusEffectType.STRENGTH,
                temporaryAdversityStats,
                targets
            );

            RemoveStatusEffectGA removeDexterityGA = new(
                StatusEffectType.DEXTERITY,
                temporaryAdversityStats,
                targets
            );

            ActionSystem.Instance.AddReaction(removeStrengthGA);
            ActionSystem.Instance.AddReaction(removeDexterityGA);

            temporaryAdversityStats = 0;
        }
        DiscardAllCardsGA discardAllCardsGA = new();
        ActionSystem.Instance.AddReaction(discardAllCardsGA);
    }
    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        adversityTriggeredThisTurn = false;
        if (keepBlockNextTurn)
        {
            keepBlockNextTurn = false;
        }
        else
        {
            int currentBlock = HeroView.GetStatusEffectStacks(
                StatusEffectType.ARMOR
            );

            if (currentBlock > 0)
            {
                RemoveStatusEffectGA removeBlockGA = new(
                    StatusEffectType.ARMOR,
                    currentBlock,
                    new System.Collections.Generic.List<CombatantView>
                    {
                HeroView
                    }
                );

                ActionSystem.Instance.AddReaction(removeBlockGA);
            }
        }
        LostHealthFromCardThisTurn = false;
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

        int oldHealth = HeroView.CurrentHealth;

        int newHealth = oldHealth - action.Amount;

        HeroView.SetCurrentHealth(newHealth);
        action.ActualHealthLost = oldHealth - HeroView.CurrentHealth;

        if (
            action.IsFromCard &&
            HeroView.CurrentHealth < oldHealth
        )
        {
            LostHealthFromCardThisTurn = true;
        }

        if (RunSystem.Instance != null)
        {
            RunSystem.Instance.SaveHealth(HeroView.CurrentHealth);
        }

        if (
            HeroView.CurrentHealth <= 0 &&
            BattleSystem.Instance != null
        )
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
    private IEnumerator KeepBlockPerformer(KeepBlockGA action)
    {
        keepBlockNextTurn = true;

        yield return null;
    }
    private IEnumerator VowOfMightPerformer(VowOfMightGA action)
    {
        if (
            action.Caster == null ||
            action.Targets == null ||
            action.Targets.Count == 0
        )
        {
            yield break;
        }

        LoseHealthGA loseHealthGA = new(
            action.HealthAmount,
            true
        );

        ActionSystem.Instance.AddReaction(loseHealthGA);

        AddStatusEffectGA addStrengthGA = new(
            StatusEffectType.STRENGTH,
            action.StrengthAmount,
            new System.Collections.Generic.List<CombatantView>
            {
            action.Caster
            }
        );

        ActionSystem.Instance.AddReaction(addStrengthGA);

        AddStatusEffectGA addWeakGA = new(
            StatusEffectType.WEAK,
            action.WeakAmount,
            action.Targets
        );

        ActionSystem.Instance.AddReaction(addWeakGA);

        yield return null;
    }
    private IEnumerator GainAdversityPowerPerformer(
    GainAdversityPowerGA action
)
    {
        if (action.Amount > 0)
        {
            adversityPower += action.Amount;
        }

        yield return null;
    }
    private void LoseHealthPostReaction(LoseHealthGA action)
    {
        if (
            !action.IsFromCard ||
            action.ActualHealthLost <= 0 ||
            adversityPower <= 0 ||
            adversityTriggeredThisTurn ||
            HeroView == null ||
            HeroView.CurrentHealth <= 0
        )
        {
            return;
        }

        adversityTriggeredThisTurn = true;

        temporaryAdversityStats += adversityPower;

        var targets = new System.Collections.Generic.List<CombatantView>
    {
        HeroView
    };

        AddStatusEffectGA strengthGA = new(
            StatusEffectType.STRENGTH,
            adversityPower,
            targets
        );

        AddStatusEffectGA dexterityGA = new(
            StatusEffectType.DEXTERITY,
            adversityPower,
            targets
        );

        ActionSystem.Instance.AddReaction(strengthGA);
        ActionSystem.Instance.AddReaction(dexterityGA);
    }
}
