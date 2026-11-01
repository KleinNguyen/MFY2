using UnityEngine;

public class HeroSystem : Singleton<HeroSystem>
{
    [field: SerializeField] public HeroView HeroView { get; private set; }
    private void OnEnable()
    {
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }
    private void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }
    public void Setup(HeroData heroData)
    {
        HeroView.Setup(heroData);
    }
    private void EnemyTurnPreReaction(EnemyTurnGA enemyTurnGA)
    {
        DiscardAllCardsGA discardAllCardsGA = new();
        ActionSystem.Instance.AddReaction(discardAllCardsGA);
    }
    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        int poisonStacks = HeroView.GetStatusEffectStacks(StatusEffectType.POISON);
        if(poisonStacks > 0)
        {
            ApplyPoisonGA applyPoisonGA = new(poisonStacks, HeroView);
            ActionSystem.Instance.AddReaction(applyPoisonGA);
        }
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.AddReaction(drawCardsGA);
    }
}
