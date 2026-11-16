using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Encounter")]
public class EncounterData : ScriptableObject
{
    [field: SerializeField] public List<EnemyData> Enemies { get; private set; } = new();
    [field: SerializeField] public EncounterType Type { get; private set; }
    [field: SerializeField, Min(1)] public int MinFloor { get; private set; } = 1;
    [field: SerializeField, Min(1)] public int MaxFloor { get; private set; } = 9;
    [field: SerializeField, Min(0)] public int Weight { get; private set; } = 1;
}