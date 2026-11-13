using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerkSystem : Singleton<PerkSystem>
{
    [SerializeField] private PerksUI perksUI;
    private readonly List<Perk> perks = new();

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<RandomPerkCounterGA>(RandomCounterPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<RandomPerkCounterGA>();
        foreach (Perk perk in perks)
        {
            perk.OnRemove();
            if (perksUI != null) perksUI.RemovePerkUI(perk);
        }
        perks.Clear();
    }

    public void AddPerk(Perk perk)
    {
        if (perk == null || perks.Contains(perk)) return;
        perks.Add(perk);
        if (perksUI != null) perksUI.AddPerkUI(perk);
        perk.OnAdd();
    }

    public void RemovePerk(Perk perk)
    {
        if (!perks.Remove(perk)) return;
        perk.OnRemove();
        if (perksUI != null) perksUI.RemovePerkUI(perk);
    }

    private IEnumerator RandomCounterPerformer(RandomPerkCounterGA action)
    {
        if (action.Caster == null || action.Caster.CurrentHealth <= 0 || action.Damage <= 0) yield break;
        if (EnemySystem.Instance == null) yield break;

        List<EnemyView> enemies = EnemySystem.Instance.Enemies.FindAll(enemy =>
            enemy != null && enemy.CurrentHealth > 0 && enemy.gameObject.activeInHierarchy);

        if (enemies.Count == 0) yield break;

        EnemyView target = enemies[Random.Range(0, enemies.Count)];
        ActionSystem.Instance.AddReaction(new DealDamageGA(
            action.Damage, new List<CombatantView> { target }, action.Caster, false));
        yield return null;
    }
}