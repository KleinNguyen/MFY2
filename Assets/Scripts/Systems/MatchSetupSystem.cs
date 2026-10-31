using System.Collections.Generic;
using UnityEngine;

public class MatchSetupSystem : MonoBehaviour
{
    [SerializeField] private HeroData heroData;
    [SerializeField] private PerkData perkData;
    [SerializeField] private List<EnemyData> enemyDatas;
    private void Start()
    {
        if (RunSystem.Instance == null) return;
        if (RunSystem.Instance.CurrentEncounter == null) return;
        HeroSystem.Instance.Setup(RunSystem.Instance.HeroData);
        HeroSystem.Instance.HeroView.SetCurrentHealth(RunSystem.Instance.CurrentHealth);
        EnemySystem.Instance.Setup(RunSystem.Instance.CurrentEncounter.Enemies);
        CardSystem.Instance.Setup(RunSystem.Instance.Deck);
        PerkSystem.Instance.AddPerk(new Perk(perkData));
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.Perform(drawCardsGA);
    }
}
