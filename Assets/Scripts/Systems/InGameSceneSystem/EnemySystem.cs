using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySystem : Singleton<EnemySystem>
{
    [SerializeField] private EnemyBoardView enemyBoardView;
    public List<EnemyView> Enemies => enemyBoardView.EnemyViews;
    private int thornsDamage;

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<EnemyTurnGA>(EnemyTurnPerformer);
        ActionSystem.AttachPerformer<EnemyMoveGA>(EnemyMovePerformer);
        ActionSystem.AttachPerformer<FinishEnemyMoveGA>(FinishEnemyMovePerformer);
        ActionSystem.AttachPerformer<AttackHeroGA>(AttackHeroPerformer);
        ActionSystem.AttachPerformer<KillEnemyGA>(KillEnemyPerformer);
        ActionSystem.AttachPerformer<GainThornsGA>(GainThornsPerformer);
        ActionSystem.SubscribeReaction<AttackHeroGA>(AttackHeroPostReaction, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<EnemyTurnGA>();
        ActionSystem.DetachPerformer<EnemyMoveGA>();
        ActionSystem.DetachPerformer<FinishEnemyMoveGA>();
        ActionSystem.DetachPerformer<AttackHeroGA>();
        ActionSystem.DetachPerformer<KillEnemyGA>();
        ActionSystem.DetachPerformer<GainThornsGA>();
        ActionSystem.UnsubscribeReaction<AttackHeroGA>(AttackHeroPostReaction, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    public void Setup(EncounterData encounter)
    {
        thornsDamage = 0;
        enemyBoardView.AddEnemies(encounter.Enemies);
        GrantTurnArmor(true);
    }
    private void GrantTurnArmor(bool immediate)
    {
        foreach (EnemyView enemy in Enemies)
        {
            if (enemy == null || enemy.CurrentHealth <= 0) continue;

            int armor = enemy.CurrentArmorGain;
            if (armor <= 0) continue;

            if (immediate)
            {
                enemy.AddStatusEffect(StatusEffectType.ARMOR, armor);
            }
            else
            {
                AddStatusEffectGA gainArmor = new(StatusEffectType.ARMOR, armor, new List<CombatantView> { enemy });
                ActionSystem.Instance.AddReaction(gainArmor);
            }
        }
    }

    private bool CanAct(EnemyView enemy)
    {
        HeroView hero = HeroSystem.Instance != null ? HeroSystem.Instance.HeroView : null;
        return enemy != null && enemy.CurrentHealth > 0 && hero != null && hero.CurrentHealth > 0 &&
            (BattleSystem.Instance == null || !BattleSystem.Instance.IsBattleOver);
    }

    private IEnumerator EnemyTurnPerformer(EnemyTurnGA action)
    {
        foreach (EnemyView enemy in Enemies)
        {
            if (!CanAct(enemy)) continue;

            int poison = enemy.GetStatusEffectStacks(StatusEffectType.POISON);
            if (poison > 0)
                ActionSystem.Instance.AddReaction(new ApplyPoisonGA(poison, enemy));

            ActionSystem.Instance.AddReaction(new ArmorBreakTickGA(enemy));
            ActionSystem.Instance.AddReaction(new EnemyMoveGA(enemy));
        }

        yield return null;
    }

    private IEnumerator EnemyMovePerformer(EnemyMoveGA action)
    {
        action.WasExecuted = false;
        EnemyView enemy = action.Enemy;
        if (!CanAct(enemy)) yield break;

        action.Move = enemy.CurrentMove;
        action.MoveType = enemy.CurrentMoveType;
        action.WasExecuted = true;

        int strength = enemy.CurrentStrengthGain;
        if (strength > 0)
        {
            AddStatusEffectGA gainStrength = new(StatusEffectType.STRENGTH, strength, new List<CombatantView> { enemy });
            ActionSystem.Instance.AddReaction(gainStrength);
        }

        for (int hit = 0; hit < enemy.CurrentHitCount; hit++)
        {
            AttackHeroGA attack = new(enemy);
            action.Attacks.Add(attack);
            ActionSystem.Instance.AddReaction(attack);
        }

        ActionSystem.Instance.AddReaction(new FinishEnemyMoveGA(action));
        yield return null;
    }

    private IEnumerator AttackHeroPerformer(AttackHeroGA action)
    {
        action.WasExecuted = false;
        EnemyView attacker = action.Attacker;
        if (!CanAct(attacker) || !attacker.CurrentMoveAttacks) yield break;

        Vector3 startPosition = attacker.transform.position;
        Tween forward = attacker.transform.DOMove(startPosition + Vector3.left, 0.15f).SetLink(attacker.gameObject);
        yield return forward.WaitForCompletion();
        if (!CanAct(attacker)) yield break;

        Tween backward = attacker.transform.DOMove(startPosition, 0.15f).SetLink(attacker.gameObject);
        yield return backward.WaitForCompletion();
        if (!CanAct(attacker)) yield break;

        action.WasExecuted = true;
        DealDamageGA damage = new(attacker.GetAttackDamage(), new List<CombatantView> { HeroSystem.Instance.HeroView }, attacker);
        ActionSystem.Instance.AddReaction(damage);
    }

    private IEnumerator FinishEnemyMovePerformer(FinishEnemyMoveGA action)
    {
        EnemyMoveGA move = action.MoveAction;
        if (move == null || !move.WasExecuted || !CanAct(move.Enemy)) yield break;

        TryApplyMoveDebuff(move);

        EnemyView enemy = move.Enemy;
        if (enemy.GetStatusEffectStacks(StatusEffectType.WEAK) > 0)
            enemy.RemoveStatusEffect(StatusEffectType.WEAK, 1);

        enemy.AdvanceMove();
        yield return null;
    }
    private void TryApplyMoveDebuff(EnemyMoveGA action)
    {
        EnemyView enemy = action.Enemy;
        EnemyMoveData move = action.Move;
        if (!CanAct(enemy) || move == null || !enemy.CanApplyRandomDebuff) return;
        if (!action.Attacks.Exists(attack => attack.WasExecuted)) return;

        float chance = Mathf.Clamp(move.DebuffChancePercent, 0f, 100f);
        if (chance <= 0f || (chance < 100f && Random.value * 100f >= chance)) return;

        int index = Random.Range(0, enemy.AllowedDebuffs.Count);
        StatusEffectType effect = enemy.AllowedDebuffs[index];
        HeroView hero = HeroSystem.Instance.HeroView;

        AddStatusEffectGA debuff = new(effect, Mathf.Max(1, move.DebuffStacks), new List<CombatantView> { hero });
        ActionSystem.Instance.AddReaction(debuff);
    }

    private IEnumerator KillEnemyPerformer(KillEnemyGA action)
    {
        EnemyView enemy = action.EnemyView;
        if (enemy == null || !Enemies.Contains(enemy)) yield break;

        yield return enemyBoardView.RemoveEnemy(enemy);
        if (Enemies.Count == 0) BattleSystem.Instance.Victory();
    }

    private IEnumerator GainThornsPerformer(GainThornsGA action)
    {
        if (action.Amount > 0) thornsDamage += action.Amount;
        yield return null;
    }

    private void AttackHeroPostReaction(AttackHeroGA action)
    {
        if (!action.WasExecuted || thornsDamage <= 0 || !CanAct(action.Attacker)) return;

        DealDamageGA damage = new(thornsDamage, new List<CombatantView> { action.Attacker }, HeroSystem.Instance.HeroView, false);
        ActionSystem.Instance.AddReaction(damage);
    }

    private void EnemyTurnPostReaction(EnemyTurnGA action)
    {
        thornsDamage = 0;
        GrantTurnArmor(false);
    }
}