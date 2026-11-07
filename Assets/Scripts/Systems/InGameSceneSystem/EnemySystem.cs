using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EnemySystem : Singleton<EnemySystem>
{
    [SerializeField] private EnemyBoardView enemyBoardView;
    public List<EnemyView> Enemies => enemyBoardView.EnemyViews;
    private void OnEnable()
    {
        ActionSystem.AttachPerformer<EnemyTurnGA>(EnemyTurnPerformer);
        ActionSystem.AttachPerformer<AttackHeroGA>(AttackHeroPerformer);
        ActionSystem.AttachPerformer<KillEnemyGA>(KillEnemyPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<EnemyTurnGA>();
        ActionSystem.DetachPerformer<AttackHeroGA>();
        ActionSystem.DetachPerformer<KillEnemyGA>();
    }

    public void Setup(EncounterData encounter)
    {
        enemyBoardView.AddEnemies(encounter.Enemies);
    }
    private IEnumerator EnemyTurnPerformer(EnemyTurnGA enemyTurnGA)
    {
        foreach (var enemy in enemyBoardView.EnemyViews)
        {
            int poisonStacks = enemy.GetStatusEffectStacks(StatusEffectType.POISON);
            if(poisonStacks > 0)
            {
                ApplyPoisonGA applyBurnGA = new(poisonStacks, enemy);
                ActionSystem.Instance.AddReaction(applyBurnGA);
            }
            AttackHeroGA attackHeroGA = new(enemy);
            ActionSystem.Instance.AddReaction(attackHeroGA);
        }
        yield return null;
    }
    private IEnumerator AttackHeroPerformer(
        AttackHeroGA attackHeroGA
    )
    {
        EnemyView attacker = attackHeroGA.Attacker;

        if (attacker == null || attacker.CurrentHealth <= 0)
        {
            yield break;
        }

        Tween tween = attacker.transform.DOMoveX(
            attacker.transform.position.x - 1f,
            0.15f
        );

        yield return tween.WaitForCompletion();

        attacker.transform.DOMoveX(
            attacker.transform.position.x + 1f,
            0.15f
        );

        int damage = attacker.AttackPower;

        int weakStacks = attacker.GetStatusEffectStacks(
            StatusEffectType.WEAK
        );

        //change weak dmg
        if (weakStacks > 0)
        {
            damage = Mathf.FloorToInt(damage * 0.75f);
        }

        DealDamageGA dealDamageGA = new(
            damage,
            new() { HeroSystem.Instance.HeroView },
            attackHeroGA.Caster
        );

        ActionSystem.Instance.AddReaction(dealDamageGA);

        if (weakStacks > 0)
        {
            RemoveStatusEffectGA removeWeakGA = new(
                StatusEffectType.WEAK,
                1,
                new() { attacker }
            );

            ActionSystem.Instance.AddReaction(removeWeakGA);
        }
    }
    private IEnumerator KillEnemyPerformer(KillEnemyGA killEnemyGA)
    {
        EnemyView enemy = killEnemyGA.EnemyView;
        if (enemy == null || !enemyBoardView.EnemyViews.Contains(enemy)) yield break;
        yield return enemyBoardView.RemoveEnemy(killEnemyGA.EnemyView);
        if(enemyBoardView.EnemyViews.Count == 0)
        {
            BattleSystem.Instance.Victory();
        }
    }
}
