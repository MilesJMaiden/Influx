using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#region SafeArea Helper
/// <summary>
/// Represents a 2D safe area (in room-local space) defined by a minimum and maximum point.
/// </summary>
public struct SafeArea
{
    public Vector2 min;
    public Vector2 max;

    public SafeArea(Vector2 min, Vector2 max)
    {
        this.min = min;
        this.max = max;
    }
}
#endregion

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

    // Constant for interior margin (used for Container and Computer spawns)
    private const float spawnMargin = 1f;
    // Margin for bigger objects (applied to all other objects)
    private const float bigObjectMargin = 3f;
    // New: Extra spacing between big objects
    private const float bigObjectSpacing = 5f;
    // New: Extra spacing for Computer objects so they do not overlap with any other objects.
    private const float computerSpacing = 3f;
    // Default safe offset to keep spawns away from walls.
    private const float defaultSafeOffset = 2.5f;
    // Wall margin: objects (except WallDisplay) must be at least this far from any wall.
    private const float wallMargin = 4f;

    private readonly ObjectPool containerPool;
    private readonly ObjectPool computerPool;
    private readonly ObjectPool wallDisplayPool;
    private readonly HashSet<Vector3> windowWallPositions = new();

    private readonly int width;  // room width in tiles
    private readonly int height; // room height in tiles

    // Margin values to keep containers and computers away from room edges.
    private readonly float containerMargin;
    private readonly float computerMargin;

    // Candidate positions (in room-local coordinates)
    private readonly List<(Vector3 position, Quaternion rotation, bool isHorizontal)> wallPositions = new();
    private readonly List<Vector3> cornerPositions = new();
    private readonly List<Vector3> computerPositions = new();

    // List to track positions of spawned big objects (generic objects)
    private readonly List<Vector3> placedBigObjectPositions = new();
    // New: List to track positions of spawned Computer objects.
    private readonly List<Vector3> placedComputerPositions = new();

    // Flag indicating if this spawner is for a corridor.
    private readonly bool isCorridor;

    // The spawn settings that specify which prefabs (and quantities) to spawn.
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
    /// Computes the safe area rectangle for a given margin.
    /// The safe area is the room's bounds inset by (margin + defaultSafeOffset + wallMargin) on every side.
    /// </summary>
    private SafeArea ComputeSafeArea(float margin)
    {
        float roomWidthWorld = width * TileSize;
        float roomHeightWorld = height * TileSize;
        float totalOffset = margin + defaultSafeOffset + wallMargin;
        Vector2 min = new Vector2(totalOffset, totalOffset);
        Vector2 max = new Vector2(roomWidthWorld - totalOffset, roomHeightWorld - totalOffset);
        return new SafeArea(min, max);
    }

    /// <summary>
    /// Caches candidate positions for wall displays, containers, and computers.
    /// Room local coordinates span from (0,0) (bottom-left) to (width*TileSize, height*TileSize) (top-right).
    /// </summary>
    private void CachePossiblePositions()
    {
        wallPositions.Clear();
        cornerPositions.Clear();
        computerPositions.Clear();

        float roomWidthWorld = width * TileSize;
        float roomHeightWorld = height * TileSize;

        // Wall displays remain along the perimeter.
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

        // For Container spawns, use the safe area computed from containerMargin.
        SafeArea containerSafeArea = ComputeSafeArea(containerMargin);
        cornerPositions.Add(new Vector3(containerSafeArea.min.x, 0, containerSafeArea.min.y));
        cornerPositions.Add(new Vector3(containerSafeArea.min.x, 0, containerSafeArea.max.y));
        cornerPositions.Add(new Vector3(containerSafeArea.max.x, 0, containerSafeArea.min.y));
        cornerPositions.Add(new Vector3(containerSafeArea.max.x, 0, containerSafeArea.max.y));

        // For Computer spawns, use the safe area computed from computerMargin.
        SafeArea computerSafeArea = ComputeSafeArea(computerMargin);
        float midX = (computerSafeArea.min.x + computerSafeArea.max.x) / 2f;
        float midZ = (computerSafeArea.min.y + computerSafeArea.max.y) / 2f;
        computerPositions.Add(new Vector3(midX, 0, computerSafeArea.min.y));
        computerPositions.Add(new Vector3(midX, 0, computerSafeArea.max.y));
        computerPositions.Add(new Vector3(computerSafeArea.min.x, 0, midZ));
        computerPositions.Add(new Vector3(computerSafeArea.max.x, 0, midZ));
    }

    /// <summary>
    /// Determines if a prefab should be considered a big object (that needs extra spacing).
    /// Here, we treat any prefab that is not a Container, Computer, WallDisplay, or WindowWall as a big object.
    /// </summary>
    private bool IsBigObject(GameObject prefab)
    {
        return !prefab.CompareTag("Container") &&
               !prefab.CompareTag("Computer") &&
               !prefab.CompareTag("WallDisplay") &&
               !prefab.CompareTag("WindowWall");
    }

    /// <summary>
    /// Spawns objects based on the spawn settings.
    /// Container, Computer, WallDisplay, and WindowWall spawns are handled with their specialized safe areas.
    /// All other objects are spawned using the larger safe area defined by bigObjectMargin.
    /// Additionally, extra spacing checks are applied for big objects and for Computer objects.
    /// </summary>
    public void SpawnObjects()
    {
        if (isCorridor)
            return;
        if (spawnSettings == null || spawnSettings.spawnEntries == null || spawnSettings.spawnEntries.Length == 0)
            return;

        int maxAttempts = 5;

        foreach (SpawnEntry entry in spawnSettings.spawnEntries)
        {
            // Skip Agent and Wall spawn entries.
            if (entry.prefab != null &&
                (entry.prefab.CompareTag("Agent") || entry.prefab.CompareTag("Wall")))
            {
                continue;
            }

            for (int i = 0; i < entry.count; i++)
            {
                Vector3 spawnPos = Vector3.zero;
                Quaternion spawnRot = Quaternion.identity;
                bool spawnSuccess = false;
                int attempts = 0;

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
                    spawnSuccess = true;
                }
                else if (entry.prefab.CompareTag("Container"))
                {
                    if (!TryGetValidPosition(cornerPositions, entry.prefab, out spawnPos))
                    {
                        while (attempts < maxAttempts)
                        {
                            spawnPos = GetRandomPositionWithinSafeArea(containerMargin);
                            if (ValidateSpawnCandidate(spawnPos, Quaternion.identity, entry.prefab))
                            {
                                spawnSuccess = true;
                                break;
                            }
                            attempts++;
                        }
                    }
                    else
                    {
                        spawnSuccess = ValidateSpawnCandidate(spawnPos, Quaternion.identity, entry.prefab);
                    }
                }
                else if (entry.prefab.CompareTag("Computer"))
                {
                    if (!TryGetValidPosition(computerPositions, entry.prefab, out spawnPos))
                    {
                        while (attempts < maxAttempts)
                        {
                            spawnPos = GetRandomPositionWithinSafeArea(computerMargin);
                            if (ValidateSpawnCandidate(spawnPos, Quaternion.identity, entry.prefab))
                            {
                                spawnSuccess = true;
                                break;
                            }
                            attempts++;
                        }
                    }
                    else
                    {
                        spawnSuccess = ValidateSpawnCandidate(spawnPos, Quaternion.identity, entry.prefab);
                    }
                }
                else if (entry.prefab.CompareTag("WindowWall"))
                {
                    // For WindowWall spawns, restrict candidate positions to the center of each wall.
                    float roomWidthWorld = width * TileSize;
                    float roomHeightWorld = height * TileSize;
                    List<Vector3> windowWallCandidates = new List<Vector3>()
                    {
                        new Vector3(roomWidthWorld / 2f, 0, 0),                     // Bottom wall center
                        new Vector3(roomWidthWorld / 2f, 0, roomHeightWorld),         // Top wall center
                        new Vector3(0, 0, roomHeightWorld / 2f),                      // Left wall center
                        new Vector3(roomWidthWorld, 0, roomHeightWorld / 2f)          // Right wall center
                    };
                    spawnPos = windowWallCandidates[Random.Range(0, windowWallCandidates.Count)];
                    spawnSuccess = ValidateSpawnCandidate(spawnPos, Quaternion.identity, entry.prefab);
                }
                // All other objects use the larger safe area (bigObjectMargin)
                else
                {
                    while (!spawnSuccess && attempts < maxAttempts)
                    {
                        spawnPos = GetRandomPositionWithinSafeArea(bigObjectMargin);
                        if (ValidateSpawnCandidate(spawnPos, Quaternion.identity, entry.prefab))
                        {
                            spawnSuccess = true;
                            break;
                        }
                        attempts++;
                    }
                }

                if (spawnSuccess)
                {
                    Object.Instantiate(entry.prefab, roomParent.TransformPoint(spawnPos), spawnRot, roomParent);

                    // Record positions for extra spacing checks.
                    if (IsBigObject(entry.prefab))
                    {
                        placedBigObjectPositions.Add(spawnPos);
                    }
                    else if (entry.prefab.CompareTag("Computer"))
                    {
                        placedComputerPositions.Add(spawnPos);
                    }
                }
                else
                {
                    Debug.LogWarning($"Failed to spawn {entry.prefab.name} in a non-overlapping position after {maxAttempts} attempts.");
                }
            }
        }
    }

    /// <summary>
    /// Returns a random position within the safe area computed from the given margin.
    /// </summary>
    private Vector3 GetRandomPositionWithinSafeArea(float margin)
    {
        SafeArea safeArea = ComputeSafeArea(margin);
        float x = Random.Range(safeArea.min.x, safeArea.max.x);
        float z = Random.Range(safeArea.min.y, safeArea.max.y);
        return new Vector3(x, 0, z);
    }

    /// <summary>
    /// Attempts to pick a candidate position from a list that passes validation.
    /// </summary>
    private bool TryGetValidPosition(List<Vector3> candidates, GameObject prefab, out Vector3 validPos)
    {
        List<Vector3> shuffledCandidates = new List<Vector3>(candidates);
        int count = shuffledCandidates.Count;
        while (count > 1)
        {
            count--;
            int k = Random.Range(0, count + 1);
            Vector3 temp = shuffledCandidates[k];
            shuffledCandidates[k] = shuffledCandidates[count];
            shuffledCandidates[count] = temp;
        }
        foreach (var candidate in shuffledCandidates)
        {
            if (ValidateSpawnCandidate(candidate, Quaternion.identity, prefab))
            {
                validPos = candidate;
                return true;
            }
        }
        validPos = Vector3.zero;
        return false;
    }

    /// <summary>
    /// Validates a candidate spawn position using the prefab's Collider.
    /// Converts the candidate (in local space) to world space, then uses Physics.OverlapBox to detect overlapping colliders.
    /// Also:
    /// - For non-WallDisplay objects, an OverlapSphere ensures the candidate is at least 'wallMargin' units from any collider tagged "Wall".
    /// - For WallDisplay objects, an OverlapSphere ensures no collider tagged "WindowWall" overlaps.
    /// - For WindowWall objects, the candidate must be at the center of a wall.
    /// - For big objects, extra spacing from already spawned big objects is enforced.
    /// - For Computer objects, extra spacing is enforced from both previously spawned Computers and big objects.
    /// </summary>
    private bool ValidateSpawnCandidate(Vector3 candidate, Quaternion rotation, GameObject prefab)
    {
        Vector3 worldPos = roomParent.TransformPoint(candidate);
        Collider prefabCollider = prefab.GetComponent<Collider>();
        if (prefabCollider == null)
        {
            return true;
        }

        Vector3 halfExtents;
        if (prefabCollider is BoxCollider box)
        {
            halfExtents = box.size * 0.5f;
        }
        else
        {
            halfExtents = prefabCollider.bounds.extents;
        }

        Collider[] overlaps = Physics.OverlapBox(worldPos, halfExtents, rotation);
        if (overlaps.Length > 0)
            return false;

        // For non-WallDisplay objects, enforce a wall margin.
        if (!prefab.CompareTag("WallDisplay"))
        {
            Collider[] wallCheck = Physics.OverlapSphere(worldPos, wallMargin);
            foreach (Collider col in wallCheck)
            {
                if (col.gameObject.CompareTag("Wall"))
                    return false;
            }
        }

        // For WallDisplay objects, ensure no WindowWall overlaps.
        if (prefab.CompareTag("WallDisplay"))
        {
            Collider[] windowCheck = Physics.OverlapSphere(worldPos, wallMargin);
            foreach (Collider col in windowCheck)
            {
                if (col.gameObject.CompareTag("WindowWall"))
                    return false;
            }
        }

        // For WindowWall objects, check that the candidate is at the center of a wall.
        if (prefab.CompareTag("WindowWall"))
        {
            float roomWidthWorld = width * TileSize;
            float roomHeightWorld = height * TileSize;
            float tolerance = 0.5f; // adjust as needed

            bool isBottomCenter = Mathf.Abs(candidate.x - roomWidthWorld / 2f) <= tolerance &&
                                  Mathf.Abs(candidate.z - 0f) <= tolerance;
            bool isTopCenter = Mathf.Abs(candidate.x - roomWidthWorld / 2f) <= tolerance &&
                               Mathf.Abs(candidate.z - roomHeightWorld) <= tolerance;
            bool isLeftCenter = Mathf.Abs(candidate.x - 0f) <= tolerance &&
                                Mathf.Abs(candidate.z - roomHeightWorld / 2f) <= tolerance;
            bool isRightCenter = Mathf.Abs(candidate.x - roomWidthWorld) <= tolerance &&
                                 Mathf.Abs(candidate.z - roomHeightWorld / 2f) <= tolerance;
            if (!(isBottomCenter || isTopCenter || isLeftCenter || isRightCenter))
            {
                return false;
            }
        }

        // For big objects, enforce extra spacing from already spawned big objects.
        if (IsBigObject(prefab))
        {
            foreach (Vector3 placedPos in placedBigObjectPositions)
            {
                if (Vector3.Distance(candidate, placedPos) < bigObjectSpacing)
                {
                    return false;
                }
            }
        }

        // Extra spacing check for Computer objects.
        if (prefab.CompareTag("Computer"))
        {
            foreach (Vector3 placedPos in placedComputerPositions)
            {
                if (Vector3.Distance(candidate, placedPos) < computerSpacing)
                {
                    return false;
                }
            }
            // Optionally, also check spacing against big objects.
            foreach (Vector3 placedPos in placedBigObjectPositions)
            {
                if (Vector3.Distance(candidate, placedPos) < computerSpacing)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
