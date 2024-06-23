using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public TileType Type;
    new public SpriteRenderer renderer;
    public Vector3Int Coordinates;

    public void Init(Vector3Int cellCoordinates)
    {
        Coordinates = cellCoordinates;
    }

    public void SetTileType(TileType tileType)
    {
        if (Type == TileType.HardLimit)
            return;

        Type = tileType;

        switch (tileType)
        {
            case TileType.Room:
                renderer.color = Color.green;
                break;
            case TileType.Corridor:
                renderer.color = Color.cyan;
                break;
            case TileType.Water:
                renderer.color = Color.blue;
                break;
            case TileType.Wall:
                renderer.color = Color.red;
                break;
            case TileType.RoomWall:
                renderer.color = Color.yellow;
                break;
            case TileType.HardLimit:
                renderer.color = Color.black;
                break;
        }
    }
}
