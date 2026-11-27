using System;
using System.Collections.Generic;
using UnityEngine;

public partial class RunSystem
{
    private RunSaveData battleStartSave;

    public string ContinueSceneName =>
        CurrentEncounter != null && SelectedMapPointId >= 0
            ? "InGameScene"
            : "MapScene";
    public int SavedMysteryResult => battleStartSave?.mysteryResult ?? -1;
    public bool CanRestartBattle =>
        !IsRunFinished &&
        battleStartSave != null &&
        CurrentEncounter != null &&
        SelectedMapPointId == battleStartSave.battlePointId &&
        CurrentEncounter.name == battleStartSave.battleEncounter;

    public void PauseRunTimer() => isRunTimerRunning = false;

    public bool SaveCheckpoint()
    {
        if (IsRunFinished || HeroData == null || CurrentHealth <= 0 ||
            CurrentAct == null || Deck == null || MapPoints.Count == 0 ||
            SelectedMapPointId >= 0)
            return false;
        if (GetMapPoint(CompletedMapPointId)?.Type == MapPointType.Boss) return false;

        RunSaveData data = CreateSaveData();
        return data != null && RunSaveSystem.Write(data);
    }

    public bool SaveGame()
    {
        if (IsRunFinished) return false;
        if (!CanRestartBattle) return SaveCheckpoint();

        battleStartSave.elapsedTime = ElapsedRunTime;
        return RunSaveSystem.Write(battleStartSave);
    }

    public bool CaptureBattleStart()
    {
        if (CanRestartBattle) return true;
        if (IsRunFinished || CurrentEncounter == null || GetMapPoint(SelectedMapPointId) == null) return false;

        RunSaveData data = CreateSaveData();
        if (data == null) return false;

        data.resumeBattle = true;
        data.battlePointId = SelectedMapPointId;
        data.battleEncounter = CurrentEncounter.name;
        data.mysteryResult = -1;
        battleStartSave = data;
        RunSaveSystem.Write(data);
        return true;
    }

    public void SaveMysteryResult(int result)
    {
        if (!CanRestartBattle || result < 0 || result > 5 ||
            GetMapPoint(SelectedMapPointId)?.Type != MapPointType.Mystery)
            return;
        if (battleStartSave.mysteryResult >= 0) return;

        battleStartSave.mysteryResult = result;
        SaveGame();
    }

    public bool RestartBattle()
    {
        if (!CanRestartBattle) return false;

        RunSaveCatalog catalog = Resources.Load<RunSaveCatalog>("RunSaveCatalog");
        if (catalog == null || !IsSaveValid(battleStartSave, catalog)) return false;

        battleStartSave.elapsedTime = ElapsedRunTime;
        if (!RunSaveSystem.Write(battleStartSave)) return false;

        ApplySaveData(battleStartSave, catalog);
        return true;
    }

    public bool TryLoadCheckpoint()
    {
        RunSaveCatalog catalog = Resources.Load<RunSaveCatalog>("RunSaveCatalog");
        if (catalog == null ||
            !RunSaveSystem.TryRead(
                out RunSaveData data,
                candidate => IsSaveValid(candidate, catalog)
            ))
        {
            Debug.LogError("Run save is missing, invalid, or incompatible. Check the save catalog.");
            return false;
        }

        ApplySaveData(data, catalog);
        return true;
    }

    private RunSaveData CreateSaveData()
    {
        RunSaveCatalog catalog = Resources.Load<RunSaveCatalog>("RunSaveCatalog");
        if (catalog == null || HeroData == null || CurrentAct == null || Deck == null)
        {
            Debug.LogError("Run Rebuild Save Catalog and check the active run before saving.");
            return null;
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
            cardRemovalPrice = CardRemovalPrice,
            healthPotions = HealthPotionCount,
            manaPotions = ManaPotionCount,
            elapsedTime = ElapsedRunTime,
            upgradedCardIds = new List<string>(upgradedCardIds),
            lastNormal = LastNormalEncounter != null ? LastNormalEncounter.name : "",
            lastElite = LastEliteEncounter != null ? LastEliteEncounter.name : ""
        };
        foreach (string id in upgradedCardIds)
        {
            CardData card = catalog.FindCard(id);
            if (card == null || !card.CanUpgrade)
            {
                Debug.LogError("An upgraded card type is missing or invalid. Rebuild Save Catalog.");
                return null;
            }
        }

        foreach (CardData card in Deck)
        {
            if (card == null || catalog.FindCard(card.SaveId) == null)
            {
                Debug.LogError("A deck card is missing from the save catalog. Rebuild Save Catalog.");
                return null;
            }
            data.deck.Add(new SavedDeckCard { id = card.SaveId, upgraded = IsCardUpgraded(card) });
        }

        foreach (MapPoint point in MapPoints)
        {
            data.map.Add(new SavedMapPoint
            {
                id = point.Id,
                floor = point.Floor,
                position = point.Position,
                type = point.Type,
                next = new List<int>(point.NextPoints)
            });
        }

        return data;
    }

    private void ApplySaveData(RunSaveData data, RunSaveCatalog catalog)
    {
        ClearRuntimeUpgrades();
        startingHero = HeroData = catalog.FindHero(data.hero);
        Deck = new List<CardData>();

        if (data.upgradedCardIds != null)
            upgradedCardIds.UnionWith(data.upgradedCardIds);

        foreach (SavedDeckCard saved in data.deck)
        {
            Deck.Add(catalog.FindCard(saved.id));
            if (saved.upgraded) upgradedCardIds.Add(saved.id);
        }

        CurrentActIndex = data.actIndex;
        CurrentStage = data.stage;
        CurrentHealth = data.health;
        Gold = data.gold;
        CardRemovalPrice = data.cardRemovalPrice > 0 ? data.cardRemovalPrice : StartingCardRemovalPrice;
        HealthPotionCount = data.healthPotions;
        ManaPotionCount = data.manaPotions;
        ElapsedRunTime = data.elapsedTime;
        CompletedMapPointId = data.completedPointId;
        SelectedMapPointId = data.resumeBattle ? data.battlePointId : -1;
        CurrentEncounter = data.resumeBattle ? FindSavedEncounter(data) : null;
        Result = RunResult.None;
        isRunTimerRunning = false;
        ResetRewards();

        battleStartSave = data.resumeBattle ? data : null;
        LastNormalEncounter = CurrentAct.NormalEncounters?.Find(
            encounter => encounter != null && encounter.name == data.lastNormal
        );
        LastEliteEncounter = CurrentAct.EliteEncounters?.Find(
            encounter => encounter != null && encounter.name == data.lastElite
        );

        MapPoints = new List<MapPoint>();
        foreach (SavedMapPoint saved in data.map)
        {
            MapPoint point = new(saved.id, saved.floor, saved.position, saved.type);
            foreach (int next in saved.next) point.AddNextPoint(next);
            MapPoints.Add(point);
        }
    }

    private EncounterData FindSavedEncounter(RunSaveData data)
    {
        MapPointType type = data.map.Find(point => point.id == data.battlePointId).type;
        ActData act = acts[data.actIndex];

        if (type == MapPointType.Boss)
        {
            return act.BossEncounter != null && act.BossEncounter.name == data.battleEncounter
                ? act.BossEncounter
                : null;
        }

        List<EncounterData> pool = type == MapPointType.Elite ? act.EliteEncounters : act.NormalEncounters;
        return pool?.Find(encounter => encounter != null && encounter.name == data.battleEncounter);
    }

    private bool IsSaveValid(RunSaveData data, RunSaveCatalog catalog)
    {
        if (data.cardRemovalPrice < 0) return false;
        HeroData hero = catalog.FindHero(data.hero);
        if (data.version != 1 || hero == null || acts == null ||
            data.actIndex < 0 || data.actIndex >= acts.Length)
            return false;

        ActData act = acts[data.actIndex];
        if (act == null || act.name != data.actName ||
            data.health <= 0 || data.health > hero.Health ||
            data.gold < 0 || data.healthPotions < 0 || data.manaPotions < 0)
            return false;

        if (float.IsNaN(data.elapsedTime) || float.IsInfinity(data.elapsedTime) ||
            data.elapsedTime < 0f || data.deck == null ||
            data.map == null || data.map.Count == 0)
            return false;

        if (data.upgradedCardIds != null)
        {
            foreach (string id in data.upgradedCardIds)
            {
                if (string.IsNullOrWhiteSpace(id)) return false;

                CardData card = catalog.FindCard(id);
                if (card == null || !card.CanUpgrade) return false;
            }
        }

        foreach (SavedDeckCard saved in data.deck)
        {
            if (saved == null) return false;
            CardData card = catalog.FindCard(saved.id);
            if (card == null || (saved.upgraded && !card.CanUpgrade)) return false;
        }

        Dictionary<int, SavedMapPoint> points = new();
        int bosses = 0;
        bool hasStart = false;

        // Build the point index before validating links between map nodes.
        foreach (SavedMapPoint point in data.map)
        {
            if (point == null || point.id < 0 || point.floor < 0 ||
                points.ContainsKey(point.id) || point.next == null ||
                !Enum.IsDefined(typeof(MapPointType), point.type))
                return false;

            if (float.IsNaN(point.position.x) || float.IsNaN(point.position.y) ||
                float.IsInfinity(point.position.x) || float.IsInfinity(point.position.y))
                return false;

            points.Add(point.id, point);
            if (point.floor == 0) hasStart = true;
            if (point.type == MapPointType.Boss) bosses++;
        }

        if (!hasStart || bosses != 1 || data.completedPointId < -1) return false;
        if (data.completedPointId == -1
            ? data.stage != 0
            : !points.TryGetValue(data.completedPointId, out SavedMapPoint completed) ||
                completed.type == MapPointType.Boss ||
                data.stage != completed.floor + 1)
            return false;

        foreach (SavedMapPoint point in data.map)
        {
            if (point.type == MapPointType.Boss ? point.next.Count != 0 : point.next.Count == 0) return false;

            foreach (int next in point.next)
            {
                if (!points.TryGetValue(next, out SavedMapPoint target) ||
                    target.floor != point.floor + 1)
                    return false;
            }
        }

        if (!data.resumeBattle) return true;
        if (!points.TryGetValue(data.battlePointId, out SavedMapPoint battle) ||
            data.mysteryResult < -1 || data.mysteryResult > 5)
            return false;
        if (battle.type != MapPointType.Normal &&
            battle.type != MapPointType.Elite &&
            battle.type != MapPointType.Boss &&
            battle.type != MapPointType.Mystery)
            return false;
        if (data.completedPointId == -1
            ? battle.floor != 0
            : !points[data.completedPointId].next.Contains(battle.id))
            return false;
        if (battle.type != MapPointType.Mystery && data.mysteryResult != -1) return false;

        return FindSavedEncounter(data) != null;
    }
    public bool ContinueFromSave()
    {
        RunSaveCatalog catalog = Resources.Load<RunSaveCatalog>(
            "RunSaveCatalog"
        );

        if (catalog == null)
        {
            Debug.LogError("RunSaveCatalog is missing.");
            return false;
        }

        bool saveLoaded = RunSaveSystem.TryRead(
            out RunSaveData data,
            candidate => IsMapContinueSaveValid(candidate, catalog)
        );

        if (!saveLoaded)
        {
            Debug.LogError(
                "Run save is missing, invalid, or incompatible. Check the save catalog."
            );

            return false;
        }

        ApplySaveData(data, catalog);
        return true;
    }

    // Continue restores map progress without resuming a battle.
    private bool IsMapContinueSaveValid(
        RunSaveData data,
        RunSaveCatalog catalog
    )
    {
        if (data == null)
        {
            return false;
        }

        data.resumeBattle = false;
        data.battlePointId = -1;
        data.battleEncounter = null;
        data.mysteryResult = -1;

        return IsSaveValid(data, catalog);
    }
}