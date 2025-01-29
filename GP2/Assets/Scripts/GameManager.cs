using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameManager is responsible for procedural generation of the play space.
/// Uses Object Pools for optimized tile placement.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Fields and Properties

    [Header("Prefab References")]
    [Tooltip("Prefab for the floor tile.")]
    public GameObject floorPrefab;

    [Tooltip("Prefab for the wall tile.")]
    public GameObject wallPrefab;

    [Tooltip("Prefab for the door tile.")]
    public GameObject doorPrefab;

    [Header("Grid Settings")]
    [Tooltip("Number of floor tiles along the X-axis.")]
    public int gridWidth = 5;

    [Tooltip("Number of floor tiles along the Y-axis.")]
    public int gridHeight = 5;

    private const int TileSize = 5; // Each tile is 5x5 units
    private Dictionary<Vector2Int, GameObject> spawnedTiles = new(); // Tracks placed objects

    // Object Pools
    private ObjectPool floorPool;
    private ObjectPool wallPool;
    private ObjectPool doorPool;

    // Parent Transforms for organizing hierarchy
    private Transform rootParent;
    private Transform floorParent;
    private Transform wallParent;
    private Transform doorParent;

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Initializes object pools and generates the play space.
    /// </summary>
    private void Start()
    {
        CreateHierarchy();
        InitializePools();
        GeneratePlaySpace();
    }

    #endregion

    #region Play Space Generation

    /// <summary>
    /// Creates a structured hierarchy for object pools.
    /// Ensures objects are properly grouped in the scene hierarchy.
    /// </summary>
    private void CreateHierarchy()
    {
        rootParent = new GameObject("ObjectPools").transform;
        floorParent = new GameObject("Floors").transform;
        wallParent = new GameObject("Walls").transform;
        doorParent = new GameObject("Doors").transform;

        floorParent.SetParent(rootParent);
        wallParent.SetParent(rootParent);
        doorParent.SetParent(rootParent);
    }

    /// <summary>
    /// Initializes object pools with structured parent organization.
    /// </summary>
    private void InitializePools()
    {
        floorPool = new ObjectPool(floorPrefab, floorParent, gridWidth * gridHeight);
        wallPool = new ObjectPool(wallPrefab, wallParent, (gridWidth * gridHeight) / 3);
        doorPool = new ObjectPool(doorPrefab, doorParent, (gridWidth * gridHeight) / 5);
    }

    /// <summary>
    /// Generates the procedural play space using object pooling.
    /// </summary>
    private void GeneratePlaySpace()
    {
        // Step 1: Place all floors
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int position = new Vector2Int(x, y);
                SpawnTile(floorPool, floorParent, position);
            }
        }

        // Step 2: Place walls and doors after floors
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int position = new Vector2Int(x, y);

                if (Random.value > 0.7f) // 30% chance to place a wall
                {
                    SpawnTile(wallPool, wallParent, position);
                }
                else if (Random.value > 0.9f) // 10% chance to place a door
                {
                    SpawnTile(doorPool, doorParent, position);
                }
            }
        }
    }

    /// <summary>
    /// Retrieves a tile from the pool and places it in the game world.
    /// Prevents duplicate placements.
    /// </summary>
    /// <param name="pool">The ObjectPool to retrieve the tile from.</param>
    /// <param name="parent">The parent transform to assign the object under.</param>
    /// <param name="gridPos">The grid position to place the tile at.</param>
    private void SpawnTile(ObjectPool pool, Transform parent, Vector2Int gridPos)
    {
        if (spawnedTiles.ContainsKey(gridPos)) return; // Prevent overlapping objects

        Vector3 worldPosition = new Vector3(gridPos.x * TileSize, 0, gridPos.y * TileSize);
        GameObject obj = pool.Get();
        obj.transform.SetParent(parent); // Ensure object remains structured
        obj.transform.position = worldPosition;
        spawnedTiles.Add(gridPos, obj);
    }

    #endregion
}
