using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EscalatingRhythmEffect : Effect
{
    [SerializeField, Min(0)] private int damageAmount = 8;
    [SerializeField, Min(1)] private int comboPerGain = 2;
    [SerializeField, Min(0)] private int maxComboGain = 3;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        ComboSystem combo = CardSystem.Instance != null ? CardSystem.Instance.Combo : null;
        int comboBeforePlay = combo != null
            ? combo.ComboBeforeLastCardPlay
            : caster != null ? caster.GetStatusEffectStacks(StatusEffectType.COMBO) : 0;

        int comboGain = Mathf.Min(
            Mathf.Max(0, maxComboGain),
            Mathf.Max(0, comboBeforePlay) / Mathf.Max(1, comboPerGain)
        );

        DealDamageGA damageGA = new(damageAmount, targets ?? new List<CombatantView>(), caster);

        if (caster != null && comboGain > 0)
        {
            AddStatusEffectGA gainComboGA = new(
                StatusEffectType.COMBO,
                comboGain,
                new List<CombatantView> { caster }
            );
            gainComboGA.CardCaster = caster;
            damageGA.PostReactions.Add(gainComboGA);
        }

        return damageGA;
    }
}
