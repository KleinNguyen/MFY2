using System.Collections.Generic;
using UnityEngine;

public class MapPoint
{
    public int Id { get; private set; }
    public int Floor { get; private set; }
    public Vector2 Position { get; private set; }
    public MapPointType Type { get; private set; }

    public List<int> NextPoints { get; private set; } = new();

    public MapPoint(int id, int floor, Vector2 position, MapPointType type)
    {
        Id = id;
        Floor = floor;
        Position = position;
        Type = type;
    }

    public void AddNextPoint(int pointId)
    {
        if (NextPoints.Contains(pointId)) return;

        NextPoints.Add(pointId);
    }
}