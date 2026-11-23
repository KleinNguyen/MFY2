using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class EnemySystem : Singleton<EnemySystem>
{
    [SerializeField] private EnemyBoardView enemyBoardView;

    private int thornsDamage;
    private int extraOpeningTurns;

    public List<EnemyView> Enemies
    {
        get
        {
            return enemyBoardView.EnemyViews;
        }
    }

    // Lifecycle
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

    // Battle setup
    public void Setup(EncounterData encounter)
    {
        thornsDamage = 0;
        extraOpeningTurns = 0;
        enemyBoardView.AddEnemies(encounter.Enemies);
        GrantTurnArmor(true);
    }

    public void SetExtraOpeningTurns(int amount)
    {
        extraOpeningTurns = Mathf.Max(0, amount);
    }

    // Enemy actions
    private IEnumerator EnemyTurnPerformer(EnemyTurnGA action)
    {
        action.WasSkipped = extraOpeningTurns > 0;

        if (action.WasSkipped)
        {
            extraOpeningTurns--;
            yield break;
        }

        foreach (EnemyView enemy in Enemies)
        {
            if (!CanAct(enemy)) continue;

            int poison = enemy.GetStatusEffectStacks(StatusEffectType.POISON);
            if (poison > 0)
            {
                ActionSystem.Instance.AddReaction(new ApplyPoisonGA(poison, enemy));
            }

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

        for (int hit = 0; hit < enemy.CurrentHitCount; hit++)
        {
            AttackHeroGA attack = new AttackHeroGA(enemy);
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

        HeroView hero = HeroSystem.Instance.HeroView;
        DealDamageGA damage = new DealDamageGA(attacker.GetAttackDamage(), new List<CombatantView> { hero }, attacker);
        ActionSystem.Instance.AddReaction(damage);
    }

    private IEnumerator FinishEnemyMovePerformer(FinishEnemyMoveGA action)
    {
        EnemyMoveGA move = action.MoveAction;
        if (move == null || !move.WasExecuted || !CanAct(move.Enemy)) yield break;

        TryApplyMoveDebuff(move);

        EnemyView enemy = move.Enemy;
        if (enemy.GetStatusEffectStacks(StatusEffectType.WEAK) > 0)
        {
            enemy.RemoveStatusEffect(StatusEffectType.WEAK, 1);
        }

        enemy.AdvanceMove();
        yield return null;
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

    // Reactions
    private void AttackHeroPostReaction(AttackHeroGA action)
    {
        if (!action.WasExecuted || thornsDamage <= 0 || !CanAct(action.Attacker)) return;

        HeroView hero = HeroSystem.Instance.HeroView;
        DealDamageGA damage = new DealDamageGA(thornsDamage, new List<CombatantView> { action.Attacker }, hero, false);
        ActionSystem.Instance.AddReaction(damage);
    }

    private void EnemyTurnPostReaction(EnemyTurnGA action)
    {
        thornsDamage = 0;
        if (!action.WasSkipped) GrantTurnArmor(false);
    }

    // Turn armor
    private void GrantTurnArmor(bool immediate)
    {
        foreach (EnemyView enemy in Enemies)
        {
            if (enemy == null || enemy.CurrentHealth <= 0) continue;

            int currentArmor = enemy.GetStatusEffectStacks(StatusEffectType.ARMOR);
            int newArmor = enemy.CurrentArmorGain;

            if (immediate)
            {
                if (currentArmor > 0) enemy.RemoveStatusEffect(StatusEffectType.ARMOR, currentArmor);
                if (newArmor > 0) enemy.AddStatusEffect(StatusEffectType.ARMOR, newArmor);
                continue;
            }

            List<CombatantView> targets = new List<CombatantView> { enemy };

            if (currentArmor > 0)
            {
                RemoveStatusEffectGA removeArmor = new RemoveStatusEffectGA(StatusEffectType.ARMOR, currentArmor, targets);
                ActionSystem.Instance.AddReaction(removeArmor);
            }

            if (newArmor > 0)
            {
                AddStatusEffectGA gainArmor = new AddStatusEffectGA(StatusEffectType.ARMOR, newArmor, targets);
                ActionSystem.Instance.AddReaction(gainArmor);
            }

            // Apply Armor Break after old armor is cleared and new armor is granted.
            ActionSystem.Instance.AddReaction(new ArmorBreakTickGA(enemy));
        }
    }

    // Attack debuffs
    private void TryApplyMoveDebuff(EnemyMoveGA action)
    {
        if (action == null || !action.WasExecuted) return;

        EnemyView enemy = action.Enemy;
        EnemyMoveData move = action.Move;
        if (!CanAct(enemy) || move == null || !move.ApplyRandomDebuff) return;
        if (action.MoveType != EnemyMoveType.Attack && action.MoveType != EnemyMoveType.AttackAndDefend) return;
        if (!HasExecutedAttack(action) || enemy.AllowedDebuffs.Count == 0) return;

        float chance = Mathf.Clamp(move.DebuffChancePercent, 0f, 100f);
        if (chance <= 0f) return;
        if (chance < 100f && Random.value * 100f >= chance) return;

        int index = Random.Range(0, enemy.AllowedDebuffs.Count);
        StatusEffectType effect = enemy.AllowedDebuffs[index];
        HeroView hero = HeroSystem.Instance.HeroView;
        int stacks = Mathf.Max(1, move.DebuffStacks);

        AddStatusEffectGA debuff = new AddStatusEffectGA(effect, stacks, new List<CombatantView> { hero });
        ActionSystem.Instance.AddReaction(debuff);
    }

    private bool HasExecutedAttack(EnemyMoveGA action)
    {
        foreach (AttackHeroGA attack in action.Attacks)
        {
            if (attack != null && attack.Attacker == action.Enemy && attack.WasExecuted)
            {
                return true;
            }
        }

        return false;
    }

    // Action validation
    private bool CanAct(EnemyView enemy)
    {
        if (enemy == null || enemy.CurrentHealth <= 0) return false;
        if (HeroSystem.Instance == null || HeroSystem.Instance.HeroView == null) return false;
        if (HeroSystem.Instance.HeroView.CurrentHealth <= 0) return false;
        if (BattleSystem.Instance != null && BattleSystem.Instance.IsBattleOver) return false;

        return true;
    }
}