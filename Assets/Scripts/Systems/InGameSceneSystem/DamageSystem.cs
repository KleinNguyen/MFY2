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
        ActionSystem.AttachPerformer<DamageThenStatusGA>(DamageThenStatusPerformer);
    }
    private void OnDisable()
    {
        ActionSystem.DetachPerformer<DealDamageGA>();
        ActionSystem.DetachPerformer<BlockThenDamageGA>();
        ActionSystem.DetachPerformer<DamageThenWeakBonusGA>();
        ActionSystem.DetachPerformer<BonusDamageIfWeakGA>();
        ActionSystem.DetachPerformer<DamageThenStatusGA>();
    }
    private IEnumerator DealDamagePerformer(DealDamageGA dealDamageGA)
    {
        if (dealDamageGA.IsAttack && HasLivingTarget(dealDamageGA.Targets))
        {
            PlayHeroAttackSfx(dealDamageGA.Caster);
        }

        foreach (var target in dealDamageGA.Targets)
        {
            if (target == null || target.CurrentHealth <= 0)
            {
                continue;
            }

            bool targetHasWeak =
                target.GetStatusEffectStacks(StatusEffectType.WEAK) > 0;

            int finalDamage = dealDamageGA.Amount;
            if (
                dealDamageGA.IsAttack &&
                dealDamageGA.Caster is HeroView heroCaster
            )
            {
                finalDamage += heroCaster.GetStatusEffectStacks(
                    StatusEffectType.STRENGTH
                );
            }

            finalDamage = Mathf.Max(0, finalDamage);

            if (
                dealDamageGA.IsAttack &&
                target.GetStatusEffectStacks(StatusEffectType.EXPOSED) > 0
            )
            {
                finalDamage = Mathf.FloorToInt(finalDamage * 1.5f);
            }

            target.Damage(finalDamage);

            Instantiate(
                damageVFX,
                target.transform.position,
                Quaternion.identity
            );

            yield return new WaitForSeconds(0.5f);
            int healAmount = dealDamageGA.HealAfterDamage;
            if (targetHasWeak)
            {
                healAmount += dealDamageGA.HealIfTargetWeak;
            }

            if (healAmount > 0 && dealDamageGA.Caster is HeroView hero)
            {
                HealGA healGA = new(
                    healAmount,
                    new List<CombatantView> { hero },
                    hero
                );

                ActionSystem.Instance.AddReaction(healGA);
            }

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

    private bool HasLivingTarget(List<CombatantView> targets)
    {
        if (targets == null)
        {
            return false;
        }

        foreach (CombatantView target in targets)
        {
            if (target != null && target.CurrentHealth > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void PlayHeroAttackSfx(CombatantView caster)
    {
        if (caster is not HeroView || HeroSystem.Instance == null)
        {
            return;
        }

        switch (HeroSystem.Instance.CurrentCharacter)
        {
            case CharacterType.Red:
                SfxManager.PlaySfx(SfxId.HeroAttackRed);
                break;
            case CharacterType.Green:
                SfxManager.PlaySfx(SfxId.HeroAttackGreen);
                break;
            case CharacterType.Yellow:
                SfxManager.PlaySfx(SfxId.HeroAttackYellow);
                break;
            default:
                Debug.LogError(
                    "Cannot play hero attack SFX: the selected character is not supported.",
                    this
                );
                break;
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
        addBlockGA.CardCaster = action.CardCaster;

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
    private IEnumerator DamageThenStatusPerformer(
    DamageThenStatusGA action
)
    {
        if (action.Targets == null || action.Targets.Count == 0)
        {
            yield break;
        }
        DealDamageGA dealDamageGA = new(
            action.DamageAmount,
            action.Targets,
            action.Caster
        );

        ActionSystem.Instance.AddReaction(dealDamageGA);

        AddStatusEffectGA addStatusEffectGA = new(
            action.StatusEffectType,
            action.StackCount,
            action.Targets
        );
        addStatusEffectGA.CardCaster = action.CardCaster;

        ActionSystem.Instance.AddReaction(addStatusEffectGA);

        yield return null;
    }
}