using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsible for generating the room structure (Floors, Walls, Doors, Window Walls).
/// </summary>
public class RoomGenerator : IRoomGenerator
{
    private readonly int gridWidth;
    private readonly int gridHeight;
    private readonly ObjectPool floorPool;
    private readonly ObjectPool wallPool;
    private readonly ObjectPool windowWallPool;
    private readonly ObjectPool doorPool;
    private readonly ObjectPool wallDisplayPool;
    private readonly Dictionary<Vector2Int, GameObject> spawnedTiles;
    private readonly HashSet<Vector3> windowWallPositions = new(); // Stores window wall positions

    private const int TileSize = 5;
    private const float FloorYPosition = -1.5f;
    private const float WallOffset = -2.5f;

    public RoomGenerator(
        int width,
        int height,
        ObjectPool floor,
        ObjectPool wall,
        ObjectPool windowWall,
        ObjectPool door,
        ObjectPool wallDisplay)
    {
        gridWidth = width;
        gridHeight = height;
        floorPool = floor;
        wallPool = wall;
        windowWallPool = windowWall;
        doorPool = door;
        wallDisplayPool = wallDisplay;
        spawnedTiles = new Dictionary<Vector2Int, GameObject>();
    }

    /// <summary>
    /// Generates the room structure.
    /// </summary>
    public void GenerateRoom()
    {
        GenerateFloors();
        GeneratePerimeterWalls();
    }

    /// <summary>
    /// Returns the set of window wall positions.
    /// </summary>
    public HashSet<Vector3> GetWindowWallPositions()
    {
        return windowWallPositions;
    }

    private void GenerateFloors()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int position = new Vector2Int(x, y);
                SpawnTile(floorPool, position, new Vector3(0, FloorYPosition, 0));
            }
        }
    }

    private void GeneratePerimeterWalls()
    {
        Vector2Int topDoorPos = new Vector2Int(gridWidth / 2, gridHeight);
        Vector2Int bottomDoorPos = new Vector2Int(gridWidth / 2, -1);
        Vector2Int leftDoorPos = new Vector2Int(-1, gridHeight / 2);
        Vector2Int rightDoorPos = new Vector2Int(gridWidth, gridHeight / 2);

        for (int x = -1; x <= gridWidth; x++)
        {
            bool useWindow = Random.value > 0.7f;
            SpawnPerimeterWall(x, gridHeight, topDoorPos, Vector3.forward * WallOffset, useWindow);
            SpawnPerimeterWall(x, -1, bottomDoorPos, Vector3.back * WallOffset, useWindow);
        }

        for (int y = 0; y < gridHeight; y++)
        {
            bool useWindow = Random.value > 0.7f;
            SpawnPerimeterWall(-1, y, leftDoorPos, Vector3.left * WallOffset, useWindow, Quaternion.Euler(0, 90, 0));
            SpawnPerimeterWall(gridWidth, y, rightDoorPos, Vector3.right * WallOffset, useWindow, Quaternion.Euler(0, 90, 0));
        }
    }

    private void SpawnPerimeterWall(int x, int y, Vector2Int doorPos, Vector3 offset, bool useWindow, Quaternion rotation = default)
    {
        Vector2Int wallPos = new Vector2Int(x, y);
        if (x == doorPos.x && y == doorPos.y)
        {
            SpawnTile(doorPool, wallPos, offset, rotation);
        }
        else
        {
            ObjectPool selectedPool = useWindow ? windowWallPool : wallPool;
            GameObject wall = SpawnTile(selectedPool, wallPos, offset, rotation);
            if (wall != null && useWindow)
            {
                windowWallPositions.Add(wall.transform.position); // Store window wall position
            }
        }
    }

    private GameObject SpawnTile(ObjectPool pool, Vector2Int gridPos, Vector3 offset, Quaternion rotation = default)
    {
        if (spawnedTiles.ContainsKey(gridPos)) return null;

        Vector3 worldPosition = new Vector3(gridPos.x * TileSize, 0, gridPos.y * TileSize) + offset;
        GameObject obj = pool.Get();
        obj.transform.position = worldPosition;
        obj.transform.rotation = rotation;
        spawnedTiles.Add(gridPos, obj);
        return obj;
    }
}
