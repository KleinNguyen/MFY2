using System;
using UnityEngine;

[Serializable]
public class EnemyMoveData
{
    [field: SerializeField] public EnemyMoveType MoveType { get; private set; }
    [field: SerializeField, Min(0)] public int DamagePerHit { get; private set; } = 6;
    [field: SerializeField, Min(1)] public int HitCount { get; private set; } = 1;
    [field: SerializeField, Min(0)] public int ArmorGain { get; private set; }
    [field: SerializeField] public bool ApplyRandomDebuff { get; private set; }
    [field: SerializeField, Range(0f, 100f)] public float DebuffChancePercent { get; private set; } = 100f;
    [field: SerializeField, Min(1)] public int DebuffStacks { get; private set; } = 1;
}