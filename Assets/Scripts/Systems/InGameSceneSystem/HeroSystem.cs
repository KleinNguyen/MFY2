using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroSystem : Singleton<HeroSystem>
{
    [field: SerializeField]
    public HeroView HeroView { get; private set; }

    public CharacterType CurrentCharacter { get; private set; }

    public bool LostHealthFromCardThisTurn { get; private set; }

    private int adversityPower;
    private bool adversityTriggeredThisTurn;
    private int temporaryAdversityStats;
    private bool keepBlockNextTurn;

    private void OnEnable()
    {
        ActionSystem.SubscribeReaction<EnemyTurnGA>(
            EnemyTurnPreReaction,
            ReactionTiming.PRE
        );

        ActionSystem.SubscribeReaction<EnemyTurnGA>(
            EnemyTurnPostReaction,
            ReactionTiming.POST
        );

        ActionSystem.AttachPerformer<HealGA>(HealPerformer);
        ActionSystem.AttachPerformer<LoseHealthGA>(LoseHealthPerformer);

        ActionSystem.AttachPerformer<LoseHealthThenStatusGA>(
            LoseHealthThenStatusPerformer
        );

        ActionSystem.AttachPerformer<KeepBlockGA>(KeepBlockPerformer);
        ActionSystem.AttachPerformer<VowOfMightGA>(VowOfMightPerformer);

        ActionSystem.AttachPerformer<GainAdversityPowerGA>(
            GainAdversityPowerPerformer
        );

        ActionSystem.SubscribeReaction<LoseHealthGA>(
            LoseHealthPostReaction,
            ReactionTiming.POST
        );
    }

    private void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(
            EnemyTurnPreReaction,
            ReactionTiming.PRE
        );

        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(
            EnemyTurnPostReaction,
            ReactionTiming.POST
        );

        ActionSystem.DetachPerformer<HealGA>();
        ActionSystem.DetachPerformer<LoseHealthGA>();
        ActionSystem.DetachPerformer<LoseHealthThenStatusGA>();
        ActionSystem.DetachPerformer<KeepBlockGA>();
        ActionSystem.DetachPerformer<VowOfMightGA>();
        ActionSystem.DetachPerformer<GainAdversityPowerGA>();

        ActionSystem.UnsubscribeReaction<LoseHealthGA>(
            LoseHealthPostReaction,
            ReactionTiming.POST
        );
    }

    public void Setup(HeroData heroData)
    {
        if (heroData == null)
        {
            Debug.LogError(
                "Cannot set up hero system: HeroData is null.",
                this
            );

            return;
        }

        CurrentCharacter = heroData.Character;
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
            List<CombatantView> targets = new List<CombatantView>
            {
                HeroView
            };

            RemoveStatusEffectGA removeStrengthGA =
                new RemoveStatusEffectGA(
                    StatusEffectType.STRENGTH,
                    temporaryAdversityStats,
                    targets
                );

            ActionSystem.Instance.AddReaction(removeStrengthGA);

            temporaryAdversityStats = 0;
        }

        DiscardAllCardsGA discardAllCardsGA = new DiscardAllCardsGA();
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
                RemoveStatusEffectGA removeBlockGA =
                    new RemoveStatusEffectGA(
                        StatusEffectType.ARMOR,
                        currentBlock,
                        new List<CombatantView>
                        {
                            HeroView
                        }
                    );

                ActionSystem.Instance.AddReaction(removeBlockGA);
            }
        }

        LostHealthFromCardThisTurn = false;

        int poisonStacks = HeroView.GetStatusEffectStacks(
            StatusEffectType.POISON
        );

        if (poisonStacks > 0)
        {
            ApplyPoisonGA applyPoisonGA = new ApplyPoisonGA(
                poisonStacks,
                HeroView
            );

            ActionSystem.Instance.AddReaction(applyPoisonGA);
        }

        DrawCardsGA drawCardsGA = new DrawCardsGA(5);
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

        if (action.IsFromCard && HeroView.CurrentHealth < oldHealth)
        {
            LostHealthFromCardThisTurn = true;
        }

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

        LoseHealthGA loseHealthGA = new LoseHealthGA(
            action.HealthAmount,
            true
        );

        ActionSystem.Instance.AddReaction(loseHealthGA);

        AddStatusEffectGA addStatusEffectGA = new AddStatusEffectGA(
            action.StatusEffectType,
            action.StackCount,
            action.Targets
        );

        addStatusEffectGA.CardCaster = action.CardCaster;

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

        LoseHealthGA loseHealthGA = new LoseHealthGA(
            action.HealthAmount,
            true
        );

        ActionSystem.Instance.AddReaction(loseHealthGA);

        AddStatusEffectGA addStrengthGA = new AddStatusEffectGA(
            StatusEffectType.STRENGTH,
            action.StrengthAmount,
            new List<CombatantView>
            {
                action.Caster
            }
        );

        ActionSystem.Instance.AddReaction(addStrengthGA);

        AddStatusEffectGA addWeakGA = new AddStatusEffectGA(
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

        // Adversity grants strength once per turn after card-caused health loss.
        adversityTriggeredThisTurn = true;
        temporaryAdversityStats += adversityPower;

        List<CombatantView> targets = new List<CombatantView>
        {
            HeroView
        };

        AddStatusEffectGA strengthGA = new AddStatusEffectGA(
            StatusEffectType.STRENGTH,
            adversityPower,
            targets
        );

        ActionSystem.Instance.AddReaction(strengthGA);
    }
}