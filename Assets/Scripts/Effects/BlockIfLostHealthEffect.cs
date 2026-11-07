using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockIfLostHealthEffect : Effect
{
    [SerializeField] private int baseBlock;

    [SerializeField] private int bonusBlock;

    public override GameAction GetGameAction(
        List<CombatantView> targets,
        CombatantView caster
    )
    {
        int totalBlock = baseBlock;

        if (
            HeroSystem.Instance != null &&
            HeroSystem.Instance.LostHealthFromCardThisTurn
        )
        {
            totalBlock += bonusBlock;
        }

        return new AddStatusEffectGA(
            StatusEffectType.ARMOR,
            totalBlock,
            targets
        );
    }
}