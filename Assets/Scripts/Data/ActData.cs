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

    [field: SerializeField, Min(0)]
    public int NormalWeight { get; private set; } = 45;

    [field: SerializeField, Min(0)]
    public int EliteWeight { get; private set; } = 25;

    [field: SerializeField, Min(0)]
    public int ShopWeight { get; private set; } = 15;

    [field: SerializeField, Min(0)]
    public int RestWeight { get; private set; } = 15;
    [field: SerializeField, Min(1)] public int EarlyNormalFloors { get; private set; } = 3;
    [field: SerializeField, Min(2)] public int FirstEliteFloor { get; private set; } = 6;
    [field: SerializeField, Min(0)] public int MinimumElitePoints { get; private set; } = 1;
    [field: SerializeField] public List<StatusEffectType> EnemyDebuffPool { get; private set; } = new();
}