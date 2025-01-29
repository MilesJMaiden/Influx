using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns objects in the room based on logical placement rules.
/// </summary>
public class RoomObjectSpawner
{
    private readonly ObjectPool containerPool;
    private readonly ObjectPool computerPool;
    private readonly ObjectPool wallDisplayPool;
    private readonly HashSet<Vector3> windowWallPositions;
    private readonly int gridWidth;
    private readonly int gridHeight;
    private readonly List<(Vector3 position, Quaternion rotation, bool isHorizontal)> wallPositions = new();
    private readonly List<Vector3> cornerPositions = new();
    private readonly List<Vector3> computerPositions = new();

    private const float ObjectHeight = 1f;
    private const float WallDisplayHeight = 2.5f;
    private const float WallDisplayOffset = -1f;
    private const float HorizontalWallDisplayZOffset = 2f;
    private const float VerticalWallDisplayXOffset = 1f;

    public RoomObjectSpawner(
        int width,
        int height,
        ObjectPool container,
        ObjectPool computer,
        ObjectPool wallDisplay,
        HashSet<Vector3> windowWalls)
    {
        gridWidth = width;
        gridHeight = height;
        containerPool = container;
        computerPool = computer;
        wallDisplayPool = wallDisplay;
        windowWallPositions = windowWalls; // Store window wall positions to avoid placing displays on them

        CachePossiblePositions();
    }

    /// <summary>
    /// Determines all suitable object placements.
    /// </summary>
    private void CachePossiblePositions()
    {
        for (int x = -1; x <= gridWidth; x++)
        {
            Vector3 topWallPos = new(x * 5, 0, gridHeight * 5 - 2.5f);
            Vector3 bottomWallPos = new(x * 5, 0, -2.5f);

            // Top wall: Facing inward (0°), Bottom wall: Facing inward (180°)
            wallPositions.Add((topWallPos, Quaternion.Euler(0, 0, 0), true));
            wallPositions.Add((bottomWallPos, Quaternion.Euler(0, 180, 0), true));
        }

        for (int y = 0; y < gridHeight; y++)
        {
            Vector3 leftWallPos = new(-2.5f, 0, y * 5);
            Vector3 rightWallPos = new(gridWidth * 5 - 2.5f, 0, y * 5);

            // Left wall: Facing right (90°), Right wall: Facing left (-90°)
            wallPositions.Add((leftWallPos, Quaternion.Euler(0, 90, 0), false));
            wallPositions.Add((rightWallPos, Quaternion.Euler(0, -90, 0), false));
        }

        // Find corner positions for containers
        cornerPositions.Add(new Vector3(-2.5f, 0, -2.5f));
        cornerPositions.Add(new Vector3(-2.5f, 0, (gridHeight - 1) * 5 - 2.5f));
        cornerPositions.Add(new Vector3((gridWidth - 1) * 5 - 2.5f, 0, -2.5f));
        cornerPositions.Add(new Vector3((gridWidth - 1) * 5 - 2.5f, 0, (gridHeight - 1) * 5 - 2.5f));

        // Find computer placements (against walls but not in corners)
        for (int x = 1; x < gridWidth - 1; x++)
        {
            computerPositions.Add(new Vector3(x * 5, 0, -2.5f + 1));
            computerPositions.Add(new Vector3(x * 5, 0, (gridHeight - 1) * 5 - 2.5f - 1));
        }
        for (int y = 1; y < gridHeight - 1; y++)
        {
            computerPositions.Add(new Vector3(-2.5f + 1, 0, y * 5));
            computerPositions.Add(new Vector3((gridWidth - 1) * 5 - 2.5f - 1, 0, y * 5));
        }
    }

    /// <summary>
    /// Spawns objects in the room with logical placement.
    /// </summary>
    public void SpawnObjects()
    {
        SpawnWallDisplays();
        SpawnContainers();
        SpawnComputers();
    }

    private void SpawnWallDisplays()
    {
        foreach (var (wallPos, wallRotation, isHorizontal) in wallPositions)
        {
            if (windowWallPositions.Contains(wallPos)) continue; // Skip window walls

            if (Random.value > 0.5f) // 50% chance to place a display
            {
                GameObject display = wallDisplayPool.Get();

                Vector3 adjustedPosition = isHorizontal
                    ? wallPos + new Vector3(0, WallDisplayHeight, HorizontalWallDisplayZOffset)
                    : wallPos + new Vector3(VerticalWallDisplayXOffset, WallDisplayHeight, 0);

                display.transform.position = adjustedPosition;
                display.transform.rotation = wallRotation;
            }
        }
    }

    private void SpawnContainers()
    {
        foreach (var cornerPos in cornerPositions)
        {
            if (Random.value > 0.7f)
            {
                GameObject container = containerPool.Get();
                container.transform.position = cornerPos;
            }
        }
    }

    private void SpawnComputers()
    {
        foreach (var compPos in computerPositions)
        {
            if (Random.value > 0.5f)
            {
                GameObject computer = computerPool.Get();
                computer.transform.position = compPos;
                computer.transform.rotation = GetComputerRotation(compPos);
            }
        }
    }

    /// <summary>
    /// Determines the correct rotation for a computer based on its wall proximity.
    /// Ensures computers always face into the room.
    /// </summary>
    private Quaternion GetComputerRotation(Vector3 position)
    {
        float x = position.x;
        float z = position.z;

        // Bottom wall (computers face inward)
        if (Mathf.Abs(z - (-2.5f + 1)) < 0.1f) return Quaternion.Euler(0, 0, 0);

        // Top wall (computers face inward)
        if (Mathf.Abs(z - ((gridHeight - 1) * 5 - 2.5f - 1)) < 0.1f) return Quaternion.Euler(0, 180, 0);

        // Left wall (computers face inward)
        if (Mathf.Abs(x - (-2.5f + 1)) < 0.1f) return Quaternion.Euler(0, 90, 0);

        // Right wall (computers face inward)
        if (Mathf.Abs(x - ((gridWidth - 1) * 5 - 2.5f - 1)) < 0.1f) return Quaternion.Euler(0, -90, 0);

        return Quaternion.identity;
    }
}
