using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TheLongConEffect : Effect
{
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private int damagePerCardDrawn = 1;
    [SerializeField] private int maxBaseDamage = 30;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        int cardsDrawn = CardSystem.Instance != null
            ? CardSystem.Instance.CardsDrawnThisCombat
            : 0;

        int damage = Mathf.Clamp(
            baseDamage + cardsDrawn * damagePerCardDrawn,
            0,
            maxBaseDamage
        );

        return new DealDamageGA(damage, targets, caster);
    }
}
