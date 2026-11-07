using System.Collections.Generic;
using UnityEngine;

public enum EncounterType
{
    Normal,
    Elite,
    Boss
}

[CreateAssetMenu(menuName = "Data/Encounter")]
public class EncounterData : ScriptableObject
{
    [field: SerializeField] public List<EnemyData> Enemies { get; private set; }
    [field: SerializeField] public EncounterType Type { get; private set; }
}
