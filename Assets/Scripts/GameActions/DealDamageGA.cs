using UnityEngine;
using System.Collections.Generic;

public class DealDamageGA : GameAction, IHaveCaster
{
    public int Amount { get; set; }
    public bool IsAttack { get; private set; }
    public List<CombatantView> Targets { get; set; }
    public CombatantView Caster { get; private set; }
    public int HealIfTargetWeak { get; set; }
    public int HealAfterDamage { get; set; }
    private readonly Dictionary<CombatantView, int> bonusDamageByTarget = new();
    public DealDamageGA(int amount, List<CombatantView> targets, CombatantView caster, bool isAttack = true)
    {
        Amount = amount;
        Targets = new(targets);
        Caster = caster;
        IsAttack = isAttack;
    }
    public void SetBonusDamageForTarget(CombatantView target, int amount)
    {
        if (target == null) return;

        bonusDamageByTarget[target] = amount;
    }

    public int GetDamageForTarget(CombatantView target)
    {
        int bonusDamage = 0;

        if (target != null)
        {
            bonusDamageByTarget.TryGetValue(target, out bonusDamage);
        }

        return Amount + bonusDamage;
    }

}