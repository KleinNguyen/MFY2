using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ComboSystem : MonoBehaviour
{
    public int AttacksThisTurn { get; private set; }
    public int ComboBeforeLastCardPlay { get; private set; }

    private readonly List<GainAttackMilestonePowerGA> attackMilestonePowers = new();

    private readonly List<GainExposedComboPowerGA> exposedComboPowers = new();
    private bool exposedComboTriggeredThisTurn;

    private HeroView Hero => HeroSystem.Instance != null ? HeroSystem.Instance.HeroView : null;

    public int CurrentCombo => Hero != null
        ? Mathf.Max(0, Hero.GetStatusEffectStacks(StatusEffectType.COMBO))
        : 0;

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<GainComboGA>(GainComboPerformer);
        ActionSystem.AttachPerformer<SpendComboGA>(SpendComboPerformer);
        ActionSystem.AttachPerformer<GainAttackMilestonePowerGA>(GainAttackMilestonePowerPerformer);
        ActionSystem.AttachPerformer<GainExposedComboPowerGA>(GainExposedComboPowerPerformer);
        ActionSystem.SubscribeReaction<AddStatusEffectGA>(ExposedAppliedPostReaction, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<PlayCardGA>(AttackCardPostReaction, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(BeginPlayerTurnReaction, ReactionTiming.POST);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<GainComboGA>();
        ActionSystem.DetachPerformer<SpendComboGA>();
        ActionSystem.DetachPerformer<GainAttackMilestonePowerGA>();
        ActionSystem.DetachPerformer<GainExposedComboPowerGA>();
        ActionSystem.UnsubscribeReaction<AddStatusEffectGA>(ExposedAppliedPostReaction, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<PlayCardGA>(AttackCardPostReaction, ReactionTiming.POST);
        exposedComboPowers.Clear();
        exposedComboTriggeredThisTurn = false;
        attackMilestonePowers.Clear();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(BeginPlayerTurnReaction, ReactionTiming.POST);
        AttacksThisTurn = 0;
        ComboBeforeLastCardPlay = 0;
    }

    public void ResetCombat()
    {
        exposedComboPowers.Clear();
        exposedComboTriggeredThisTurn = false;
        attackMilestonePowers.Clear();
        AttacksThisTurn = 0;
        ComboBeforeLastCardPlay = 0;
        HeroView hero = Hero;
        int amount = CurrentCombo;
        if (hero != null && amount > 0)
            hero.RemoveStatusEffect(StatusEffectType.COMBO, amount);
    }

    public void GainCombo(int amount)
    {
        HeroView hero = Hero;
        if (hero != null && amount > 0)
            hero.AddStatusEffect(StatusEffectType.COMBO, amount);
    }

    public bool HasCombo(int amount)
    {
        return CurrentCombo >= Mathf.Max(0, amount);
    }

    public int SpendCombo(int amount)
    {
        HeroView hero = Hero;
        int spent = Mathf.Min(CurrentCombo, Mathf.Max(0, amount));
        if (hero != null && spent > 0)
            hero.RemoveStatusEffect(StatusEffectType.COMBO, spent);
        return spent;
    }

    public int SpendAllCombo()
    {
        return SpendCombo(CurrentCombo);
    }

    public void RecordCardPlay(PlayCardGA action)
    {
        if (action == null || action.Card == null || action.WasPlayed) return;

        int comboBeforePlay = CurrentCombo;
        ComboBeforeLastCardPlay = comboBeforePlay;
        int attacksBeforePlay = AttacksThisTurn;
        bool isAttack = action.Card.IsAttack;
        int ordinal = isAttack ? ++AttacksThisTurn : 0;
        action.MarkPlayed(isAttack, comboBeforePlay, attacksBeforePlay, ordinal);
    }

    private IEnumerator GainComboPerformer(GainComboGA action)
    {
        HeroView hero = Hero;
        if (hero == null || action.Amount <= 0) yield break;

        AddStatusEffectGA add = new(StatusEffectType.COMBO, action.Amount,
            new List<CombatantView> { hero });
        add.CardCaster = action.CardCaster;
        add.SourceCard = action.SourceCard;
        ActionSystem.Instance.AddReaction(add);
        yield return null;
    }

    private IEnumerator SpendComboPerformer(SpendComboGA action)
    {
        HeroView hero = Hero;

        action.ActualSpent = hero == null
            ? 0
            : action.SpendAll
                ? CurrentCombo
                : Mathf.Min(CurrentCombo, Mathf.Max(0, action.Amount));

        if (hero == null)
        {
            yield break;
        }

        if (action.ActualSpent > 0)
        {
            RemoveStatusEffectGA remove = new(
                StatusEffectType.COMBO,
                action.ActualSpent,
                new List<CombatantView> { hero }
            );

            remove.CardCaster = action.CardCaster;
            remove.SourceCard = action.SourceCard;

            ActionSystem.Instance.AddReaction(remove);
        }

        if (action.ActualSpent > 0 && action.DrawPerComboSpent > 0)
        {
            DrawCardsGA draw = new(
                action.ActualSpent * action.DrawPerComboSpent
            );

            draw.CardCaster = action.CardCaster;
            draw.SourceCard = action.SourceCard;

            ActionSystem.Instance.AddReaction(draw);
        }

        int damageAmount =
            Mathf.Max(0, action.BaseDamageAfterSpend) +
            action.ActualSpent * Mathf.Max(0, action.DamagePerComboSpent);

        if (damageAmount > 0 &&
            action.DamageTargets != null &&
            action.DamageTargets.Count > 0)
        {
            DealDamageGA damage = new(
                damageAmount,
                action.DamageTargets,
                hero
            );

            damage.ComboOnKill = Mathf.Max(0, action.ComboOnKill);
            damage.CardCaster = action.CardCaster;
            damage.SourceCard = action.SourceCard;

            ActionSystem.Instance.AddReaction(damage);
        }

        yield return null;
    }

    private IEnumerator GainAttackMilestonePowerPerformer(GainAttackMilestonePowerGA action)
    {
        HeroView hero = Hero;
        if (hero != null && hero.CurrentHealth > 0 && action.RequiredAttacks > 0 &&
            (action.ComboGain > 0 || action.DrawAmount > 0))
        {
            attackMilestonePowers.Add(action);
        }

        yield return null;
    }

    private void AttackCardPostReaction(PlayCardGA action)
    {
        HeroView hero = Hero;
        if (action == null || !action.WasPlayed || !action.WasAttackCard ||
            hero == null || hero.CurrentHealth <= 0)
        {
            return;
        }

        foreach (GainAttackMilestonePowerGA power in attackMilestonePowers)
        {
            if (action.AttackOrdinal != power.RequiredAttacks)
            {
                continue;
            }

            if (power.ComboGain > 0)
            {
                AddStatusEffectGA gain = new(
                    StatusEffectType.COMBO,
                    power.ComboGain,
                    new List<CombatantView> { hero }
                );
                gain.CardCaster = hero;
                gain.SourceCard = power.SourceCard;
                ActionSystem.Instance.AddReaction(gain);
            }

            if (power.DrawAmount > 0)
            {
                DrawCardsGA draw = new(power.DrawAmount);
                draw.CardCaster = hero;
                draw.SourceCard = power.SourceCard;
                ActionSystem.Instance.AddReaction(draw);
            }
        }
    }

    private IEnumerator GainExposedComboPowerPerformer(GainExposedComboPowerGA action)
    {
        HeroView hero = Hero;
        if (hero != null && hero.CurrentHealth > 0 && action.Amount > 0)
        {
            exposedComboPowers.Add(action);
        }

        yield return null;
    }

    private void ExposedAppliedPostReaction(AddStatusEffectGA action)
    {
        HeroView hero = Hero;
        if (action == null || action.StatusEffectType != StatusEffectType.EXPOSED ||
            action.StackCount <= 0 || action.Targets == null ||
            hero == null || hero.CurrentHealth <= 0 || action.CardCaster != hero ||
            exposedComboPowers.Count == 0 || exposedComboTriggeredThisTurn)
        {
            return;
        }

        bool appliedToEnemy = false;
        foreach (CombatantView target in action.Targets)
        {
            if (target is EnemyView enemy && enemy.CurrentHealth > 0 &&
                enemy.gameObject.activeInHierarchy &&
                enemy.GetStatusEffectStacks(StatusEffectType.EXPOSED) > 0)
            {
                appliedToEnemy = true;
                break;
            }
        }

        if (!appliedToEnemy)
        {
            return;
        }

        exposedComboTriggeredThisTurn = true;
        foreach (GainExposedComboPowerGA power in exposedComboPowers)
        {
            AddStatusEffectGA gain = new(
                StatusEffectType.COMBO,
                power.Amount,
                new List<CombatantView> { hero }
            );
            gain.CardCaster = hero;
            gain.SourceCard = power.SourceCard;
            ActionSystem.Instance.AddReaction(gain);
        }
    }

    private void BeginPlayerTurnReaction(EnemyTurnGA action)
    {
        exposedComboTriggeredThisTurn = false;
        AttacksThisTurn = 0;
        ComboBeforeLastCardPlay = 0;
    }
}
