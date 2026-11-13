using System;
using UnityEngine;

public enum HeroPerkTrigger
{
    AttackCardPlayed,
    EnemyKilled,
    HeroAttacked
}

[Serializable]
public class HeroPerkCondition : PerkCondition
{
    [SerializeField] private HeroPerkTrigger trigger;
    [SerializeField, Range(0f, 100f)] private float chancePercent = 10f;

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        switch (trigger)
        {
            case HeroPerkTrigger.AttackCardPlayed:
                ActionSystem.SubscribeReaction<PlayCardGA>(reaction, reactionTiming);
                break;
            case HeroPerkTrigger.EnemyKilled:
                ActionSystem.SubscribeReaction<KillEnemyGA>(reaction, reactionTiming);
                break;
            case HeroPerkTrigger.HeroAttacked:
                ActionSystem.SubscribeReaction<DealDamageGA>(reaction, reactionTiming);
                break;
        }
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        switch (trigger)
        {
            case HeroPerkTrigger.AttackCardPlayed:
                ActionSystem.UnsubscribeReaction<PlayCardGA>(reaction, reactionTiming);
                break;
            case HeroPerkTrigger.EnemyKilled:
                ActionSystem.UnsubscribeReaction<KillEnemyGA>(reaction, reactionTiming);
                break;
            case HeroPerkTrigger.HeroAttacked:
                ActionSystem.UnsubscribeReaction<DealDamageGA>(reaction, reactionTiming);
                break;
        }
    }

    public override bool SubConditionIsMet(GameAction action)
    {
        HeroView hero = HeroSystem.Instance != null ? HeroSystem.Instance.HeroView : null;
        if (hero == null || hero.CurrentHealth <= 0) return false;

        bool valid = false;
        switch (trigger)
        {
            case HeroPerkTrigger.AttackCardPlayed:
                valid = action is PlayCardGA play && play.WasPlayed && play.WasAttackCard;
                break;
            case HeroPerkTrigger.EnemyKilled:
                valid = action is KillEnemyGA kill && kill.EnemyView != null &&
                    kill.EnemyView.CurrentHealth <= 0 && EnemySystem.Instance != null &&
                    EnemySystem.Instance.Enemies.Contains(kill.EnemyView);
                break;
            case HeroPerkTrigger.HeroAttacked:
                valid = action is DealDamageGA hit && hit.IsAttack && hit.Caster is EnemyView &&
                    hit.Targets != null && hit.Targets.Contains(hero);
                break;
        }

        if (!valid || chancePercent <= 0f) return false;
        return chancePercent >= 100f || UnityEngine.Random.value < chancePercent / 100f;
    }
}