using System;
using System.Collections.Generic;
using UnityEngine;

public partial class RunSystem
{
    public bool SaveCheckpoint()
    {
        if (IsRunFinished || HeroData == null || CurrentHealth <= 0 || CurrentAct == null || Deck == null || MapPoints.Count == 0 || SelectedMapPointId >= 0) return false;
        if (GetMapPoint(CompletedMapPointId)?.Type == MapPointType.Boss) return false;

        RunSaveCatalog catalog = Resources.Load<RunSaveCatalog>("RunSaveCatalog");
        if (catalog == null)
        {
            Debug.LogError("Run Rebuild Save Catalog before saving.");
            return false;
        }

        RunSaveData data = new()
        {
            hero = HeroData.Character,
            actName = CurrentAct.name,
            actIndex = CurrentActIndex,
            stage = CurrentStage,
            completedPointId = CompletedMapPointId,
            health = CurrentHealth,
            gold = Gold,
            healthPotions = HealthPotionCount,
            manaPotions = ManaPotionCount,
            elapsedTime = ElapsedRunTime,
            lastNormal = LastNormalEncounter != null ? LastNormalEncounter.name : "",
            lastElite = LastEliteEncounter != null ? LastEliteEncounter.name : ""
        };

        foreach (CardData card in Deck)
        {
            if (card == null || catalog.FindCard(card.SaveId) == null)
            {
                Debug.LogError("A deck card is missing from the save catalog. Rebuild Save Catalog.");
                return false;
            }

            data.deck.Add(new SavedDeckCard { id = card.SaveId, upgraded = IsCardUpgraded(card) });
        }

        foreach (MapPoint point in MapPoints)
            data.map.Add(new SavedMapPoint { id = point.Id, floor = point.Floor, position = point.Position, type = point.Type, next = new List<int>(point.NextPoints) });

        return RunSaveSystem.Write(data);
    }

    public bool TryLoadCheckpoint()
    {
        RunSaveCatalog catalog = Resources.Load<RunSaveCatalog>("RunSaveCatalog");
        if (catalog == null || !RunSaveSystem.TryRead(out RunSaveData data, candidate => IsSaveValid(candidate, catalog)))
        {
            Debug.LogError("Run save is missing, invalid, or incompatible. Check the save catalog.");
            return false;
        }

        ClearRuntimeUpgrades();
        startingHero = HeroData = catalog.FindHero(data.hero);
        Deck = new List<CardData>();

        foreach (SavedDeckCard saved in data.deck)
        {
            Deck.Add(catalog.FindCard(saved.id));
            if (saved.upgraded) UpgradeDeckCard(Deck.Count - 1);
        }

        CurrentActIndex = data.actIndex;
        CurrentStage = data.stage;
        CurrentHealth = data.health;
        Gold = data.gold;
        HealthPotionCount = data.healthPotions;
        ManaPotionCount = data.manaPotions;
        ElapsedRunTime = data.elapsedTime;
        CompletedMapPointId = data.completedPointId;
        SelectedMapPointId = -1;
        CurrentEncounter = null;
        Result = RunResult.None;
        isRunTimerRunning = false;
        ResetRewards();

        LastNormalEncounter = CurrentAct.NormalEncounters?.Find(encounter => encounter != null && encounter.name == data.lastNormal);
        LastEliteEncounter = CurrentAct.EliteEncounters?.Find(encounter => encounter != null && encounter.name == data.lastElite);

        MapPoints = new List<MapPoint>();
        foreach (SavedMapPoint saved in data.map)
        {
            MapPoint point = new(saved.id, saved.floor, saved.position, saved.type);
            foreach (int next in saved.next) point.AddNextPoint(next);
            MapPoints.Add(point);
        }
        return true;
    }

    private bool IsSaveValid(RunSaveData data, RunSaveCatalog catalog)
    {
        HeroData hero = catalog.FindHero(data.hero);
        if (data.version != 1 || hero == null || acts == null || data.actIndex < 0 || data.actIndex >= acts.Length) return false;

        ActData act = acts[data.actIndex];
        if (act == null || act.name != data.actName || data.health <= 0 || data.health > hero.Health || data.gold < 0 || data.healthPotions < 0 || data.manaPotions < 0) return false;
        if (float.IsNaN(data.elapsedTime) || float.IsInfinity(data.elapsedTime) || data.elapsedTime < 0f || data.deck == null || data.map == null || data.map.Count == 0) return false;

        foreach (SavedDeckCard saved in data.deck)
        {
            if (saved == null) return false;
            CardData card = catalog.FindCard(saved.id);
            if (card == null || (saved.upgraded && !card.CanUpgrade)) return false;
        }

        Dictionary<int, SavedMapPoint> points = new();
        int bosses = 0;
        bool hasStart = false;

        foreach (SavedMapPoint point in data.map)
        {
            if (point == null || point.id < 0 || point.floor < 0 || points.ContainsKey(point.id) || point.next == null || !Enum.IsDefined(typeof(MapPointType), point.type)) return false;
            if (float.IsNaN(point.position.x) || float.IsNaN(point.position.y) || float.IsInfinity(point.position.x) || float.IsInfinity(point.position.y)) return false;

            points.Add(point.id, point);
            if (point.floor == 0) hasStart = true;
            if (point.type == MapPointType.Boss) bosses++;
        }

        if (!hasStart || bosses != 1 || data.completedPointId < -1) return false;
        if (data.completedPointId == -1 ? data.stage != 0 : !points.TryGetValue(data.completedPointId, out SavedMapPoint completed) || completed.type == MapPointType.Boss || data.stage != completed.floor + 1) return false;

        foreach (SavedMapPoint point in data.map)
        {
            if (point.type == MapPointType.Boss ? point.next.Count != 0 : point.next.Count == 0) return false;

            foreach (int next in point.next)
                if (!points.TryGetValue(next, out SavedMapPoint target) || target.floor != point.floor + 1) return false;
        }
        return true;
    }
}