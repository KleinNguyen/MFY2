
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Data/Hero")]

public class HeroData : ScriptableObject
{
    // HeroData.cs
    [field: SerializeField] public CharacterType Character { get; private set; }
    [field: SerializeField] public Sprite Image { get; private set; }
    [field: SerializeField] public Sprite SelectionBackground { get; private set; }
    [field: SerializeField] public string Description { get; private set; }
    [field: SerializeField] public int Health { get; private set; }
    [field: SerializeField, Min(0)] public int StartingMana { get; private set; } = 3;
    [field: SerializeField] public List<CardData> Deck { get; private set; }

}
