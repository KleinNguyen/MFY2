using System;
using System.Collections.Generic;
using UnityEngine;

public class Perk
{
    private readonly PerkData data;
    private bool subscribed;

    public Sprite Image => data.Image;

    public Perk(PerkData perkData)
    {
        data = perkData != null ? perkData : throw new ArgumentNullException(nameof(perkData));
    }

    public void OnAdd()
    {
        if (subscribed || data.PerkCondition == null) return;

        data.PerkCondition.SubscribeCondition(Reaction);
        subscribed = true;
    }

    public void OnRemove()
    {
        if (!subscribed) return;

        data.PerkCondition.UnsubscribeCondition(Reaction);
        subscribed = false;
    }

    private void Reaction(GameAction gameAction)
    {
        if (!subscribed || data.AutoTargetEffect?.Effect == null) return;
        if (ActionSystem.Instance == null || HeroSystem.Instance == null) return;
        if (BattleSystem.Instance != null && BattleSystem.Instance.IsBattleOver) return;

        HeroView hero = HeroSystem.Instance.HeroView;
        if (hero == null || hero.CurrentHealth <= 0) return;
        if (!data.PerkCondition.SubConditionIsMet(gameAction)) return;

        GameAction action = data.AutoTargetEffect.Effect.GetGameAction(new List<CombatantView>(), hero);
        if (action != null) ActionSystem.Instance.AddReaction(action);
    }
}