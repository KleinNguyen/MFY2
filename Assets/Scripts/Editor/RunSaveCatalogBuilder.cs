using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RunSaveCatalogBuilder
{
    [MenuItem("Tools/MFY2/Rebuild Save Catalog")]
    private static void Rebuild()
    {
        const string folder = "Assets/Resources";
        const string path = folder + "/RunSaveCatalog.asset";

        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Resources");

        RunSaveCatalog catalog = AssetDatabase.LoadAssetAtPath<RunSaveCatalog>(path);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<RunSaveCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
        }

        catalog.cards = new List<CardData>();
        catalog.heroes = new List<HeroData>();

        foreach (string guid in AssetDatabase.FindAssets("t:CardData", new[] { "Assets/Data/Cards" }))
        {
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid));
            SerializedObject serialized = new(card);
            serialized.FindProperty("<SaveId>k__BackingField").stringValue = guid;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(card);
            catalog.cards.Add(card);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:HeroData", new[] { "Assets/Data" }))
            catalog.heroes.Add(AssetDatabase.LoadAssetAtPath<HeroData>(AssetDatabase.GUIDToAssetPath(guid)));

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Selection.activeObject = catalog;
        Debug.Log($"Save catalog: {catalog.cards.Count} cards, {catalog.heroes.Count} heroes.", catalog);
    }
}