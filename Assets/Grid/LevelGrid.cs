using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGrid : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] Tile tilePrefab;
    [SerializeField] Grid grid;

    [Header("Floor Size")]
    [SerializeField] int floorWidth = 56;
    [SerializeField] int floorHeight = 32;

    [Header("Rooms")]
    [SerializeField, Tooltip("Will generate between n and n*2 rooms. Min 2 rooms. Will fill the rest with dummy rooms.")]
    int roomDensity = 2;
    [SerializeField]
    int roomGridSizeX = 3;
    [SerializeField]
    int roomGridSizeY = 2;
    [SerializeField]
    int minRoomWidth = 5;
    [SerializeField]
    int minRoomHeight = 4;
    [SerializeField]
    float minRoomRatio = .666667f;
    [SerializeField]
    float roomMergeChance = .90f;

    [Header("Corridors")]
    [SerializeField]
    float roomLinkChance = .666667f;
    [SerializeField]
    int deadEndChances = 3;
    [SerializeField]
    float deadEndTurnChance = .2f;


    Dictionary<Vector3Int, Tile> tiles = new Dictionary<Vector3Int, Tile>();

    #region FloorGeneration
    [Button]
    void GenerateFloor()
    {
        if (tiles != null)
        {
            foreach (var tile in tiles.Values)
            {
                Destroy(tile.gameObject);
            }
        }

        tiles = new Dictionary<Vector3Int, Tile>();

        CreateFullWallFloor();
        CreateHardBorders();
        var rooms = CreateRooms();
        SetupRoomWallTiles(rooms);
        GenerateCorridors(rooms);
        GenerateDeadEnds(rooms);
    }

    private void CreateFullWallFloor()
    {
        for (int x = 0; x < floorWidth; x++)
            for (int y = 0; y < floorHeight; y++)
            {
                Tile newTile = Instantiate(tilePrefab);

                var cellCoordinates = new Vector3Int(x, y, 0);

                newTile.transform.position = grid.GetCellCenterWorld(cellCoordinates);

                TileType tileType = TileType.Wall;

                newTile.Init(cellCoordinates);
                newTile.SetTileType(tileType);

                tiles.Add(cellCoordinates, newTile);
            }
    }

    void CreateHardBorders()
    {
        for (int x = 0; x < floorWidth; x++)
        {
            tiles[new Vector3Int(x, 0)].SetTileType(TileType.HardLimit);
            tiles[new Vector3Int(x, floorHeight - 1)].SetTileType(TileType.HardLimit);
        }
        for (int y = 0; y < floorHeight; y++)
        {
            tiles[new Vector3Int(0, y)].SetTileType(TileType.HardLimit);
            tiles[new Vector3Int(floorWidth - 1, y)].SetTileType(TileType.HardLimit);
        }
    }

    List<RoomData> CreateRooms()
    {
        List<RoomData> roomsData = new List<RoomData>();

        int roomAmount = roomDensity < 2 ? 2 : Mathf.RoundToInt(roomDensity * Random.Range(1f, 2f));

        roomAmount = Mathf.Min(roomAmount, roomGridSizeX * roomGridSizeY);

        List<Vector2Int> gridCellsWithRooms = new List<Vector2Int>();

        //Add in real rooms
        for (int i = 0; i < roomAmount; i++)
        {
            Vector2Int currentRoomGridCell;

            do
            {
                currentRoomGridCell = new Vector2Int(
                    Random.Range(0, roomGridSizeX),
                    Random.Range(0, roomGridSizeY));
            } while (gridCellsWithRooms.Contains(currentRoomGridCell));

            gridCellsWithRooms.Add(currentRoomGridCell);

            RoomData newRoom = CreateRoom(currentRoomGridCell, false);

            for (int x = 0; x < newRoom.size.x; x++)
                for (int y = 0; y < newRoom.size.y; y++)
                {
                    Vector3Int coord = new Vector3Int(newRoom.coordinates.x + x, newRoom.coordinates.y + y);

                    tiles[coord].SetTileType(TileType.Room);
                }

            roomsData.Add(newRoom);
        }

        // Fill the unused toom grid cells with dummies rooms
        for (int x = 0; x < roomGridSizeX; x++)
        {
            for (int y = 0; y < roomGridSizeY; y++)
            {
                Vector2Int roomGridCoord = new Vector2Int(x, y);
                if (!gridCellsWithRooms.Contains(roomGridCoord))
                {
                    //Add in a dummy room

                    RoomData newRoom = CreateRoom(roomGridCoord, true);

                    Vector3Int coord = new Vector3Int(newRoom.coordinates.x, newRoom.coordinates.y);

                    tiles[coord].SetTileType(TileType.Room);

                    roomsData.Add(newRoom);
                }
            }
        }

        return roomsData;
    }

    RoomData CreateRoom(Vector2Int gridRoomCoordinates, bool isDummyRoom)
    {
        //Deciding the size of the room
        int roomWidth = Random.Range(minRoomWidth, floorWidth / roomGridSizeX - 2);
        int roomHeight = Random.Range(minRoomWidth, floorHeight / roomGridSizeY - 2);

        if (isDummyRoom)
        {
            roomWidth = 1;
            roomHeight = 1;
        }

        else
        {
            //If the ratio is wrong, reshape the room
            if (roomWidth / (float)roomHeight < minRoomRatio)
            {
                while (roomHeight > minRoomHeight && roomWidth / (float)roomHeight < minRoomRatio)
                {
                    roomHeight--;
                }

                while (roomWidth / (float)roomHeight < minRoomRatio)
                {
                    roomWidth++;
                }
            }
        }

        int roomGridCellWidth = floorWidth / roomGridSizeX;
        int roomGridCellHeight = floorHeight / roomGridSizeY;

        //Deciding the position of the room
        int roomXPos = roomGridCellWidth * gridRoomCoordinates.x + Random.Range(1, (roomGridCellWidth - roomWidth) - 1);
        int roomYPos = roomGridCellHeight * gridRoomCoordinates.y + Random.Range(1, (roomGridCellHeight - roomHeight) - 1);

        return new RoomData(new Vector2Int(roomXPos, roomYPos), new Vector2Int(roomWidth, roomHeight), gridRoomCoordinates);
    }

    void SetupRoomWallTiles(List<RoomData> rooms)
    {
        foreach (var room in rooms)
        {
            for (int x = -1; x < room.size.x + 1; x++)
            {
                Vector3Int tilesCoord = new Vector3Int(room.coordinates.x + x, room.coordinates.y - 1);

                if (tiles[tilesCoord].Type == TileType.Wall)
                    tiles[tilesCoord].SetTileType(TileType.RoomWall);

                tilesCoord = new Vector3Int(room.coordinates.x + x, room.coordinates.y + room.size.y);

                if (tiles[tilesCoord].Type == TileType.Wall)
                    tiles[tilesCoord].SetTileType(TileType.RoomWall);
            }
            for (int y = 0; y < room.size.y; y++)
            {
                Vector3Int tilesCoord = new Vector3Int(room.coordinates.x - 1, room.coordinates.y + y);

                if (tiles[tilesCoord].Type == TileType.Wall)
                    tiles[tilesCoord].SetTileType(TileType.RoomWall);

                tilesCoord = new Vector3Int(room.coordinates.x + room.size.x, room.coordinates.y + y);

                if (tiles[tilesCoord].Type == TileType.Wall)
                    tiles[tilesCoord].SetTileType(TileType.RoomWall);
            }
        }
    }

    void GenerateCorridors(List<RoomData> rooms)
    {
        Dictionary<Vector2Int, RoomData> roomsByCoord = new Dictionary<Vector2Int, RoomData>();

        foreach (RoomData room in rooms)
        {
            roomsByCoord.Add(room.coordinatesOnRoomGrid, room);
        }

        //Attempts to link from left to right
        for (int roomGridY = 0; roomGridY < roomGridSizeY; roomGridY++)
        {
            for (int roomGridX = 0; roomGridX < roomGridSizeX - 1; roomGridX++)
            {
                Vector2Int currentRoomCoord = new Vector2Int(roomGridX, roomGridY);
                Vector2Int targetRoomCoord;

                if (Random.Range(0f, 1f) < roomLinkChance)
                {
                    targetRoomCoord = currentRoomCoord;
                    targetRoomCoord.x++;
                    GenerateCorridorBetweenRooms(roomsByCoord[currentRoomCoord], roomsByCoord[targetRoomCoord], true);
                }
            }
        }

        //Attempts to link from bottom to top
        for (int roomGridX = 0; roomGridX < roomGridSizeX; roomGridX++)
        {
            for (int roomGridY = 0; roomGridY < roomGridSizeY - 1; roomGridY++)
            {
                Vector2Int currentRoomCoord = new Vector2Int(roomGridX, roomGridY);
                Vector2Int targetRoomCoord;

                if (Random.Range(0f, 1f) < roomLinkChance)
                {
                    targetRoomCoord = currentRoomCoord;
                    targetRoomCoord.y++;
                    GenerateCorridorBetweenRooms(roomsByCoord[currentRoomCoord], roomsByCoord[targetRoomCoord], false);
                }
            }
        }
    }

    void GenerateCorridorBetweenRooms(RoomData room1, RoomData room2, bool isHorizontalLink)
    {
        if (isHorizontalLink)
        {
            int colinearTileX = Random.Range(room1.coordinates.x + room1.size.x + 1, room2.coordinates.x - 1);

            Vector3Int tile1Coord = new Vector3Int(colinearTileX, Random.Range(room1.coordinates.y, room1.coordinates.y + room1.size.y));

            Vector3Int tile2Coord = new Vector3Int(colinearTileX, Random.Range(room2.coordinates.y, room2.coordinates.y + room2.size.y));

            for (int x = room1.coordinates.x + room1.size.x; x < colinearTileX + 1; x++)
            {
                Vector3Int coordToSetCorridor = new Vector3Int(x, tile1Coord.y);
                tiles[coordToSetCorridor].SetTileType(TileType.Corridor);
            }

            for (int y = Mathf.Min(tile1Coord.y, tile2Coord.y); y < Mathf.Max(tile1Coord.y, tile2Coord.y) + 1; y++)
            {
                Vector3Int coordToSetCorridor = new Vector3Int(tile1Coord.x, y);
                tiles[coordToSetCorridor].SetTileType(TileType.Corridor);
            }

            for (int x = tile2Coord.x; x < room2.coordinates.x + 1; x++)
            {
                Vector3Int coordToSetCorridor = new Vector3Int(x, tile2Coord.y);
                tiles[coordToSetCorridor].SetTileType(TileType.Corridor);
            }
        }
        else
        {
            int colinearTileY = Random.Range(room1.coordinates.y + room1.size.y + 1, room2.coordinates.y - 1);

            Vector3Int tile1Coord = new Vector3Int(Random.Range(room1.coordinates.x, room1.coordinates.x + room1.size.x), colinearTileY);

            Vector3Int tile2Coord = new Vector3Int(Random.Range(room2.coordinates.x, room2.coordinates.x + room2.size.x), colinearTileY);

            for (int y = room1.coordinates.y + room1.size.y; y < colinearTileY + 1; y++)
            {
                Vector3Int coordToSetCorridor = new Vector3Int(tile1Coord.x, y);
                tiles[coordToSetCorridor].SetTileType(TileType.Corridor);
            }

            for (int x = Mathf.Min(tile1Coord.x, tile2Coord.x); x < Mathf.Max(tile1Coord.x, tile2Coord.x) + 1; x++)
            {
                Vector3Int coordToSetCorridor = new Vector3Int(x, tile1Coord.y);
                tiles[coordToSetCorridor].SetTileType(TileType.Corridor);
            }

            for (int y = tile2Coord.y; y < room2.coordinates.y + 1; y++)
            {
                Vector3Int coordToSetCorridor = new Vector3Int(tile2Coord.x, y);
                tiles[coordToSetCorridor].SetTileType(TileType.Corridor);
            }
        }
    }

    void GenerateDeadEnds(List<RoomData> rooms)
    {
        int deadEndAmount = Random.Range(deadEndChances, deadEndChances * 2);

        for (int deadEndIndex = 0; deadEndIndex < deadEndAmount; deadEndIndex++)
        {
            RoomData startingRoom = rooms[Random.Range(0, rooms.Count)];

            int dir = Random.Range(0, 4);

            Vector3Int startingCoord = new Vector3Int();
            Vector3Int direction = new Vector3Int();

            switch (dir)
            {
                //top
                case 0:
                    direction = new Vector3Int(1,0);
                    startingCoord = new Vector3Int(startingRoom.coordinates.x + startingRoom.size.x - 1, startingRoom.coordinates.y + Random.Range(0, startingRoom.size.y));
                    break;
                //bottom
                case 1:
                    direction = new Vector3Int(-1,0);
                    startingCoord = new Vector3Int(startingRoom.coordinates.x, startingRoom.coordinates.y + Random.Range(0, startingRoom.size.y));
                    break;
                //left
                case 2:
                    direction = new Vector3Int(0,-1);
                    startingCoord = new Vector3Int(startingRoom.coordinates.x + Random.Range(0, startingRoom.size.x), startingRoom.coordinates.y);
                    break;
                //right
                case 3:
                    direction = new Vector3Int(0,1);
                    startingCoord = new Vector3Int(startingRoom.coordinates.x + Random.Range(0, startingRoom.size.x), startingRoom.coordinates.y + startingRoom.size.y - 1);
                    break;
            }

            GenerateDeadEnd(startingCoord, direction);
        }
    }

    void GenerateDeadEnd(Vector3Int startPosition, Vector3Int direction)
    {
        Tile targetTile = tiles[startPosition + direction];

        do
        {
            targetTile.SetTileType(TileType.Corridor);
            startPosition = targetTile.Coordinates;

            if (Random.Range(0f, 1f) < deadEndTurnChance)
            {
                int buffer = direction.x;
                direction.x = direction.y;
                direction.y = buffer;

                direction *= Random.Range(0f, 1f) < .5f ? 1 : -1;
            }

            targetTile = tiles[startPosition + direction];
        } while (targetTile.Type == TileType.Wall);
    }
    #endregion

    #region InterfaceMethod
    public Tile GetTileAtCoordinate(Vector3Int coords)
    {
        if (tiles.ContainsKey(coords))
            return tiles[coords];

        return null;
    }

    public Vector3 GetWorldPosFromCoord(Vector3Int targetTileCoord)
    {
        return grid.GetCellCenterWorld(targetTileCoord);
    }
    #endregion
}
