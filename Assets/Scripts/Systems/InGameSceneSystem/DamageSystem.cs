using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class DamageSystem : MonoBehaviour
{
    [SerializeField] private GameObject damageVFX;
    private void OnEnable()
    {
        ActionSystem.AttachPerformer<DealDamageGA>(DealDamagePerformer); 
        ActionSystem.AttachPerformer<BlockThenDamageGA>(BlockThenDamagePerformer);
        ActionSystem.AttachPerformer<DamageThenWeakBonusGA>(DamageThenWeakBonusPerformer);
        ActionSystem.AttachPerformer<BonusDamageIfWeakGA>(BonusDamageIfWeakPerformer);
    }
    private void OnDisable()
    {
        ActionSystem.DetachPerformer<DealDamageGA>();
        ActionSystem.DetachPerformer<BlockThenDamageGA>();
        ActionSystem.DetachPerformer<DamageThenWeakBonusGA>();
        ActionSystem.DetachPerformer<BonusDamageIfWeakGA>();
    }
    private IEnumerator DealDamagePerformer(DealDamageGA dealDamageGA)
    {
        foreach (var target in dealDamageGA.Targets)
        {
            if (target == null || target.CurrentHealth <= 0) continue;
            target.Damage(dealDamageGA.Amount);
            Instantiate(damageVFX, target.transform.position, Quaternion.identity);
            yield return new WaitForSeconds(0.5f);
            if (target.CurrentHealth <= 0)
            {
                if (target is EnemyView enemyView)
                {
                    KillEnemyGA killEnemyGA = new(enemyView);
                    ActionSystem.Instance.AddReaction(killEnemyGA);
                }
                else if (target is HeroView)
                {
                    BattleSystem.Instance.Defeat();
                }
            }
        }
    }
    private IEnumerator BlockThenDamagePerformer(
    BlockThenDamageGA action
)
    {
        if (action.Caster == null)
        {
            Debug.LogError(
                "BlockThenDamageGA has no caster."
            );

            yield break;
        }

        List<CombatantView> blockTargets = new()
    {
        action.Caster
    };

        AddStatusEffectGA addBlockGA = new(
            StatusEffectType.ARMOR,
            action.BlockAmount,
            blockTargets
        );

        ActionSystem.Instance.AddReaction(addBlockGA);

        DealDamageGA dealDamageGA = new(
            action.DamageAmount,
            action.Targets,
            action.Caster
        );

        ActionSystem.Instance.AddReaction(dealDamageGA);

        yield return null;
    }
    private IEnumerator DamageThenWeakBonusPerformer(
    DamageThenWeakBonusGA action
)
    {
        if (action.Targets == null || action.Targets.Count == 0)
        {
            yield break;
        }
        DealDamageGA baseDamageGA = new(
            action.BaseDamage,
            action.Targets,
            action.Caster
        );

        ActionSystem.Instance.AddReaction(baseDamageGA);
        BonusDamageIfWeakGA bonusDamageGA = new(
            action.BonusDamage,
            action.Targets,
            action.Caster
        );

        ActionSystem.Instance.AddReaction(bonusDamageGA);

        yield return null;
    }
    private IEnumerator BonusDamageIfWeakPerformer(
    BonusDamageIfWeakGA action
)
    {
        if (action.BonusDamage <= 0)
        {
            yield break;
        }

        List<CombatantView> validTargets = new();

        foreach (CombatantView target in action.Targets)
        {
            if (target == null || target.CurrentHealth <= 0)
            {
                continue;
            }

            int weakStacks = target.GetStatusEffectStacks(
                StatusEffectType.WEAK
            );

            if (weakStacks > 0)
            {
                validTargets.Add(target);
            }
        }

        if (validTargets.Count == 0)
        {
            yield break;
        }

        DealDamageGA bonusDamageGA = new(
            action.BonusDamage,
            validTargets,
            action.Caster
        );

        ActionSystem.Instance.AddReaction(bonusDamageGA);

        yield return null;
    }
}