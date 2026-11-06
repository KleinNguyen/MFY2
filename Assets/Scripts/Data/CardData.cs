using UnityEngine;
using System.Collections.Generic;
using SerializeReferenceEditor;



[CreateAssetMenu(menuName = "Data/Card")]
public class CardData : ScriptableObject
{
    // CardData.cs
    [field: SerializeField] public CharacterType Character { get; private set; }
    [field: SerializeField] public CardRarity Rarity { get; private set; }
    [field: SerializeField] public Sprite CardImage { get; private set; }
    [field: SerializeField] public string Description { get; private set; }
    [field: SerializeField] public int Mana { get; private set; }
    [field: SerializeField] public bool ExhaustOnPlay { get; private set; }
    [field: SerializeField] public bool IsSly { get; private set; }

    [field: SerializeReference, SR] public Effect ManualTargetEffect { get; private set; } = null;
    [field: SerializeField] public List<AutoTargetEffect> OtherEffects { get; private set; }
    [field: SerializeField] public bool CanUpgrade { get; private set; }

    [field: SerializeField] public string UpgradedDescription { get; private set; }

    [field: SerializeField] public int UpgradedMana { get; private set; }

    [field: SerializeReference, SR]
    public Effect UpgradedManualTargetEffect { get; private set; }

    [field: SerializeField]
    public List<AutoTargetEffect> UpgradedOtherEffects { get; private set; }
}
