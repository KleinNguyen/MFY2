using System.Collections.Generic;
using UnityEngine;

public static class MapGeneratorSystem
{
    private const int FLOOR_COUNT = 10;
    private const int COLUMN_COUNT = 7;

    public static List<MapPoint> GenerateMap()
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

        Vector2 bossPosition = new(
            600f,
            150f + FLOOR_COUNT * 160f
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
                MapPointType.Battle
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
            float y = 150f + floor * 160f;

            MapPointType type = GetRandomPointType();

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
    private static MapPointType GetRandomPointType()
    {
        int randomValue = Random.Range(0, 100);

        if (randomValue < 65) return MapPointType.Battle;
        if (randomValue < 82) return MapPointType.Shop;

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
}