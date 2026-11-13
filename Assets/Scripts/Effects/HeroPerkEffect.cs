using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public class HeroPerkEffect : Effect
{
    [SerializeField] private HeroPerkEffectType effectType;
    [SerializeField, Min(0)] private int amount = 2;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        if (caster == null) return null;

        switch (effectType)
        {
            case HeroPerkEffectType.GainCombo:
                return new AddStatusEffectGA(StatusEffectType.COMBO, amount, new List<CombatantView> { caster });
            case HeroPerkEffectType.Heal:
                return new HealGA(amount, new List<CombatantView> { caster }, caster);
            case HeroPerkEffectType.RandomCounter:
                return new RandomPerkCounterGA(amount, caster);
            default:
                return null;
        }
    }
}

public class RandomPerkCounterGA : GameAction, IHaveCaster
{
    public int Damage { get; }
    public CombatantView Caster { get; }

    public RandomPerkCounterGA(int damage, CombatantView caster)
    {
        Damage = Mathf.Max(0, damage);
        Caster = caster;
    }
}