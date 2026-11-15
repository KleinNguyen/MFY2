using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectSystem : MonoBehaviour
{
    private const float ARMOR_BREAK_REDUCTION = 0.25f;
    private int cardBlockMultiplier = 1;

    private void OnEnable()
    {
        cardBlockMultiplier = 1;
        ActionSystem.AttachPerformer<FortifiedTrapGA>(FortifiedTrapPerformer);
        ActionSystem.AttachPerformer<AddStatusEffectGA>(AddStatusEffectPerformer);
        ActionSystem.AttachPerformer<RemoveStatusEffectGA>(RemoveStatusEffectPerformer);
        ActionSystem.AttachPerformer<ArmorBreakTickGA>(ArmorBreakTickPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EndPlayerTurnReaction, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EndEnemyTurnReaction, ReactionTiming.POST);
    }

    private void OnDisable()
    {
        cardBlockMultiplier = 1;
        ActionSystem.DetachPerformer<FortifiedTrapGA>();
        ActionSystem.DetachPerformer<AddStatusEffectGA>();
        ActionSystem.DetachPerformer<RemoveStatusEffectGA>();
        ActionSystem.DetachPerformer<ArmorBreakTickGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EndPlayerTurnReaction, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EndEnemyTurnReaction, ReactionTiming.POST);
    }

    private IEnumerator AddStatusEffectPerformer(AddStatusEffectGA action)
    {
        if (action.Targets == null) yield break;

        foreach (CombatantView target in action.Targets)
        {
            if (target == null || target.CurrentHealth <= 0) continue;

            int finalStackCount = action.StackCount;

            if (action.StatusEffectType == StatusEffectType.ARMOR)
            {
                if (action.CardCaster != null)
                    finalStackCount += action.CardCaster.GetStatusEffectStacks(StatusEffectType.DEXTERITY);

                finalStackCount = Mathf.Max(0, finalStackCount);

                if (action.CardCaster is HeroView && target == action.CardCaster)
                    finalStackCount *= cardBlockMultiplier;
            }

            if (finalStackCount <= 0) continue;

            target.AddStatusEffect(action.StatusEffectType, finalStackCount);

            SfxId? sfxId = action.StatusEffectType switch
            {
                StatusEffectType.ARMOR => SfxId.ArmorGained,
                StatusEffectType.WEAK => SfxId.WeakApplied,
                StatusEffectType.POISON => SfxId.PoisonApplied,
                StatusEffectType.ARMOR_BREAK => SfxId.ArmorBreakApplied,
                _ => null
            };

            if (sfxId.HasValue)
                SfxManager.PlayCombatEffectSfx(sfxId.Value);

            yield return null;
        }
    }

    private IEnumerator FortifiedTrapPerformer(FortifiedTrapGA action)
    {
        cardBlockMultiplier *= 2;
        yield return null;
    }

    private IEnumerator RemoveStatusEffectPerformer(RemoveStatusEffectGA action)
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

            target.RemoveStatusEffect(action.StatusEffectType, action.StackCount);

            yield return null;
        }
    }

    private IEnumerator ArmorBreakTickPerformer(ArmorBreakTickGA action)
    {
        CombatantView target = action.Target;
        if (target == null || target.CurrentHealth <= 0) yield break;

        int stacks = target.GetStatusEffectStacks(StatusEffectType.ARMOR_BREAK);
        if (stacks <= 0) yield break;

        int currentArmor = target.GetStatusEffectStacks(StatusEffectType.ARMOR);
        if (currentArmor > 0)
        {
            int remainingArmor = Mathf.FloorToInt(currentArmor * (1f - ARMOR_BREAK_REDUCTION));
            target.RemoveStatusEffect(StatusEffectType.ARMOR, currentArmor - remainingArmor);
        }

        target.RemoveStatusEffect(StatusEffectType.ARMOR_BREAK, 1);
        yield return null;
    }
    private void EndPlayerTurnReaction(EnemyTurnGA action)
    {
        cardBlockMultiplier = 1;

        if (HeroSystem.Instance == null || HeroSystem.Instance.HeroView == null)
        {
            return;
        }

        ReduceTemporaryStatuses(HeroSystem.Instance.HeroView);
    }

    private void EndEnemyTurnReaction(EnemyTurnGA action)
    {
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

    private void ReduceStatusByOne(CombatantView target, StatusEffectType statusType)
    {
        if (target == null || target.CurrentHealth <= 0)
        {
            return;
        }

        if (target.GetStatusEffectStacks(statusType) <= 0)
        {
            return;
        }

        RemoveStatusEffectGA removeStatusGA = new(statusType, 1, new List<CombatantView> { target });

        ActionSystem.Instance.AddReaction(removeStatusGA);
    }
}
