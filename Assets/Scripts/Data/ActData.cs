using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Act")]
public class ActData : ScriptableObject
{
    [field: SerializeField] public string ActName { get; private set; }

    [field: SerializeField] public Sprite MapBackground { get; private set; }

    [field: SerializeField]
    public List<EncounterData> NormalEncounters
    { get; private set; } = new();

    [field: SerializeField]
    public List<EncounterData> EliteEncounters
    { get; private set; } = new();

    [field: SerializeField]
    public EncounterData BossEncounter
    { get; private set; }

    [Header("Map Point Weights")]

    [field: SerializeField, Min(0)]
    public int NormalWeight { get; private set; } = 45;

    [field: SerializeField, Min(0)]
    public int EliteWeight { get; private set; } = 25;

    [field: SerializeField, Min(0)]
    public int ShopWeight { get; private set; } = 15;

    [field: SerializeField, Min(0)]
    public int RestWeight { get; private set; } = 15;
}