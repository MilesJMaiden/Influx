using System.Collections.Generic;
using UnityEngine;

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
    private readonly HashSet<Vector3> windowWallPositions = new();

    private readonly RoomConnections connections;

    private const int TileSize = 5;
    private const float FloorYPosition = -1.5f;
    private const float WallOffset = -2.5f;

    public RoomGenerator(
        int width,
        int height,
        ObjectPool floorPool,
        ObjectPool wallPool,
        ObjectPool windowWallPool,
        ObjectPool doorPool,
        ObjectPool wallDisplayPool,
        RoomConnections connections)
    {
        gridWidth = width;
        gridHeight = height;
        this.floorPool = floorPool;
        this.wallPool = wallPool;
        this.windowWallPool = windowWallPool;
        this.doorPool = doorPool;
        this.wallDisplayPool = wallDisplayPool;
        this.connections = connections;
        spawnedTiles = new Dictionary<Vector2Int, GameObject>();
    }

    public void GenerateRoom()
    {
        GenerateFloors();
        GeneratePerimeterWalls();
    }

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
                Vector2Int position = new(x, y);
                SpawnTile(floorPool, position, new Vector3(0, FloorYPosition, 0));
            }
        }
    }

    private void GeneratePerimeterWalls()
    {
        Vector2Int topDoorPos = new(gridWidth / 2, gridHeight);
        Vector2Int bottomDoorPos = new(gridWidth / 2, -1);
        Vector2Int leftDoorPos = new(-1, gridHeight / 2);
        Vector2Int rightDoorPos = new(gridWidth, gridHeight / 2);

        for (int x = 0; x <= gridWidth - 1; x++)
        {
            bool forceDoorTop = (x == topDoorPos.x) && connections.top;
            SpawnPerimeterWall(x, gridHeight, topDoorPos, Vector3.forward * WallOffset, forceDoorTop);
            bool forceDoorBottom = (x == bottomDoorPos.x) && connections.bottom;
            SpawnPerimeterWall(x, -1, bottomDoorPos, Vector3.back * WallOffset, forceDoorBottom);
        }

        for (int y = 0; y < gridHeight; y++)
        {
            bool forceDoorLeft = (y == leftDoorPos.y) && connections.left;
            SpawnPerimeterWall(-1, y, leftDoorPos, Vector3.left * WallOffset, forceDoorLeft, Quaternion.Euler(0, 90, 0));
            bool forceDoorRight = (y == rightDoorPos.y) && connections.right;
            SpawnPerimeterWall(gridWidth, y, rightDoorPos, Vector3.right * WallOffset, forceDoorRight, Quaternion.Euler(0, 90, 0));
        }
    }

    private void SpawnPerimeterWall(int x, int y, Vector2Int doorPos, Vector3 offset, bool forceDoor, Quaternion rotation = default)
    {
        Vector2Int wallPos = new(x, y);
        if (x == doorPos.x && y == doorPos.y)
        {
            if (forceDoor)
                return;
            else
                SpawnTile(wallPool, wallPos, offset, rotation);
        }
        else
        {
            bool useWindow = Random.value > 0.7f;
            ObjectPool selectedPool = useWindow ? windowWallPool : wallPool;
            GameObject wall = SpawnTile(selectedPool, wallPos, offset, rotation);
            if (wall != null && useWindow)
                windowWallPositions.Add(wall.transform.position);
        }
    }

    private GameObject SpawnTile(ObjectPool pool, Vector2Int gridPos, Vector3 offset, Quaternion rotation = default)
    {
        if (spawnedTiles.ContainsKey(gridPos)) return null;
        Vector3 localPosition = new Vector3(gridPos.x * TileSize, 0, gridPos.y * TileSize) + offset;
        GameObject obj = pool.Get();
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = rotation;
        spawnedTiles.Add(gridPos, obj);
        return obj;
    }
}
