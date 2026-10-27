using UnityEngine;
using System.Collections.Generic;
using SerializeReferenceEditor;

public enum CardType
{
    Attack,
    Skill,
    Power
}

[CreateAssetMenu(menuName = "Data/Card")]
public class CardData : ScriptableObject
{
    // CardData.cs
    [field: SerializeField] public CharacterType Owner { get; private set; }
    [field: SerializeField] public CardType Type { get; private set; }
    [field: SerializeField] public string Description { get; private set; }
    [field: SerializeField] public int Mana { get; private set; }
    [field: SerializeField] public bool ExhaustOnPlay { get; private set; }
    [field: SerializeField] public bool IsSly { get; private set; }

    [field: SerializeReference, SR] public Effect ManualTargetEffect { get; private set; } = null;
    [field: SerializeField] public List<AutoTargetEffect> OtherEffects { get; private set; }
}
