using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EncounterSetupTool
{
    private readonly struct Group
    {
        public readonly int First, Last, Weight;
        public readonly int[] Ids;

        public Group(int first, int last, int weight, params int[] ids)
        {
            First = first;
            Last = last;
            Weight = weight;
            Ids = ids;
        }
    }

    private static Group[][] GetGroups() => new Group[][]
    {
        // Act 1: new(tangDau, tangCuoi, weight, cacSoQuai)
        new Group[]
        {
            new(1, 3, 3, 1),
            new(1, 3, 3, 2),
            new(1, 4, 2, 3),
            new(3, 6, 3, 4, 6),
            new(4, 9, 3, 1, 5),
            new(4, 9, 3, 2, 7),
            new(5, 9, 2, 6, 8),
            new(6, 9, 2, 4, 6, 9),
            new(7, 9, 2, 1, 2, 5)
        },

        // Act 2
        new Group[]
        {
            new(1, 2, 2, 1),
            new(1, 4, 3, 2, 4),
            new(1, 5, 2, 3, 6),
            new(3, 9, 3, 1, 5),
            new(3, 9, 3, 2, 7),
            new(4, 9, 2, 4, 8),
            new(4, 9, 2, 1, 9),
            new(6, 9, 2, 2, 4, 5),
            new(6, 9, 2, 1, 4, 6)
        },

        // Act 3
        new Group[]
        {
            new(1, 5, 3, 1, 4),
            new(1, 6, 3, 2, 3),
            new(1, 6, 2, 4, 6),
            new(3, 9, 3, 2, 5),
            new(3, 9, 3, 1, 7),
            new(4, 9, 2, 4, 8),
            new(4, 9, 2, 1, 9),
            new(6, 9, 2, 1, 2, 4),
            new(6, 9, 2, 2, 4, 6)
        }
    };

    [MenuItem("Tools/Encounters/Apply All Encounters")]
    public static void ApplyAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Group[][] groups = GetGroups();
        string[] names =
        {
            "n1", "n2", "n3", "n4", "n5", "n6", "n7", "n8", "n9",
            "elite1", "elite2", "elite3", "boss"
        };
        ActData[] acts = new ActData[3];
        EnemyData[,] enemies = new EnemyData[3, 13];

        try
        {
            for (int act = 0; act < 3; act++)
            {
                acts[act] = Load<ActData>($"Assets/Data/Acts/Act{act + 1}.asset");
                if (!AssetDatabase.IsValidFolder($"Assets/Data/Encounters/Act{act + 1}"))
                    throw new Exception($"Encounter folder for Act {act + 1} is missing.");

                for (int i = 0; i < names.Length; i++)
                {
                    enemies[act, i] = Load<EnemyData>(
                        $"Assets/Data/Enemies/act{act + 1}/act{act + 1}_{names[i]}.asset"
                    );
                    EnemyRank rank = i < 9 ? EnemyRank.Normal : i < 12 ? EnemyRank.Elite : EnemyRank.Boss;

                    if (enemies[act, i].Rank != rank ||
                        enemies[act, i].Health <= 0 ||
                        enemies[act, i].Moves == null ||
                        enemies[act, i].Moves.Count == 0)
                        throw new Exception($"Invalid rank, Health or Moves: {enemies[act, i].name}");
                }

                foreach (Group group in groups[act])
                {
                    int debuffers = 0;

                    foreach (int id in group.Ids)
                    {
                        if (enemies[act, id - 1].Moves.Exists(
                                move => move != null &&
                                    move.ApplyRandomDebuff &&
                                    move.DebuffChancePercent > 0f
                            ))
                            debuffers++;
                    }

                    if (debuffers > 1)
                    {
                        throw new Exception(
                            $"A normal group in Act {act + 1} has more than one debuff enemy."
                        );
                    }
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return;
        }

        // Keep all generated asset changes together in one undo operation.
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply All Encounters");
        List<string> created = new();

        try
        {
            for (int act = 0; act < 3; act++)
            {
                List<EncounterData> normals = new();
                List<EncounterData> elites = new();

                for (int i = 0; i < groups[act].Length; i++)
                {
                    Group group = groups[act][i];
                    EnemyData[] members = Array.ConvertAll(group.Ids, id => enemies[act, id - 1]);
                    normals.Add(Write(
                        act + 1,
                        $"Normal_{i + 1:00}",
                        EncounterType.Normal,
                        group,
                        members,
                        created
                    ));
                }

                for (int i = 0; i < 3; i++)
                {
                    Group group = new(Mathf.Clamp(acts[act].FirstEliteFloor, 1, 9), 9, 1);
                    elites.Add(Write(
                        act + 1,
                        $"Elite_{i + 1:00}",
                        EncounterType.Elite,
                        group,
                        new[] { enemies[act, 9 + i] },
                        created
                    ));
                }

                EncounterData boss = Write(
                    act + 1,
                    "Boss",
                    EncounterType.Boss,
                    new Group(10, 10, 1),
                    new[] { enemies[act, 12] },
                    created
                );
                Undo.RegisterCompleteObjectUndo(acts[act], "Configure Act Encounters");

                SerializedObject serialized = new(acts[act]);
                serialized.Update();
                SetRefs(serialized, "NormalEncounters", normals);
                SetRefs(serialized, "EliteEncounters", elites);
                serialized.FindProperty("<BossEncounter>k__BackingField").objectReferenceValue = boss;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(acts[act]);
            }
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            foreach (string path in created) AssetDatabase.DeleteAsset(path);
            Debug.LogException(exception);
            return;
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
        Debug.Log("Configured 39 encounters: 27 Normal, 9 Elite, 3 Boss.");
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new Exception("Missing asset: " + path);
        return asset;
    }

    private static EncounterData Write(
        int act,
        string suffix,
        EncounterType type,
        Group group,
        EnemyData[] members,
        List<string> created
    )
    {
        string path = $"Assets/Data/Encounters/Act{act}/Act{act}_{suffix}.asset";
        UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
        if (existing != null && existing is not EncounterData) throw new Exception("Wrong asset type: " + path);

        EncounterData encounter = existing as EncounterData;

        if (encounter == null)
        {
            encounter = ScriptableObject.CreateInstance<EncounterData>();
            AssetDatabase.CreateAsset(encounter, path);
            created.Add(path);
            Undo.RegisterCreatedObjectUndo(encounter, "Create Encounter");
        }
        else Undo.RegisterCompleteObjectUndo(encounter, "Configure Encounter");

        SerializedObject serialized = new(encounter);
        serialized.Update();
        SetRefs(serialized, "Enemies", members);
        serialized.FindProperty("<Type>k__BackingField").intValue = (int)type;
        serialized.FindProperty("<MinFloor>k__BackingField").intValue = group.First;
        serialized.FindProperty("<MaxFloor>k__BackingField").intValue = group.Last;
        serialized.FindProperty("<Weight>k__BackingField").intValue = group.Weight;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(encounter);
        return encounter;
    }

    private static void SetRefs<T>(
        SerializedObject serialized,
        string name,
        IList<T> values
    ) where T : UnityEngine.Object
    {
        SerializedProperty list = serialized.FindProperty($"<{name}>k__BackingField");
        list.arraySize = values.Count;

        for (int i = 0; i < values.Count; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}