using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectSystem : MonoBehaviour
{
    private int cardBlockMultiplier = 1;
    private bool heroHadExposedAtEnemyTurnStart;

    private void OnEnable()
    {
        cardBlockMultiplier = 1;
        heroHadExposedAtEnemyTurnStart = false;

        ActionSystem.AttachPerformer<FortifiedTrapGA>(
            FortifiedTrapPerformer
        );

        ActionSystem.AttachPerformer<AddStatusEffectGA>(
            AddStatusEffectPerformer
        );

        ActionSystem.AttachPerformer<RemoveStatusEffectGA>(
            RemoveStatusEffectPerformer
        );

        ActionSystem.AttachPerformer<ArmorBreakTickGA>(
            ArmorBreakTickPerformer
        );

        ActionSystem.SubscribeReaction<EnemyTurnGA>(
            EndPlayerTurnReaction,
            ReactionTiming.PRE
        );

        ActionSystem.SubscribeReaction<EnemyTurnGA>(
            EndEnemyTurnReaction,
            ReactionTiming.POST
        );
    }

    private void OnDisable()
    {
        cardBlockMultiplier = 1;

        ActionSystem.DetachPerformer<FortifiedTrapGA>();
        ActionSystem.DetachPerformer<AddStatusEffectGA>();
        ActionSystem.DetachPerformer<RemoveStatusEffectGA>();
        ActionSystem.DetachPerformer<ArmorBreakTickGA>();

        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(
            EndPlayerTurnReaction,
            ReactionTiming.PRE
        );

        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(
            EndEnemyTurnReaction,
            ReactionTiming.POST
        );
    }

    private IEnumerator AddStatusEffectPerformer(AddStatusEffectGA action)
    {
        if (action.Targets == null)
        {
            yield break;
        }

        foreach (CombatantView target in action.Targets)
        {
            if (target == null || target.CurrentHealth <= 0)
            {
                continue;
            }

            int finalStackCount = action.StackCount;

            if (action.StatusEffectType == StatusEffectType.ARMOR)
            {
                finalStackCount = Mathf.Max(0, finalStackCount);

                if (
                    action.CardCaster is HeroView &&
                    target == action.CardCaster
                )
                {
                    finalStackCount *= cardBlockMultiplier;
                }
            }

            if (finalStackCount <= 0)
            {
                continue;
            }

            target.AddStatusEffect(
                action.StatusEffectType,
                finalStackCount
            );

            SfxId? sfxId = null;

            switch (action.StatusEffectType)
            {
                case StatusEffectType.ARMOR:
                    sfxId = SfxId.ArmorGained;
                    break;

                case StatusEffectType.WEAK:
                    sfxId = SfxId.WeakApplied;
                    break;

                case StatusEffectType.POISON:
                    sfxId = SfxId.PoisonApplied;
                    break;

                case StatusEffectType.ARMOR_BREAK:
                    sfxId = SfxId.ArmorBreakApplied;
                    break;
            }

            if (sfxId.HasValue)
            {
                SfxManager.PlayCombatEffectSfx(sfxId.Value);
            }

            yield return null;
        }
    }

    private IEnumerator FortifiedTrapPerformer(FortifiedTrapGA action)
    {
        cardBlockMultiplier *= 2;

        yield return null;
    }

    private IEnumerator RemoveStatusEffectPerformer(
        RemoveStatusEffectGA action
    )
    {
        if (action.Targets == null)
        {
            yield break;
        }

        foreach (CombatantView target in action.Targets)
        {
            if (target == null)
            {
                continue;
            }

            target.RemoveStatusEffect(
                action.StatusEffectType,
                action.StackCount
            );

            yield return null;
        }
    }

    private IEnumerator ArmorBreakTickPerformer(ArmorBreakTickGA action)
    {
        CombatantView target = action.Target;

        if (target == null || target.CurrentHealth <= 0)
        {
            yield break;
        }

        target.TickArmorBreak();

        yield return null;
    }

    private void EndPlayerTurnReaction(EnemyTurnGA action)
    {
        cardBlockMultiplier = 1;
        heroHadExposedAtEnemyTurnStart = false;

        HeroView hero = null;

        if (HeroSystem.Instance != null)
        {
            hero = HeroSystem.Instance.HeroView;
        }

        if (hero == null || hero.CurrentHealth <= 0)
        {
            return;
        }

        heroHadExposedAtEnemyTurnStart =
            hero.GetStatusEffectStacks(StatusEffectType.EXPOSED) > 0;

        ReduceStatusByOne(hero, StatusEffectType.WEAK);

        ActionSystem.Instance.AddReaction(
            new ArmorBreakTickGA(hero)
        );
    }

    private void EndEnemyTurnReaction(EnemyTurnGA action)
    {
        if (action.WasSkipped)
        {
            heroHadExposedAtEnemyTurnStart = false;
            return;
        }

        HeroView hero = null;

        if (HeroSystem.Instance != null)
        {
            hero = HeroSystem.Instance.HeroView;
        }

        if (heroHadExposedAtEnemyTurnStart)
        {
            ReduceStatusByOne(hero, StatusEffectType.EXPOSED);
        }

        heroHadExposedAtEnemyTurnStart = false;

        if (EnemySystem.Instance == null)
        {
            return;
        }

        foreach (EnemyView enemy in EnemySystem.Instance.Enemies)
        {
            if (enemy == null || enemy.CurrentHealth <= 0)
            {
                continue;
            }

            ReduceTemporaryStatuses(enemy);
        }
    }

    private void ReduceTemporaryStatuses(CombatantView target)
    {
        ReduceStatusByOne(target, StatusEffectType.EXPOSED);
    }

    private void ReduceStatusByOne(
        CombatantView target,
        StatusEffectType statusType
    )
    {
        if (target == null || target.CurrentHealth <= 0)
        {
            return;
        }

        if (target.GetStatusEffectStacks(statusType) <= 0)
        {
            return;
        }

        RemoveStatusEffectGA removeStatusGA = new RemoveStatusEffectGA(
            statusType,
            1,
            new List<CombatantView>
            {
                target
            }
        );

        ActionSystem.Instance.AddReaction(removeStatusGA);
    }
}