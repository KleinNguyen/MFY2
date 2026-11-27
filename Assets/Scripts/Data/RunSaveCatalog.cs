using System.Collections.Generic;
using UnityEngine;

public class RunSaveCatalog : ScriptableObject
{
    public List<CardData> cards = new();
    public List<HeroData> heroes = new();

    public CardData FindCard(string id) =>
        string.IsNullOrEmpty(id)
            ? null
            : cards.Find(card => card != null && card.SaveId == id);

    public HeroData FindHero(CharacterType character) =>
        heroes.Find(hero => hero != null && hero.Character == character);
}