using System.Collections.Generic;
using UnityEngine;

public static class MapGeneratorSystem
{
    private const int FLOOR_COUNT = 9;
    private const float FLOOR_SPACING = 300f;
    //private const int COLUMN_COUNT = 7;

    public static List<MapPoint> GenerateMap(ActData actData)
    {
        List<MapPoint> mapPoints = new();
        List<List<MapPoint>> floors = new();

        int nextId = 0;

        for (int floor = 0; floor < FLOOR_COUNT; floor++)
        {
            List<MapPoint> pointsOnFloor = CreateFloor(floor, ref nextId);

            floors.Add(pointsOnFloor);
            mapPoints.AddRange(pointsOnFloor);
        }

        for (int floor = 0; floor < floors.Count - 1; floor++)
        {
            ConnectFloors(floors[floor], floors[floor + 1]);
        }
        AssignPointTypes(floors, actData);

        Vector2 bossPosition = new(
            600f,
            150f + FLOOR_COUNT * FLOOR_SPACING
        );

        MapPoint boss = new(
            nextId,
            FLOOR_COUNT,
            bossPosition,
            MapPointType.Boss
        );

        foreach (MapPoint point in floors[floors.Count - 1])
        {
            point.AddNextPoint(boss.Id);
        }

        mapPoints.Add(boss);

        return mapPoints;
    }

    private static List<MapPoint> CreateFloor(int floor, ref int nextId)
    {
        List<MapPoint> points = new();
        const float mapWidth = 1200f;

        int pointCount = floor == 0 ? 1 : Random.Range(2, 5);
        float spacing = mapWidth / (pointCount + 1);

        for (int i = 0; i < pointCount; i++)
        {
            float x = spacing * (i + 1);
            float y = 150f + floor * FLOOR_SPACING;

            MapPoint point = new(
                nextId,
                floor,
                new Vector2(x, y),
                MapPointType.Normal
            );

            points.Add(point);
            nextId++;
        }

        return points;
    }
    private static MapPointType GetRandomPointType(ActData actData, bool allowElite, bool allowShop, bool allowRest)
    {
        int normal = Mathf.Max(0, actData.NormalWeight);
        int elite = allowElite ? Mathf.Max(0, actData.EliteWeight) : 0;
        int shop = allowShop ? Mathf.Max(0, actData.ShopWeight) : 0;
        int rest = allowRest ? Mathf.Max(0, actData.RestWeight) : 0;
        int total = normal + elite + shop + rest;
        if (total <= 0) return MapPointType.Normal;

        int roll = Random.Range(0, total);
        if (roll < normal) return MapPointType.Normal;
        roll -= normal;
        if (roll < elite) return MapPointType.Elite;
        roll -= elite;
        return roll < shop ? MapPointType.Shop : MapPointType.Rest;
    }

    private static void ConnectFloors(
        List<MapPoint> currentFloor,
        List<MapPoint> nextFloor
    )
    {
        foreach (MapPoint currentPoint in currentFloor)
        {
            MapPoint nearestPoint = GetNearestPoint(currentPoint, nextFloor);

            currentPoint.AddNextPoint(nearestPoint.Id);

            if (nextFloor.Count > 1 && Random.value < 0.5f)
            {
                int randomIndex = Random.Range(0, nextFloor.Count);

                currentPoint.AddNextPoint(nextFloor[randomIndex].Id);
            }
        }

        foreach (MapPoint nextPoint in nextFloor)
        {
            bool hasIncomingConnection = false;

            foreach (MapPoint currentPoint in currentFloor)
            {
                if (currentPoint.NextPoints.Contains(nextPoint.Id))
                {
                    hasIncomingConnection = true;
                    break;
                }
            }

            if (hasIncomingConnection) continue;

            MapPoint nearestPoint = GetNearestPoint(nextPoint, currentFloor);

            nearestPoint.AddNextPoint(nextPoint.Id);
        }
    }

    private static MapPoint GetNearestPoint(
        MapPoint point,
        List<MapPoint> possiblePoints
    )
    {
        MapPoint nearestPoint = possiblePoints[0];

        float nearestDistance = Mathf.Abs(
            point.Position.x - nearestPoint.Position.x
        );

        foreach (MapPoint possiblePoint in possiblePoints)
        {
            float distance = Mathf.Abs(
                point.Position.x - possiblePoint.Position.x
            );

            if (distance >= nearestDistance) continue;

            nearestPoint = possiblePoint;
            nearestDistance = distance;
        }

        return nearestPoint;
    }
    private static void AssignPointTypes(List<List<MapPoint>> floors, ActData actData)
    {
        if (actData == null || floors.Count < 6)
        {
            Debug.LogError("Assign ActData and set FLOOR_COUNT to 9 before generating the map.");
            return;
        }

        int lastFloor = floors.Count - 1;
        int earlyFloors = Mathf.Clamp(actData.EarlyNormalFloors, 1, lastFloor - 2);
        int firstEliteIndex = Mathf.Max(earlyFloors, actData.FirstEliteFloor - 1);
        List<MapPoint> normalAnchors = new();
        HashSet<MapPoint> reserved = new();

        for (int floor = 0; floor < floors.Count; floor++)
        {
            List<MapPoint> points = floors[floor];
            foreach (MapPoint point in points)
            {
                bool isEarly = floor < earlyFloors;
                bool isLast = floor == lastFloor;
                point.SetType(isEarly ? MapPointType.Normal : GetRandomPointType(
                    actData, floor >= firstEliteIndex && !isLast, !isLast, true));
            }

            MapPoint normal = points[Random.Range(0, points.Count)];
            normal.SetType(MapPointType.Normal);
            normalAnchors.Add(normal);
            reserved.Add(normal);
        }

        // Shop and an intermediate Rest move to different floors on each new map.
        int shopFloor = Random.Range(earlyFloors, lastFloor);
        List<int> restFloors = new();
        for (int floor = earlyFloors; floor < lastFloor; floor++)
        {
            if (floor != shopFloor) restFloors.Add(floor);
        }
        int restFloor = restFloors[Random.Range(0, restFloors.Count)];

        MapPoint shopPoint = PickSpecialPoint(floors[shopFloor], normalAnchors[shopFloor]);
        shopPoint.SetType(MapPointType.Shop);
        reserved.Add(shopPoint);

        MapPoint restPoint = PickSpecialPoint(floors[restFloor], normalAnchors[restFloor]);
        restPoint.SetType(MapPointType.Rest);
        reserved.Add(restPoint);

        MapPoint finalRest = PickSpecialPoint(floors[lastFloor], normalAnchors[lastFloor]);
        finalRest.SetType(MapPointType.Rest);
        reserved.Add(finalRest);

        int eliteCount = 0;
        List<MapPoint> eliteCandidates = new();
        for (int floor = 0; floor < floors.Count; floor++)
        {
            foreach (MapPoint point in floors[floor])
            {
                if (point.Type == MapPointType.Elite) eliteCount++;
                else if (floor >= firstEliteIndex && floor < lastFloor && !reserved.Contains(point))
                    eliteCandidates.Add(point);
            }
        }

        int requiredElites = Mathf.Max(0, actData.MinimumElitePoints);
        while (eliteCount < requiredElites && eliteCandidates.Count > 0)
        {
            int index = Random.Range(0, eliteCandidates.Count);
            eliteCandidates[index].SetType(MapPointType.Elite);
            eliteCandidates.RemoveAt(index);
            eliteCount++;
        }

        if (eliteCount < requiredElites)
            Debug.LogWarning("Not enough eligible points for Minimum Elite Points. Check the Act settings.");

        RepairPointConnections(floors);
    }

    private static MapPoint PickSpecialPoint(List<MapPoint> points, MapPoint normalAnchor)
    {
        List<MapPoint> candidates = points.FindAll(point => point != normalAnchor);
        return candidates[Random.Range(0, candidates.Count)];
    }

    private static void RepairPointConnections(List<List<MapPoint>> floors)
    {
        for (int floor = 1; floor < floors.Count; floor++)
        {
            List<MapPoint> previous = floors[floor - 1];
            List<MapPoint> current = floors[floor];
            List<MapPoint> normalPoints = current.FindAll(point => point.Type == MapPointType.Normal);

            foreach (MapPoint point in previous)
            {
                point.NextPoints.RemoveAll(id => current.Exists(next =>
                    next.Id == id && next.Type != MapPointType.Normal && next.Type == point.Type));
                if (!normalPoints.Exists(normal => point.NextPoints.Contains(normal.Id)))
                    point.AddNextPoint(GetNearestPoint(point, normalPoints).Id);
            }

            foreach (MapPoint point in current)
            {
                if (previous.Exists(parent => parent.NextPoints.Contains(point.Id))) continue;
                List<MapPoint> parents = previous.FindAll(parent =>
                    point.Type == MapPointType.Normal || parent.Type != point.Type);
                GetNearestPoint(point, parents).AddNextPoint(point.Id);
            }

            if (floor != floors.Count - 1) continue;
            List<MapPoint> restPoints = current.FindAll(point => point.Type == MapPointType.Rest);
            foreach (MapPoint point in previous)
            {
                if (point.Type != MapPointType.Rest && !restPoints.Exists(rest => point.NextPoints.Contains(rest.Id)))
                    point.AddNextPoint(GetNearestPoint(point, restPoints).Id);
            }
        }
    }
}
