using UnityEngine;
using System.Collections.Generic;
public class PerkSystem : Singleton<PerkSystem>
{
    private readonly List<Perk> perks = new();
    public void AddPerk(Perk perk)
    {
        perks.Add(perk);
        perk.OnAdd();
    }
    public void RemovePerk(Perk perk)
    {
        perks.Remove(perk);
        perk.OnRemove();
    }
}
