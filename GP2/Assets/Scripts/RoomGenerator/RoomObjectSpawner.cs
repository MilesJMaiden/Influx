using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;

public class RoomObjectSpawner : IRoomObjectSpawner
{
    private const float TileSize = 5f;
    // How far inside the room wall displays should be offset from the wall (in world units)
    private const float displayInset = 0.5f;
    // Extra offsets based on rotation.
    private const float offsetForNeg90 = 3f;   // For Y rotation -90 (or 270)
    private const float offsetFor180 = 3f;     // For Y rotation -180 (or 180)
    private const float offsetFor90 = 2f;      // For Y rotation 90
    private const float offsetFor0 = 2f;       // For Y rotation 0
    // Vertical offset to raise displays off the floor.
    private const float WallDisplayHeight = 2.5f;

    private readonly ObjectPool containerPool;
    private readonly ObjectPool computerPool;
    private readonly ObjectPool wallDisplayPool;
    // We'll no longer pass windowWallPositions; initialize empty.
    private readonly HashSet<Vector3> windowWallPositions = new HashSet<Vector3>();

    private readonly int width;  // room width in tiles
    private readonly int height; // room height in tiles

    // Candidate positions in local room space.
    private readonly List<(Vector3 position, Quaternion rotation, bool isHorizontal)> wallPositions = new();
    private readonly List<Vector3> cornerPositions = new();
    private readonly List<Vector3> computerPositions = new();

    public RoomObjectSpawner(
        int width,
        int height,
        ObjectPool containerPool,
        ObjectPool computerPool,
        ObjectPool wallDisplayPool)
    {
        this.width = width;
        this.height = height;
        this.containerPool = containerPool;
        this.computerPool = computerPool;
        this.wallDisplayPool = wallDisplayPool;
        CachePossiblePositions();
    }

    /// <summary>
    /// Computes candidate positions for wall displays, containers, and computers based on the room's dimensions.
    /// Room local coordinates span from (0,0) (bottom-left) to (width*TileSize, height*TileSize) (top-right).
    /// Positions are inset by displayInset.
    /// </summary>
    private void CachePossiblePositions()
    {
        wallPositions.Clear();
        cornerPositions.Clear();
        computerPositions.Clear();

        float roomWidthWorld = width * TileSize;
        float roomHeightWorld = height * TileSize;

        // Top wall: candidate positions along the top edge, inset inward.
        for (int i = 0; i < width; i++)
        {
            Vector3 topWallPos = new Vector3((i + 0.5f) * TileSize, 0, roomHeightWorld - displayInset);
            Vector3 bottomWallPos = new Vector3((i + 0.5f) * TileSize, 0, displayInset);
            // For top wall, we set rotation to 0 (we’ll later apply extra offset for Y==0)
            wallPositions.Add((topWallPos, Quaternion.Euler(0, 0, 0), true));
            // For bottom wall, use rotation 180.
            wallPositions.Add((bottomWallPos, Quaternion.Euler(0, 180, 0), true));
        }

        // Left and right walls.
        for (int j = 0; j < height; j++)
        {
            Vector3 leftWallPos = new Vector3(displayInset, 0, (j + 0.5f) * TileSize);
            Vector3 rightWallPos = new Vector3(roomWidthWorld - displayInset, 0, (j + 0.5f) * TileSize);
            // For left wall, rotation 90.
            wallPositions.Add((leftWallPos, Quaternion.Euler(0, 90, 0), false));
            // For right wall, rotation -90 (or 270).
            wallPositions.Add((rightWallPos, Quaternion.Euler(0, -90, 0), false));
        }

        // Corners.
        cornerPositions.Add(new Vector3(displayInset, 0, displayInset));                              // bottom-left
        cornerPositions.Add(new Vector3(displayInset, 0, roomHeightWorld - displayInset));              // top-left
        cornerPositions.Add(new Vector3(roomWidthWorld - displayInset, 0, displayInset));               // bottom-right
        cornerPositions.Add(new Vector3(roomWidthWorld - displayInset, 0, roomHeightWorld - displayInset)); // top-right

        // Computers: one per wall (offset inward by 1 unit).
        computerPositions.Add(new Vector3(roomWidthWorld / 2f, 0, 1));                                 // bottom wall
        computerPositions.Add(new Vector3(roomWidthWorld / 2f, 0, roomHeightWorld - 1));                   // top wall
        computerPositions.Add(new Vector3(1, 0, roomHeightWorld / 2f));                                 // left wall
        computerPositions.Add(new Vector3(roomWidthWorld - 1, 0, roomHeightWorld / 2f));                  // right wall
    }

    /// <summary>
    /// Spawns all objects in the room using the cached candidate positions.
    /// </summary>
    public void SpawnObjects()
    {
        SpawnWallDisplays();
        SpawnContainers();
        SpawnComputers();
    }

    /// <summary>
    /// Spawns wall displays at candidate positions.
    /// Applies extra positional offsets based on the display’s Y rotation.
    /// </summary>
    private void SpawnWallDisplays()
    {
        foreach (var (pos, rotation, isHorizontal) in wallPositions)
        {
            if (Random.value < 0.5f)
                continue;
            GameObject display = wallDisplayPool.Get();
            // Start with a vertical offset so the display is raised.
            Vector3 adjustedPos = pos + new Vector3(0, WallDisplayHeight, 0);

            // Apply extra offsets based on the Y rotation.
            float yRot = rotation.eulerAngles.y;
            // Using Mathf.Approximately to compare floating-point values.
            if (Mathf.Approximately(yRot, 270f) || Mathf.Approximately(yRot, -90f))
            {
                // For displays with Y rotation -90, lower X by 3.
                adjustedPos.x -= 3f;
            }
            else if (Mathf.Approximately(yRot, 180f))
            {
                // For displays with Y rotation -180 (or 180), lower Z by 3.
                adjustedPos.z -= 3f;
            }
            else if (Mathf.Approximately(yRot, 90f))
            {
                // For displays with Y rotation 90, lower X by 2.
                adjustedPos.x -= 2f;
            }
            else if (Mathf.Approximately(yRot, 0f))
            {
                // For displays with Y rotation 0, lower Z by 2.
                adjustedPos.z -= 2f;
            }

            display.transform.localPosition = adjustedPos;
            display.transform.localRotation = rotation;
        }
    }

    /// <summary>
    /// Spawns containers at the room's corner positions.
    /// </summary>
    private void SpawnContainers()
    {
        foreach (Vector3 pos in cornerPositions)
        {
            if (Random.value < 0.3f)
            {
                GameObject container = containerPool.Get();
                container.transform.localPosition = pos;
            }
        }
    }

    /// <summary>
    /// Spawns computers at candidate positions along the walls.
    /// </summary>
    private void SpawnComputers()
    {
        foreach (Vector3 pos in computerPositions)
        {
            if (Random.value < 0.5f)
            {
                GameObject computer = computerPool.Get();
                computer.transform.localPosition = pos;
                computer.transform.localRotation = GetComputerRotation(pos);
            }
        }
    }

    /// <summary>
    /// Determines the rotation for a computer so that it faces inward based on its local position.
    /// </summary>
    private Quaternion GetComputerRotation(Vector3 pos)
    {
        float roomWidthWorld = width * TileSize;
        float roomHeightWorld = height * TileSize;
        float bottomDist = pos.z;
        float topDist = roomHeightWorld - pos.z;
        float leftDist = pos.x;
        float rightDist = roomWidthWorld - pos.x;
        float minDist = Mathf.Min(bottomDist, topDist, leftDist, rightDist);
        if (minDist == bottomDist)
            return Quaternion.Euler(0, 0, 0);
        if (minDist == topDist)
            return Quaternion.Euler(0, 180, 0);
        if (minDist == leftDist)
            return Quaternion.Euler(0, 90, 0);
        if (minDist == rightDist)
            return Quaternion.Euler(0, -90, 0);
        return Quaternion.identity;
    }
}
