using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
[Serializable]
public class RunSaveData
{
    public int version = 1;
    public CharacterType hero;
    public string actName, lastNormal, lastElite;
    public int actIndex, stage, completedPointId = -1;
    public int health, gold, healthPotions, manaPotions;
    public float elapsedTime;
    public bool resumeBattle;
    public int battlePointId = -1, mysteryResult = -1;
    public string battleEncounter;
    public int cardRemovalPrice;
    public List<SavedDeckCard> deck = new();
    public List<SavedMapPoint> map = new();
}

[Serializable]
public class SavedDeckCard
{
    public string id;
    public bool upgraded;
}

[Serializable]
public class SavedMapPoint
{
    public int id, floor;
    public Vector2 position;
    public MapPointType type;
    public List<int> next = new();
}
public static class RunSaveSystem
{
    public static string SavePath => Path.Combine(Application.persistentDataPath, "run-save.json");
    public static bool HasSave => File.Exists(SavePath) || File.Exists(SavePath + ".bak");

    public static bool Write(RunSaveData data)
    {
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(SavePath + ".tmp", JsonUtility.ToJson(data, true));

            if (File.Exists(SavePath)) File.Copy(SavePath, SavePath + ".bak", true);

            File.Copy(SavePath + ".tmp", SavePath, true);
            File.Delete(SavePath + ".tmp");
            return true;
        }
        catch (Exception error)
        {
            Debug.LogError("Cannot save run: " + error.Message);
            return false;
        }
    }

    public static bool TryRead(out RunSaveData data, Func<RunSaveData, bool> validate)
    {
        data = null;

        foreach (string path in new[] { SavePath, SavePath + ".bak" })
        {
            if (!File.Exists(path)) continue;

            try
            {
                RunSaveData candidate = JsonUtility.FromJson<RunSaveData>(File.ReadAllText(path));
                if (candidate != null && validate(candidate))
                {
                    data = candidate;
                    return true;
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("Cannot read run save: " + error.Message);
            }
        }
        return false;
    }

    public static void Delete()
    {
        try
        {
            foreach (string suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(SavePath + suffix)) File.Delete(SavePath + suffix);
        }
        catch (Exception error)
        {
            Debug.LogError("Cannot delete run save: " + error.Message);
        }
    }
}

