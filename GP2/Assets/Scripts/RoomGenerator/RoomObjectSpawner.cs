using System.Collections.Generic;
using UnityEngine;

public class RoomObjectSpawner : IRoomObjectSpawner
{
    private const float TileSize = 5f;
    // How far inside the wall displays are offset from the wall.
    private const float displayInset = 0.5f;
    // Extra offsets based on rotation for wall displays.
    private const float offsetForNeg90 = 3f;   // For Y rotation -90 (or 270)
    private const float offsetFor180 = 3f;     // For Y rotation -180 (or 180)
    private const float offsetFor90 = 2f;      // For Y rotation 90
    private const float offsetFor0 = 2f;       // For Y rotation 0
    // Vertical offset to raise displays off the floor.
    private const float WallDisplayHeight = 2.5f;

    // Constant for interior margin (so objects stay inside the room)
    private const float spawnMargin = 1f;

    private readonly ObjectPool containerPool;
    private readonly ObjectPool computerPool;
    private readonly ObjectPool wallDisplayPool;
    private readonly HashSet<Vector3> windowWallPositions = new HashSet<Vector3>();

    private readonly int width;  // room width in tiles
    private readonly int height; // room height in tiles

    // Margin values to keep containers and computers away from room edges.
    private readonly float containerMargin;
    private readonly float computerMargin;

    // Candidate positions in local room space.
    private readonly List<(Vector3 position, Quaternion rotation, bool isHorizontal)> wallPositions = new();
    private readonly List<Vector3> cornerPositions = new();
    private readonly List<Vector3> computerPositions = new();

    // Flag indicating if this spawner is for a corridor.
    private readonly bool isCorridor;

    // The spawn settings that specify which prefabs (and quantities) to spawn in this room.
    private readonly RoomSpawnSettings spawnSettings;

    // Parent transform for spawned objects.
    private readonly Transform roomParent;

    public RoomObjectSpawner(
        int width,
        int height,
        RoomSpawnSettings spawnSettings,
        ObjectPool containerPool,
        ObjectPool computerPool,
        ObjectPool wallDisplayPool,
        Transform roomParent,
        bool isCorridor = false,
        float containerMargin = 2f,
        float computerMargin = 2f)
    {
        this.width = width;
        this.height = height;
        this.spawnSettings = spawnSettings;
        this.containerPool = containerPool;
        this.computerPool = computerPool;
        this.wallDisplayPool = wallDisplayPool;
        this.roomParent = roomParent;
        this.isCorridor = isCorridor;
        this.containerMargin = containerMargin;
        this.computerMargin = computerMargin;
        CachePossiblePositions();
    }

    /// <summary>
    /// Computes candidate positions for wall displays, containers, and computers.
    /// Room local coordinates span from (0,0) (bottom-left) to (width*TileSize, height*TileSize) (top-right).
    /// Container and computer positions are inset by their margin values.
    /// </summary>
    private void CachePossiblePositions()
    {
        wallPositions.Clear();
        cornerPositions.Clear();
        computerPositions.Clear();

        float roomWidthWorld = width * TileSize;
        float roomHeightWorld = height * TileSize;

        // Candidate positions for wall displays.
        for (int i = 0; i < width; i++)
        {
            Vector3 topWallPos = new Vector3((i + 0.5f) * TileSize, 0, roomHeightWorld - displayInset);
            Vector3 bottomWallPos = new Vector3((i + 0.5f) * TileSize, 0, displayInset);
            wallPositions.Add((topWallPos, Quaternion.Euler(0, 0, 0), true));
            wallPositions.Add((bottomWallPos, Quaternion.Euler(0, 180, 0), true));
        }
        for (int j = 0; j < height; j++)
        {
            Vector3 leftWallPos = new Vector3(displayInset, 0, (j + 0.5f) * TileSize);
            Vector3 rightWallPos = new Vector3(roomWidthWorld - displayInset, 0, (j + 0.5f) * TileSize);
            wallPositions.Add((leftWallPos, Quaternion.Euler(0, 90, 0), false));
            wallPositions.Add((rightWallPos, Quaternion.Euler(0, -90, 0), false));
        }

        // Candidate positions for containers (corners), inset by containerMargin.
        cornerPositions.Add(new Vector3(containerMargin, 0, containerMargin));                              // bottom-left
        cornerPositions.Add(new Vector3(containerMargin, 0, roomHeightWorld - containerMargin));              // top-left
        cornerPositions.Add(new Vector3(roomWidthWorld - containerMargin, 0, containerMargin));               // bottom-right
        cornerPositions.Add(new Vector3(roomWidthWorld - containerMargin, 0, roomHeightWorld - containerMargin)); // top-right

        // Candidate positions for computers (along walls), inset by computerMargin.
        computerPositions.Add(new Vector3(roomWidthWorld / 2f, 0, computerMargin));                                 // bottom wall
        computerPositions.Add(new Vector3(roomWidthWorld / 2f, 0, roomHeightWorld - computerMargin));                   // top wall
        computerPositions.Add(new Vector3(computerMargin, 0, roomHeightWorld / 2f));                                 // left wall
        computerPositions.Add(new Vector3(roomWidthWorld - computerMargin, 0, roomHeightWorld / 2f));                  // right wall
    }

    public void SpawnObjects()
    {
        // Do not spawn objects in corridors.
        if (isCorridor)
            return;

        // If no spawn settings or no entries defined, nothing to spawn.
        if (spawnSettings == null || spawnSettings.spawnEntries == null || spawnSettings.spawnEntries.Length == 0)
            return;

        // Iterate over each spawn entry.
        foreach (SpawnEntry entry in spawnSettings.spawnEntries)
        {
            // Spawn the specified number of instances.
            for (int i = 0; i < entry.count; i++)
            {
                Vector3 spawnPos = GetRandomInteriorPosition();
                Quaternion spawnRot = Quaternion.identity;

                // Check for specific tags and use candidate positions if available.
                if (entry.prefab.CompareTag("WallDisplay") && wallPositions.Count > 0)
                {
                    var candidate = wallPositions[Random.Range(0, wallPositions.Count)];
                    spawnPos = candidate.position + new Vector3(0, WallDisplayHeight, 0);
                    float yRot = candidate.rotation.eulerAngles.y;
                    if (Mathf.Approximately(yRot, 270f) || Mathf.Approximately(yRot, -90f))
                        spawnPos.x -= offsetForNeg90;
                    else if (Mathf.Approximately(yRot, 180f))
                        spawnPos.z -= offsetFor180;
                    else if (Mathf.Approximately(yRot, 90f))
                        spawnPos.x -= offsetFor90;
                    else if (Mathf.Approximately(yRot, 0f))
                        spawnPos.z -= offsetFor0;
                    spawnRot = candidate.rotation;
                }
                else if (entry.prefab.CompareTag("Container") && cornerPositions.Count > 0)
                {
                    spawnPos = cornerPositions[Random.Range(0, cornerPositions.Count)];
                }
                else if (entry.prefab.CompareTag("Computer") && computerPositions.Count > 0)
                {
                    spawnPos = computerPositions[Random.Range(0, computerPositions.Count)];
                }

                Object.Instantiate(entry.prefab, roomParent.TransformPoint(spawnPos), spawnRot, roomParent);
            }
        }
    }

    /// <summary>
    /// Returns a random interior position within the room, keeping a margin from the edges.
    /// Assumes the room's bottom-left is at (0,0).
    /// </summary>
    private Vector3 GetRandomInteriorPosition()
    {
        float roomWidthWorld = width * TileSize;
        float roomHeightWorld = height * TileSize;
        float x = Random.Range(spawnMargin, roomWidthWorld - spawnMargin);
        float z = Random.Range(spawnMargin, roomHeightWorld - spawnMargin);
        return new Vector3(x, 0, z);
    }
}
