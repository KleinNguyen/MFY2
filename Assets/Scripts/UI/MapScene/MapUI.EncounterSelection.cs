using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class MapUI
{
    // Encounter selection
    private EncounterData GetEncounter(MapPoint point)
    {
        RunSystem run = RunSystem.Instance;
        if (run == null || run.CurrentAct == null || point == null) return null;

        ActData act = run.CurrentAct;
        int floor = point.Floor + 1;
        if (point.Type == MapPointType.Boss)
        {
            if (IsEncounterAvailable(act.BossEncounter, EncounterType.Boss, floor)) return act.BossEncounter;
            Debug.LogError($"Invalid Boss encounter at floor {floor} in {act.name}.", this);
            return null;
        }

        bool normal = point.Type == MapPointType.Normal || point.Type == MapPointType.Mystery;
        if (!normal && point.Type != MapPointType.Elite) return null;

        EncounterType type = normal ? EncounterType.Normal : EncounterType.Elite;
        List<EncounterData> pool = normal ? act.NormalEncounters : act.EliteEncounters;
        List<EncounterData> available = new();
        if (pool != null)
        {
            foreach (EncounterData encounter in pool)
            {
                if (IsEncounterAvailable(encounter, type, floor) &&
                    !available.Contains(encounter))
                    available.Add(encounter);
            }
        }

        if (available.Count == 0)
        {
            Debug.LogError($"No suitable {type} encounter at floor {floor} in {act.name}.", this);
            return null;
        }

        EncounterData previous = normal ? run.LastNormalEncounter : run.LastEliteEncounter;
        if (available.Count > 1) available.Remove(previous);

        int totalWeight = 0;
        foreach (EncounterData encounter in available) totalWeight += encounter.Weight;
        // Select by weight without allowing the previous encounter when alternatives exist.
        int roll = Random.Range(0, totalWeight);
        foreach (EncounterData encounter in available)
        {
            roll -= encounter.Weight;
            if (roll >= 0) continue;
            Debug.Log($"{act.name}, floor {floor}: {available.Count} candidates -> {encounter.name}", encounter);
            return encounter;
        }

        return null;
    }

    private bool IsEncounterAvailable(EncounterData encounter, EncounterType type, int floor)
    {
        if (encounter == null || encounter.Type != type || encounter.Weight <= 0) return false;
        if (floor < encounter.MinFloor || floor > encounter.MaxFloor) return false;
        if (encounter.Enemies == null || encounter.Enemies.Count == 0 || encounter.Enemies.Count > 3) return false;
        if (type != EncounterType.Normal && encounter.Enemies.Count != 1) return false;

        EnemyRank rank = type switch
        {
            EncounterType.Elite => EnemyRank.Elite,
            EncounterType.Boss => EnemyRank.Boss,
            _ => EnemyRank.Normal
        };

        foreach (EnemyData enemy in encounter.Enemies)
        {
            if (enemy == null || enemy.Rank != rank || enemy.Health <= 0 ||
                enemy.Moves == null || enemy.Moves.Count == 0)
                return false;
        }

        return true;
    }
}
