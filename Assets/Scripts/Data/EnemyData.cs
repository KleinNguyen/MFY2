using System.Collections.Generic;
using UnityEngine;

public enum EnemyRank
{
    Normal,
    Elite,
    Boss
}

[CreateAssetMenu(menuName = "Data/Enemy")]
public class EnemyData : ScriptableObject
{
    [field: SerializeField] public Sprite Image { get; private set; }
    [field: SerializeField] public Sprite[] IdleFrames { get; private set; }
    [field: SerializeField] public int Health { get; private set; }
    [field: SerializeField] public int AttackPower { get; private set; }
    [field: SerializeField] public EnemyRank Rank { get; private set; }
}
