using UnityEngine;
using System.Collections.Generic;

#region SafeArea Helper
/// <summary>
/// A simple structure representing a 2D safe area (in local room space)
/// defined by a minimum and maximum point.
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

    // Constant for interior margin (for generic objects spawning inside the room area)
    private const float spawnMargin = 1f;

    // ***** NEW: Default safe offset to keep spawns away from walls *****
    private const float defaultSafeOffset = 2.5f;

    private readonly ObjectPool containerPool;
    private readonly ObjectPool computerPool;
    private readonly ObjectPool wallDisplayPool;
    private readonly HashSet<Vector3> windowWallPositions = new HashSet<Vector3>();

    private readonly int width;  // room width in tiles
    private readonly int height; // room height in tiles

    // Margin values to keep containers and computers away from room edges.
    private readonly float containerMargin;
    private readonly float computerMargin;

    // Candidate positions (in room-local coordinates)
    private readonly List<(Vector3 position, Quaternion rotation, bool isHorizontal)> wallPositions = new();
    private readonly List<Vector3> cornerPositions = new();
    private readonly List<Vector3> computerPositions = new();

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
    /// The safe area is the room’s bounds inset by (margin + defaultSafeOffset) on every side.
    /// </summary>
    private SafeArea ComputeSafeArea(float margin)
    {
        float roomWidthWorld = width * TileSize;
        float roomHeightWorld = height * TileSize;
        float totalOffset = margin + defaultSafeOffset;
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

        // For container candidates, use the safe area computed from containerMargin.
        SafeArea containerSafeArea = ComputeSafeArea(containerMargin);
        cornerPositions.Add(new Vector3(containerSafeArea.min.x, 0, containerSafeArea.min.y));   // bottom-left
        cornerPositions.Add(new Vector3(containerSafeArea.min.x, 0, containerSafeArea.max.y));   // top-left
        cornerPositions.Add(new Vector3(containerSafeArea.max.x, 0, containerSafeArea.min.y));   // bottom-right
        cornerPositions.Add(new Vector3(containerSafeArea.max.x, 0, containerSafeArea.max.y));   // top-right

        // For computer candidates, use the safe area computed from computerMargin.
        SafeArea computerSafeArea = ComputeSafeArea(computerMargin);
        float midX = (computerSafeArea.min.x + computerSafeArea.max.x) / 2f;
        float midZ = (computerSafeArea.min.y + computerSafeArea.max.y) / 2f;
        computerPositions.Add(new Vector3(midX, 0, computerSafeArea.min.y));   // bottom safe edge
        computerPositions.Add(new Vector3(midX, 0, computerSafeArea.max.y));   // top safe edge
        computerPositions.Add(new Vector3(computerSafeArea.min.x, 0, midZ));     // left safe edge
        computerPositions.Add(new Vector3(computerSafeArea.max.x, 0, midZ));     // right safe edge
    }

    /// Spawns objects based on the spawn settings.
    /// For Container and Computer prefabs, candidate positions are sampled from the safe area and explicitly validated.
    /// (Agent spawn entries are skipped here.)
    /// </summary>
    public void SpawnObjects()
    {
        if (isCorridor)
            return;
        if (spawnSettings == null || spawnSettings.spawnEntries == null || spawnSettings.spawnEntries.Length == 0)
            return;

        int maxAttempts = 5; // Maximum attempts to find a valid spawn position.

        foreach (SpawnEntry entry in spawnSettings.spawnEntries)
        {
            // ***** SKIP AGENT ENTRIES HERE *****
            if (entry.prefab != null && entry.prefab.CompareTag("Agent"))
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
                    // For wall displays, use precomputed wall candidates.
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
                    // Try candidates from precomputed corner positions.
                    if (!TryGetValidPosition(cornerPositions, entry.prefab, out spawnPos))
                    {
                        // Fallback: sample from the safe area until valid.
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
                        // We found a candidate from the cornerPositions list.
                        spawnSuccess = ValidateSpawnCandidate(spawnPos, Quaternion.identity, entry.prefab);
                    }
                }
                else if (entry.prefab.CompareTag("Computer"))
                {
                    // Try candidates from precomputed computer positions.
                    if (!TryGetValidPosition(computerPositions, entry.prefab, out spawnPos))
                    {
                        // Fallback: sample from the safe area until valid.
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
                else
                {
                    // For unknown tags, pick a random interior position (fallback) and validate it.
                    while (!spawnSuccess && attempts < maxAttempts)
                    {
                        spawnPos = GetRandomInteriorPosition();
                        if (ValidateSpawnCandidate(spawnPos, Quaternion.identity, entry.prefab))
                        {
                            spawnSuccess = true;
                        }
                        attempts++;
                    }
                }

                if (spawnSuccess)
                {
                    Object.Instantiate(entry.prefab, roomParent.TransformPoint(spawnPos), spawnRot, roomParent);
                }
                else
                {
                    Debug.LogWarning($"Failed to spawn {entry.prefab.name} in a non-overlapping position after {maxAttempts} attempts.");
                    // Optionally re-add to pool or schedule retry.
                }
            }
        }
    }

    /// <summary>
    /// Returns a random interior position within the room (fallback).
    /// Assumes the room's bottom-left is (0,0) in local space.
    /// </summary>
    private Vector3 GetRandomInteriorPosition()
    {
        float roomWidthWorld = width * TileSize;
        float roomHeightWorld = height * TileSize;
        float x = Random.Range(spawnMargin, roomWidthWorld - spawnMargin);
        float z = Random.Range(spawnMargin, roomHeightWorld - spawnMargin);
        return new Vector3(x, 0, z);
    }

    /// <summary>
    /// Attempts to pick a candidate position from a list that passes validation.
    /// Uses ValidateSpawnCandidate to check each candidate.
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
    /// Validates a candidate spawn position using the prefab's BoxCollider.
    /// Converts the candidate (in local space) to world space,
    /// then uses Physics.OverlapBox with a layer mask to detect any overlapping colliders.
    /// Only colliders on objects tagged "Wall" or "WallDisplay" will cause a failure.
    /// </summary>
    private bool ValidateSpawnCandidate(Vector3 candidate, Quaternion rotation, GameObject prefab)
    {
        Vector3 worldPos = roomParent.TransformPoint(candidate);
        BoxCollider prefabCollider = prefab.GetComponent<BoxCollider>();
        if (prefabCollider == null)
        {
            // If the prefab doesn't have a BoxCollider, assume it's valid.
            return true;
        }
        Vector3 halfExtents = prefabCollider.size * 0.5f;
        // Optionally, you could use layers here instead of tags.
        Collider[] overlaps = Physics.OverlapBox(worldPos, halfExtents, rotation);
        foreach (var col in overlaps)
        {
            Debug.Log("Overlap with object: " + col.gameObject.name + " tag: " + col.gameObject.tag);
            if (col.gameObject.CompareTag("Wall") || col.gameObject.CompareTag("WallDisplay"))
            {
                return false;
            }
        }
        return true;
    }
}
