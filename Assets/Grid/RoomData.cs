using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoomData
{
    public Vector2Int coordinates;
    public Vector2Int size;
    public Vector2Int coordinatesOnRoomGrid;
    public RoomData(Vector2Int coordinates, Vector2Int size, Vector2Int coordinatesOnRoomGrid)
    {
        this.coordinates = coordinates;
        this.size = size;
        this.coordinatesOnRoomGrid = coordinatesOnRoomGrid;
    }
}
