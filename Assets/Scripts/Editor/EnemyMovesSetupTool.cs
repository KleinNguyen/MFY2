using System;
using UnityEditor;
using UnityEngine;

public static class EnemyMovesSetupTool
{
    [MenuItem("Tools/Enemies/Apply All Enemy Health")]
    public static void ApplyAllHealth()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before applying enemy Health.");
            return;
        }

        string[] names =
        {
            "n1", "n2", "n3", "n4", "n5", "n6", "n7", "n8", "n9",
            "elite1", "elite2", "elite3", "boss"
        };
        int[][] healthByAct =
        {
            new[] { 18, 16, 14, 12, 16, 10, 22, 14, 18, 42, 52, 36, 90 },
            new[] { 24, 20, 18, 16, 22, 14, 28, 18, 22, 56, 64, 46, 120 },
            new[] { 30, 24, 22, 20, 26, 18, 34, 22, 26, 72, 80, 60, 150 }
        };

        EnemyData[] enemies = new EnemyData[healthByAct.Length * names.Length];

        for (int act = 0; act < healthByAct.Length; act++)
        {
            if (healthByAct[act].Length != names.Length)
            {
                Debug.LogError($"Health count does not match enemy count for Act {act + 1}.");
                return;
            }

            for (int i = 0; i < names.Length; i++)
            {
                int index = act * names.Length + i;
                string path = $"Assets/Data/Enemies/act{act + 1}/act{act + 1}_{names[i]}.asset";
                enemies[index] = AssetDatabase.LoadAssetAtPath<EnemyData>(path);

                if (enemies[index] == null || healthByAct[act][i] <= 0)
                {
                    Debug.LogError("Enemy asset is missing or its proposed Health is invalid: " + path);
                    return;
                }

                SerializedObject serialized = new(enemies[index]);
                if (serialized.FindProperty("<Health>k__BackingField") == null)
                {
                    Debug.LogError("EnemyData is missing the serialized Health field.", enemies[index]);
                    return;
                }
            }
        }

        // Health updates are applied as one undoable editor operation.
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply All Enemy Health");
        Undo.RegisterCompleteObjectUndo(enemies, "Apply All Enemy Health");

        try
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                SerializedObject serialized = new(enemies[i]);
                serialized.Update();
                serialized.FindProperty("<Health>k__BackingField").intValue =
                    healthByAct[i / names.Length][i % names.Length];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(enemies[i]);
            }
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
            return;
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
        Debug.Log($"Applied Health to {enemies.Length} enemies.");
    }
    private readonly struct MoveSpec
    {
        public readonly EnemyMoveType Type;
        public readonly int Damage, Hits, Armor;
        public readonly float Chance;

        public MoveSpec(EnemyMoveType type, int damage, int hits, int armor, float chance)
        {
            Type = type;
            Damage = damage;
            Hits = hits;
            Armor = armor;
            Chance = chance;
        }
    }

    private sealed class EnemySpec
    {
        public readonly string Path;
        public readonly MoveSpec[] Moves;

        public EnemySpec(string name, params MoveSpec[] moves)
        {
            Path = $"Assets/Data/Enemies/{name}.asset";
            Moves = moves;
        }
    }

    private static MoveSpec A(
        int damage,
        int hits = 1,
        float chance = 0f
    ) => new(EnemyMoveType.Attack, damage, hits, 0, chance);
    private static MoveSpec D(int armor) => new(EnemyMoveType.Defend, 0, 1, armor, 0f);
    private static MoveSpec AD(
        int damage,
        int armor,
        int hits = 1,
        float chance = 0f
    ) => new(EnemyMoveType.AttackAndDefend, damage, hits, armor, chance);

    private static EnemySpec[] GetSpecs() => new EnemySpec[]
    {
        // Act 1
        new("act1/act1_n1", A(4), AD(4, 2)),
        new("act1/act1_n2", A(3), AD(3, 2)),
        new("act1/act1_n3", A(5), A(2, 2)),
        new("act1/act1_n4", A(3), AD(2, 2)),
        new("act1/act1_n5", AD(3, 3), D(5), A(5, chance: 10f)),
        new("act1/act1_n6", A(4), A(2, 2)),
        new("act1/act1_n7", D(6), A(4), AD(3, 3)),
        new("act1/act1_n8", A(5), A(3, 2, 10f)),
        new("act1/act1_n9", A(3), AD(2, 3, 2)),
        new("act1/act1_elite1", A(7), AD(5, 5), A(3, 2, 15f)),
        new("act1/act1_elite2", AD(4, 6), D(8), A(8, chance: 15f)),
        new("act1/act1_elite3", A(4, 2), AD(6, 3), A(3, 3, 15f)),
        new("act1/act1_boss", A(8, chance: 15f), AD(6, 6, chance: 15f), A(4, 3), D(8)),

        // Act 2
        new("act2/act2_n1", A(5), AD(5, 3)),
        new("act2/act2_n2", A(4), AD(4, 3)),
        new("act2/act2_n3", A(6), A(3, 2, 10f)),
        new("act2/act2_n4", A(4), AD(3, 3)),
        new("act2/act2_n5", AD(4, 5), D(7), A(6, chance: 10f)),
        new("act2/act2_n6", A(5), A(3, 2)),
        new("act2/act2_n7", D(8), A(5), AD(4, 5, chance: 10f)),
        new("act2/act2_n8", A(6), A(4, 2, 10f)),
        new("act2/act2_n9", A(4), AD(3, 4, 2, 10f)),
        new("act2/act2_elite1", A(9), AD(7, 7), A(4, 2, 20f)),
        new("act2/act2_elite2", AD(6, 8), D(11), A(10, chance: 20f)),
        new("act2/act2_elite3", A(5, 2), AD(8, 4), A(4, 3, 20f)),
        new("act2/act2_boss", A(10, chance: 20f), AD(8, 8, chance: 20f), A(5, 3), D(10)),

        // Act 3
        new("act3/act3_n1", A(6), AD(6, 4)),
        new("act3/act3_n2", A(5), AD(5, 4)),
        new("act3/act3_n3", A(7), A(3, 2, 15f)),
        new("act3/act3_n4", A(5), AD(4, 4)),
        new("act3/act3_n5", AD(5, 7), D(9), A(7, chance: 15f)),
        new("act3/act3_n6", A(6), A(3, 2, 15f)),
        new("act3/act3_n7", D(10), A(6), AD(5, 7, chance: 15f)),
        new("act3/act3_n8", A(8), A(3, 3, 15f)),
        new("act3/act3_n9", A(5), AD(4, 5, 2, 15f)),
        new("act3/act3_elite1", A(11), AD(9, 9), A(5, 2, 20f)),
        new("act3/act3_elite2", AD(8, 10), D(14), A(12, chance: 20f)),
        new("act3/act3_elite3", A(6, 2), AD(10, 5), A(5, 3, 20f)),
        new("act3/act3_boss", A(12, chance: 25f), AD(10, 10, chance: 25f), A(6, 3), D(12))
    };

    [MenuItem("Tools/Enemies/Apply All Enemy Moves")]
    public static void ApplyAllMoves()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before applying enemy Moves.");
            return;
        }

        EnemySpec[] specs = GetSpecs();
        EnemyData[] enemies = new EnemyData[specs.Length];

        for (int i = 0; i < specs.Length; i++)
        {
            enemies[i] = AssetDatabase.LoadAssetAtPath<EnemyData>(specs[i].Path);
            if (enemies[i] == null)
            {
                Debug.LogError("Enemy asset not found: " + specs[i].Path);
                return;
            }

            SerializedObject serialized = new(enemies[i]);
            if (serialized.FindProperty("<Moves>k__BackingField") == null)
            {
                Debug.LogError("EnemyData is missing the serialized Moves field.", enemies[i]);
                return;
            }
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply All Enemy Moves");
        Undo.RegisterCompleteObjectUndo(enemies, "Apply All Enemy Moves");

        try
        {
            for (int i = 0; i < specs.Length; i++)
            {
                SerializedObject serialized = new(enemies[i]);
                serialized.Update();

                SerializedProperty moves = serialized.FindProperty("<Moves>k__BackingField");
                moves.arraySize = specs[i].Moves.Length;

                for (int j = 0; j < specs[i].Moves.Length; j++)
                {
                    MoveSpec spec = specs[i].Moves[j];
                    SerializedProperty move = moves.GetArrayElementAtIndex(j);

                    SetInt(move, "MoveType", (int)spec.Type);
                    SetInt(move, "DamagePerHit", spec.Damage);
                    SetInt(move, "HitCount", spec.Hits);
                    SetInt(move, "ArmorGain", spec.Armor);
                    SetInt(move, "DebuffStacks", 1);
                    move.FindPropertyRelative("<ApplyRandomDebuff>k__BackingField").boolValue = spec.Chance > 0f;
                    move.FindPropertyRelative("<DebuffChancePercent>k__BackingField").floatValue =
                        spec.Chance > 0f ? 100f : 0f;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(enemies[i]);
            }
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
            return;
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
        Debug.Log($"Applied Moves to {enemies.Length} enemies.");
    }

    private static void SetInt(SerializedProperty move, string name, int value)
    {
        move.FindPropertyRelative($"<{name}>k__BackingField").intValue = value;
    }
}