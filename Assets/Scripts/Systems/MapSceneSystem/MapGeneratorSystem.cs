using System.Collections.Generic;
using UnityEngine;

public static class MapGeneratorSystem
{
    // private const int FLOOR_COUNT = 2;
    private const int FLOOR_COUNT = 10;
    private const float FLOOR_SPACING = 300f;
    private const int COLUMN_COUNT = 7;

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

        if (floor == 0)
        {
            MapPoint startingPoint = new(
                nextId,
                floor,
                new Vector2(600f, 150f),
                MapPointType.Normal
            );

            points.Add(startingPoint);
            nextId++;

            return points;
        }

        List<int> availableColumns = new();

        for (int i = 0; i < COLUMN_COUNT; i++)
        {
            availableColumns.Add(i);
        }

        int pointCount = Random.Range(2, 5);

        for (int i = 0; i < pointCount; i++)
        {
            int randomIndex = Random.Range(0, availableColumns.Count);

            int column = availableColumns[randomIndex];
            availableColumns.RemoveAt(randomIndex);

            float randomOffset = Random.Range(-30f, 30f);

            float x = 150f + column * 150f + randomOffset;
            float y = 150f + floor * FLOOR_SPACING;

            MapPointType type = MapPointType.Normal;

            MapPoint point = new(
                nextId,
                floor,
                new Vector2(x, y),
                type
            );

            points.Add(point);
            nextId++;
        }

        return points;
    }
    private static MapPointType GetRandomPointType(ActData actData)
    {
        int normalWeight = Mathf.Max(0, actData.NormalWeight);
        int eliteWeight = Mathf.Max(0, actData.EliteWeight);
        int shopWeight = Mathf.Max(0, actData.ShopWeight);
        int restWeight = Mathf.Max(0, actData.RestWeight);

        int totalWeight =
            normalWeight +
            eliteWeight +
            shopWeight +
            restWeight;

        if (totalWeight <= 0)
        {
            Debug.LogWarning(
                "All map point weights are zero. Using Normal."
            );

            return MapPointType.Normal;
        }

        int randomValue = Random.Range(0, totalWeight);

        if (randomValue < normalWeight)
        {
            return MapPointType.Normal;
        }

        randomValue -= normalWeight;

        if (randomValue < eliteWeight)
        {
            return MapPointType.Elite;
        }

        randomValue -= eliteWeight;

        if (randomValue < shopWeight)
        {
            return MapPointType.Shop;
        }

        return MapPointType.Rest;
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
    private static void AssignPointTypes(List<List<MapPoint>> floors, ActData actData
    )
    {
        for (int floor = 1; floor < floors.Count; floor++)
        {
            List<MapPoint> previousFloor = floors[floor - 1];
            List<MapPoint> currentFloor = floors[floor];

            foreach (MapPoint currentPoint in currentFloor)
            {
                MapPointType pointType = GetRandomPointType(actData);

                if (pointType == MapPointType.Shop)
                {
                    foreach (MapPoint previousPoint in previousFloor)
                    {
                        if (previousPoint.Type != MapPointType.Shop) continue;

                        if (previousPoint.NextPoints.Contains(currentPoint.Id))
                        {
                            pointType = MapPointType.Normal;
                            break;
                        }
                    }
                }

                currentPoint.SetType(pointType);
            }
        }
    }
}