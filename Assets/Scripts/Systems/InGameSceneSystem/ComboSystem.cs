using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ComboSystem : MonoBehaviour
{
    public int AttacksThisTurn { get; private set; }

    private HeroView Hero => HeroSystem.Instance != null ? HeroSystem.Instance.HeroView : null;

    public int CurrentCombo => Hero != null
        ? Mathf.Max(0, Hero.GetStatusEffectStacks(StatusEffectType.COMBO))
        : 0;

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<GainComboGA>(GainComboPerformer);
        ActionSystem.AttachPerformer<SpendComboGA>(SpendComboPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(BeginPlayerTurnReaction, ReactionTiming.POST);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<GainComboGA>();
        ActionSystem.DetachPerformer<SpendComboGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(BeginPlayerTurnReaction, ReactionTiming.POST);
        AttacksThisTurn = 0;
    }

    public void ResetCombat()
    {
        AttacksThisTurn = 0;
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
        action.ActualSpent = hero == null ? 0 : action.SpendAll
            ? CurrentCombo : Mathf.Min(CurrentCombo, Mathf.Max(0, action.Amount));
        if (hero == null || action.ActualSpent <= 0) yield break;

        RemoveStatusEffectGA remove = new(StatusEffectType.COMBO, action.ActualSpent,
            new List<CombatantView> { hero });
        remove.CardCaster = action.CardCaster;
        remove.SourceCard = action.SourceCard;
        ActionSystem.Instance.AddReaction(remove);
        yield return null;
    }

    private void BeginPlayerTurnReaction(EnemyTurnGA action)
    {
        AttacksThisTurn = 0;
    }
}
