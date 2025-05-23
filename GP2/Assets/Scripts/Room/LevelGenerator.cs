﻿using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.UI;

public enum DoorDirection { Top, Bottom, Left, Right }

public class LevelGenerator : MonoBehaviour
{
    [Header("Design Settings")]
    public LevelDesignSettings designSettings; // Contains spawn settings, dimensions, and connection probabilities.

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public GameObject windowWallPrefab;
    public GameObject wallDisplayPrefab;
    public GameObject containerPrefab;
    public GameObject computerPrefab;
    public GameObject floorQuadPrefab;
    public GameObject cornerWallPrefab;

    [Header("Layout Settings")]
    // The gap (in world units) between rooms – filled by corridor rooms.
    public float corridorWidth = 5f;
    //World units per tile.
    public const float TileSize = 5f;
    // List of generated room data.
    private List<RoomData> rooms;
    // For centering rooms in grid cells.
    private Dictionary<int, float> columnWidths;
    private Dictionary<int, float> rowHeights;

    [Header("Alert Settings")]
    [SerializeField] private GameObject redAlertRoomLight;
    [SerializeField] AudioClip alertClip;
    [SerializeField] private GameObject alienPrefab;

    [Header("Variant Selection UI")]
    [SerializeField] private Canvas variantSelectionCanvas;
    [SerializeField] private Button[] variantButtons = new Button[3];
    [SerializeField] private LevelDesignSettings[] designVariants = new LevelDesignSettings[3];

    [Header("HUD")]
    [SerializeField] private Canvas hudCanvas;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip selectClip;

    private void Start()
    {
        // hide it initially
        hudCanvas.gameObject.SetActive(false);

        variantSelectionCanvas.gameObject.SetActive(true);

        for (int i = 0; i < variantButtons.Length; i++)
        {
            int index = i;
            variantButtons[index].onClick.RemoveAllListeners();
            variantButtons[index].onClick.AddListener(() => OnVariantSelected(index));
        }
    }

    private void OnVariantSelected(int variantIndex)
    {
        // play the click sound
        if (sfxSource != null && selectClip != null)
            sfxSource.PlayOneShot(selectClip);

        // now hide UI & set the chosen settings
        designSettings = designVariants[variantIndex];
        variantSelectionCanvas.gameObject.SetActive(false);

        // show the HUD now that we’re “in game”
        hudCanvas.gameObject.SetActive(true);

        bool success = false;
        while (!success)
        {
            success = BuildLevel();
            if (!success)
            {
                Debug.LogWarning("Level generation failed—destroying & retrying.");
                // tear down everything created so far
                for (int i = transform.childCount - 1; i >= 0; i--)
                    DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
    }

    /// <summary>
    /// Moves all of your original Start() logic here, except:
    /// 1) Wherever you call spawner.SpawnObjects(), capture its bool return.
    /// 2) If any call returns false, immediately return false (to trigger a retry).
    /// 3) At the end, return true.
    /// Additionally, this version sets up one "red alert" light per room (disabled by default)
    /// and hooks up a RoomAlertController to blink it whenever *all* that room's computers read 0.
    /// </summary>
    private bool BuildLevel()
    {
        // 1) Room layout
        int roomCount = designSettings.roomSpawnSettings.Length;
        rooms = GenerateRooms(roomCount);
        var basePositions = ComputeBasePositions(rooms);
        var roomPositions = ComputeRoomPositions(rooms, basePositions);

        // 2) Root containers
        var levelContainer = new GameObject("Level");
        levelContainer.transform.parent = transform;
        var corridorContainer = new GameObject("Corridors");
        corridorContainer.transform.parent = levelContainer.transform;

        // 3) Gather for agent spawn
        var agentRoomData = new List<(Transform, Vector2, RoomSpawnSettings)>();
        var roomLookup = new Dictionary<RoomData, Transform>();

        // 4) Build each room
        for (int i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            // container
            var roomGO = new GameObject($"Room_{room.gridPosition.x}_{room.gridPosition.y}");
            roomGO.transform.parent = levelContainer.transform;
            roomGO.transform.position = roomPositions[room];
            roomLookup[room] = roomGO.transform;

            // floor
            float w = room.dimensions.x * TileSize;
            float h = room.dimensions.y * TileSize;
            new RoomFloorGenerator(floorQuadPrefab, roomGO.transform, w, h).GenerateFloor();

            // pools + geometry
            var floorPool = new ObjectPool(floorPrefab, roomGO.transform, 100);
            var wallPool = new ObjectPool(wallPrefab, roomGO.transform, 50);
            var winPool = new ObjectPool(windowWallPrefab, roomGO.transform, 10);
            var doorPool = new ObjectPool(doorPrefab, roomGO.transform, 4);
            var dispPool = new ObjectPool(wallDisplayPrefab, roomGO.transform, 10);

            new RoomGenerator(
                (int)room.dimensions.x,
                (int)room.dimensions.y,
                floorPool, wallPool, winPool, doorPool, dispPool,
                room.connections
            ).GenerateRoom();

            // object spawner
            var spawnSettings = designSettings.roomSpawnSettings[i];
            var containerPool2 = new ObjectPool(containerPrefab, roomGO.transform, 10);
            var computerPool2 = new ObjectPool(computerPrefab, roomGO.transform, 10);
            var spawner = new RoomObjectSpawner(
                (int)room.dimensions.x,
                (int)room.dimensions.y,
                spawnSettings,
                containerPool2,
                computerPool2,
                dispPool,
                roomGO.transform
            );
            if (!spawner.SpawnObjects())
                return false;  // abort on any failure

            // corner walls
            var bl = new Vector3(-1.25f, 0, -1.25f);
            var br = new Vector3(w - 3.75f, 0, -1.25f);
            var tl = new Vector3(-1.25f, 0, h - 3.75f);
            var tr = new Vector3(w - 3.75f, 0, h - 3.75f);
            var rots = new Quaternion[]
            {
            Quaternion.Euler(0,180,0),
            Quaternion.Euler(0, 90,0),
            Quaternion.Euler(0,270,0),
            Quaternion.Euler(0,  0,0)
            };
            var poses = new Vector3[] { bl, br, tl, tr };
            for (int j = 0; j < 4; j++)
                Instantiate(cornerWallPrefab,
                            roomGO.transform.TransformPoint(poses[j]),
                            rots[j],
                            roomGO.transform);

            // --- RED ALERT LIGHT SETUP ---
            // 1) Instantiate & size to floor footprint
            float roomWidthWorld = w;
            float roomHeightWorld = h;
            // offset X/Z by -2.5, lift to y = 0.1
            Vector3 localCenter = new Vector3(
                roomWidthWorld / 2f - 2.5f,
                0.1f,
                roomHeightWorld / 2f - 2.5f
            );
            var alertGO = Instantiate(
                redAlertRoomLight,
                roomGO.transform.TransformPoint(localCenter),
                Quaternion.identity,
                roomGO.transform
            );
            // match floor X/Z, stretch Y to 10
            alertGO.transform.localScale = new Vector3(
                roomWidthWorld,
                10f,
                roomHeightWorld
            );
            alertGO.SetActive(false);


            // 2) Collect this room’s computer sliders
            var sliders = roomGO.GetComponentsInChildren<Computer>()
                                .Select(c => c.GetComponentInChildren<Slider>())
                                .Where(s => s != null)
                                .ToList();

            // 3) Attach & initialize controller
            var controller = roomGO.AddComponent<RoomAlertController>();
            controller.Initialize(alertGO, sliders, alertClip, alienPrefab);


            // end per‐room
            agentRoomData.Add((roomGO.transform, room.dimensions, spawnSettings));
        }

        // 5) Corridors, navmesh, agents, camera
        ConnectRooms(roomLookup, corridorContainer.transform);
        BakeNavMeshes();

        foreach (var (roomT, dims, settings) in agentRoomData)
            foreach (var entry in settings.spawnEntries)
                if (entry.prefab != null && entry.prefab.CompareTag("Agent"))
                    AgentManager.Instance.SpawnAgentsForRoom(roomT, dims, entry);

        var bounds = ComputeBoundsFromChildren(levelContainer);
        CameraManager.Instance.SetLevelBounds(bounds);

        return true;
    }

    /// <summary>
    /// Computes a bounding box that encapsulates all Renderers in the children of the given root.
    /// </summary>
    private Bounds ComputeBoundsFromChildren(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.zero);
        }
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer rend in renderers)
        {
            bounds.Encapsulate(rend.bounds);
        }
        return bounds;
    }

    #region Connectivity & Positioning

    private List<RoomData> GenerateRooms(int count)
    {
        List<RoomData> roomList = new List<RoomData>();
        Dictionary<Vector2Int, RoomData> grid = new Dictionary<Vector2Int, RoomData>();
        HashSet<Vector2Int> frontier = new HashSet<Vector2Int>();

        Vector2Int startPos = Vector2Int.zero;
        RoomData startRoom = new RoomData(startPos, GetRandomRoomShape(), new RoomConnections(false, false, false, false), GetRandomRoomDimensions());
        roomList.Add(startRoom);
        grid[startPos] = startRoom;
        AddFrontier(frontier, grid, startPos);

        while (roomList.Count < count && frontier.Count > 0)
        {
            Vector2Int pos = frontier.ElementAt(Random.Range(0, frontier.Count));
            frontier.Remove(pos);

            List<Vector2Int> adjacent = new List<Vector2Int>
            {
                pos + Vector2Int.up,
                pos + Vector2Int.down,
                pos + Vector2Int.left,
                pos + Vector2Int.right
            };
            List<RoomData> connectedNeighbors = adjacent.Where(p => grid.ContainsKey(p)).Select(p => grid[p]).ToList();
            if (connectedNeighbors.Count == 0)
                continue;
            RoomData neighbor = connectedNeighbors[Random.Range(0, connectedNeighbors.Count)];
            RoomConnections con = new RoomConnections(false, false, false, false);
            Vector2Int diff = pos - neighbor.gridPosition;
            if (diff == Vector2Int.up)
            {
                con.bottom = true;
                neighbor.connections.top = true;
            }
            else if (diff == Vector2Int.down)
            {
                con.top = true;
                neighbor.connections.bottom = true;
            }
            else if (diff == Vector2Int.left)
            {
                con.right = true;
                neighbor.connections.left = true;
            }
            else if (diff == Vector2Int.right)
            {
                con.left = true;
                neighbor.connections.right = true;
            }
            RoomData newRoom = new RoomData(pos, GetRandomRoomShape(), con, GetRandomRoomDimensions());
            roomList.Add(newRoom);
            grid[pos] = newRoom;
            AddFrontier(frontier, grid, pos);
        }
        return roomList;
    }

    private void AddFrontier(HashSet<Vector2Int> frontier, Dictionary<Vector2Int, RoomData> grid, Vector2Int pos)
    {
        foreach (Vector2Int d in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            Vector2Int neighbor = pos + d;
            if (!grid.ContainsKey(neighbor))
                frontier.Add(neighbor);
        }
    }

    private Dictionary<RoomData, Vector3> ComputeBasePositions(List<RoomData> rooms)
    {
        Dictionary<RoomData, Vector3> basePositions = new Dictionary<RoomData, Vector3>();
        List<int> uniqueXs = rooms.Select(r => r.gridPosition.x).Distinct().OrderBy(x => x).ToList();
        List<int> uniqueYs = rooms.Select(r => r.gridPosition.y).Distinct().OrderBy(y => y).ToList();

        columnWidths = new Dictionary<int, float>();
        foreach (int x in uniqueXs)
        {
            float maxWidth = rooms.Where(r => r.gridPosition.x == x)
                                  .Max(r => r.dimensions.x * TileSize);
            columnWidths[x] = maxWidth;
        }
        rowHeights = new Dictionary<int, float>();
        foreach (int y in uniqueYs)
        {
            float maxHeight = rooms.Where(r => r.gridPosition.y == y)
                                   .Max(r => r.dimensions.y * TileSize);
            rowHeights[y] = maxHeight;
        }
        foreach (RoomData room in rooms)
        {
            int colIndex = uniqueXs.IndexOf(room.gridPosition.x);
            int rowIndex = uniqueYs.IndexOf(room.gridPosition.y);
            float offsetX = 0f;
            float offsetZ = 0f;
            for (int i = 0; i < colIndex; i++)
                offsetX += columnWidths[uniqueXs[i]];
            offsetX += colIndex * corridorWidth;
            for (int i = 0; i < rowIndex; i++)
                offsetZ += rowHeights[uniqueYs[i]];
            offsetZ += rowIndex * corridorWidth;
            basePositions[room] = new Vector3(offsetX, 0, offsetZ);
        }
        return basePositions;
    }

    private Dictionary<RoomData, Vector3> ComputeRoomPositions(List<RoomData> rooms, Dictionary<RoomData, Vector3> basePositions)
    {
        Dictionary<RoomData, Vector3> adjustedPositions = new Dictionary<RoomData, Vector3>();
        foreach (RoomData room in rooms)
        {
            int col = room.gridPosition.x;
            int row = room.gridPosition.y;
            float cellWidth = columnWidths[col];
            float cellHeight = rowHeights[row];
            float roomWidth = room.dimensions.x * TileSize;
            float roomHeight = room.dimensions.y * TileSize;
            Vector3 offset = new Vector3((cellWidth - roomWidth) / 2f, 0, (cellHeight - roomHeight) / 2f);
            adjustedPositions[room] = basePositions[room] + offset;
        }
        return adjustedPositions;
    }

    private Vector3 GetLocalDoorPosition(RoomData room, DoorDirection dir)
    {
        float w = room.dimensions.x * TileSize;
        float h = room.dimensions.y * TileSize;
        switch (dir)
        {
            case DoorDirection.Top:
                return new Vector3(w / 2, 0, h);
            case DoorDirection.Bottom:
                return new Vector3(w / 2, 0, 0);
            case DoorDirection.Left:
                return new Vector3(0, 0, h / 2);
            case DoorDirection.Right:
                return new Vector3(w, 0, h / 2);
            default:
                return Vector3.zero;
        }
    }

    private void ConnectRooms(Dictionary<RoomData, Transform> roomLookup, Transform corridorParent)
    {
        HashSet<(RoomData, RoomData)> generatedCorridors = new HashSet<(RoomData, RoomData)>();
        foreach (var kvp in roomLookup)
        {
            RoomData room = kvp.Key;
            Transform roomT = kvp.Value;
            // Top connection.
            if (room.connections.top)
            {
                RoomData neighbor = roomLookup.Keys.FirstOrDefault(r => r.gridPosition == room.gridPosition + Vector2Int.up);
                if (neighbor != null && neighbor.connections.bottom && room.gridPosition.y < neighbor.gridPosition.y)
                {
                    if (!generatedCorridors.Contains((room, neighbor)) && !generatedCorridors.Contains((neighbor, room)))
                    {
                        Transform neighborT = roomLookup[neighbor];
                        Vector3 doorPosRoom = roomT.position + GetLocalDoorPosition(room, DoorDirection.Top) + Vector3.forward * (corridorWidth / 2);
                        Vector3 doorPosNeighbor = neighborT.position + GetLocalDoorPosition(neighbor, DoorDirection.Bottom) - Vector3.forward * (corridorWidth / 2);
                        GenerateCorridorRoom(doorPosRoom, doorPosNeighbor, vertical: true, corridorParent);
                        generatedCorridors.Add((room, neighbor));
                    }
                }
            }
            // Right connection.
            if (room.connections.right)
            {
                RoomData neighbor = roomLookup.Keys.FirstOrDefault(r => r.gridPosition == room.gridPosition + Vector2Int.right);
                if (neighbor != null && neighbor.connections.left && room.gridPosition.x < neighbor.gridPosition.x)
                {
                    if (!generatedCorridors.Contains((room, neighbor)) && !generatedCorridors.Contains((neighbor, room)))
                    {
                        Transform neighborT = roomLookup[neighbor];
                        Vector3 doorPosRoom = roomT.position + GetLocalDoorPosition(room, DoorDirection.Right) + Vector3.right * (corridorWidth / 2);
                        Vector3 doorPosNeighbor = neighborT.position + GetLocalDoorPosition(neighbor, DoorDirection.Left) - Vector3.right * (corridorWidth / 2);
                        GenerateCorridorRoom(doorPosRoom, doorPosNeighbor, vertical: false, corridorParent);
                        generatedCorridors.Add((room, neighbor));
                    }
                }
            }
        }
    }

    // Generates a corridor room between two door positions.
    private void GenerateCorridorRoom(Vector3 doorPosA, Vector3 doorPosB, bool vertical, Transform corridorParent)
    {
        Vector3 mid = (doorPosA + doorPosB) / 2f;
        float distance = vertical ? Mathf.Abs(doorPosB.z - doorPosA.z) : Mathf.Abs(doorPosB.x - doorPosA.x);
        float corridorLengthWorld = Mathf.Max(TileSize, distance * 2f);
        int corridorTiles = Mathf.CeilToInt(corridorLengthWorld / TileSize);
        int gridWidth = vertical ? 1 : corridorTiles;
        int gridHeight = vertical ? corridorTiles : 1;
        float corridorRoomWidth = gridWidth * TileSize;
        float corridorRoomHeight = gridHeight * TileSize;
        Vector3 bottomLeft;
        if (vertical)
        {
            bottomLeft = mid - new Vector3(TileSize / 2f, 0, corridorRoomHeight / 2f);
        }
        else
        {
            bottomLeft = mid - new Vector3(corridorRoomWidth / 2f, 0, TileSize / 2f);
        }
        GameObject corridorRoom = new GameObject("Corridor_" + (vertical ? "Vertical" : "Horizontal"));
        corridorRoom.transform.parent = corridorParent;
        corridorRoom.transform.position = bottomLeft;

        RoomFloorGenerator floorGen = new RoomFloorGenerator(floorQuadPrefab, corridorRoom.transform, corridorRoomWidth, corridorRoomHeight);
        floorGen.GenerateFloor();

        ObjectPool corridorFloorPool = new ObjectPool(floorPrefab, corridorRoom.transform, corridorTiles * corridorTiles);
        ObjectPool corridorWallPool = new ObjectPool(wallPrefab, corridorRoom.transform, corridorTiles * 2);
        ObjectPool corridorWindowWallPool = new ObjectPool(windowWallPrefab, corridorRoom.transform, corridorTiles);
        ObjectPool corridorDoorPool = new ObjectPool(doorPrefab, corridorRoom.transform, 2);
        ObjectPool corridorWallDisplayPool = new ObjectPool(wallDisplayPrefab, corridorRoom.transform, corridorTiles);

        RoomConnections connections = vertical
            ? new RoomConnections(true, true, false, false)
            : new RoomConnections(false, false, true, true);
        RoomGenerator roomGen = new RoomGenerator(
            width: gridWidth,
            height: gridHeight,
            floorPool: corridorFloorPool,
            wallPool: corridorWallPool,
            windowWallPool: corridorWindowWallPool,
            doorPool: corridorDoorPool,
            wallDisplayPool: corridorWallDisplayPool,
            connections: connections);
        roomGen.GenerateRoom();

        ObjectPool corridorContainerPool = new ObjectPool(containerPrefab, corridorRoom.transform, 2);
        ObjectPool corridorComputerPool = new ObjectPool(computerPrefab, corridorRoom.transform, 2);
        RoomObjectSpawner spawner = new RoomObjectSpawner(
            width: gridWidth,
            height: gridHeight,
            spawnSettings: new RoomSpawnSettings(), // empty settings
            containerPool: corridorContainerPool,
            computerPool: corridorComputerPool,
            wallDisplayPool: corridorWallDisplayPool,
            roomParent: corridorRoom.transform,
            isCorridor: true);
        spawner.SpawnObjects();

        AddNavMeshLinkToCorridor(corridorRoom, vertical, corridorRoomWidth, corridorRoomHeight, corridorWidth);
    }

    /// <summary>
    /// Adds a NavMeshLink to the given corridor room so that it exactly spans the corridor floor,
    /// with a 2.5f deduction from the computed center, and with -1 added to the X start point and +1 to the X end point.
    /// The rotation is set based solely on the vertical flag: if vertical is true, the link is rotated 90°; if false, 0.
    /// </summary>
    /// <param name="corridorRoom">The corridor GameObject (generated by GenerateCorridorRoom).</param>
    /// <param name="vertical">True if the corridor is vertical; false if horizontal.</param>
    /// <param name="corridorRoomWidth">The corridor room’s width in world units (gridWidth * TileSize).</param>
    /// <param name="corridorRoomHeight">The corridor room’s height in world units (gridHeight * TileSize).</param>
    /// <param name="corridorWidth">The desired NavMeshLink width.</param>
    private void AddNavMeshLinkToCorridor(GameObject corridorRoom, bool vertical,
        float corridorRoomWidth, float corridorRoomHeight, float corridorWidth)
    {
        // Create the NavMeshLink GameObject as a child of the corridorRoom.
        GameObject linkObject = new GameObject("CorridorNavMeshLink");
        linkObject.transform.SetParent(corridorRoom.transform, false);

        // Compute the corridor's center in local space (assuming the corridorRoom pivot is at the bottom-left)
        // and deduct 2.5f from the x and z coordinates.
        Vector3 localCenter = new Vector3((corridorRoomWidth / 2f) - 2.5f, 0f, (corridorRoomHeight / 2f) - 2.5f);
        linkObject.transform.localPosition = localCenter;

        // Add the NavMeshLink component.
        NavMeshLink link = linkObject.AddComponent<NavMeshLink>();
        link.width = corridorWidth;
        link.bidirectional = true;

        // Set rotation and endpoints based solely on the vertical flag.
        if (vertical)
        {
            // Vertical corridor: rotate 90° so the link’s local X axis aligns with the corridor’s long (vertical) direction.
            linkObject.transform.localRotation = Quaternion.Euler(0, 90, 0);
            float halfLength = corridorRoomHeight / 2f;
            link.startPoint = new Vector3(-halfLength - 1f, 0, 0);
            link.endPoint = new Vector3(halfLength + 1f, 0, 0);
        }
        else
        {
            // Horizontal corridor: rotation is explicitly set to 0.
            linkObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
            float halfLength = corridorRoomWidth / 2f;
            link.startPoint = new Vector3(-halfLength - 1f, 0, 0);
            link.endPoint = new Vector3(halfLength + 1f, 0, 0);
        }
    }

    private void BakeNavMeshes()
    {
        NavMeshSurface[] surfaces = FindObjectsOfType<NavMeshSurface>();
        foreach (NavMeshSurface surface in surfaces)
        {
            surface.BuildNavMesh();
        }
    }

    #endregion

    #region Room Generation

    private RoomShape GetRandomRoomShape()
    {
        float roll = Random.value;
        if (roll < designSettings.probabilitySquare)
            return RoomShape.Square;
        else if (roll < designSettings.probabilitySquare + designSettings.probabilityLShaped)
            return RoomShape.LShaped;
        else
            return RoomShape.TShaped;
    }

    private Vector2 GetRandomRoomDimensions()
    {
        float w = Random.Range(designSettings.minRoomSize.x, designSettings.maxRoomSize.x);
        float h = Random.Range(designSettings.minRoomSize.y, designSettings.maxRoomSize.y);
        int width = Mathf.RoundToInt(w);
        int height = Mathf.RoundToInt(h);
        if (width % 2 == 0) width += 1;
        if (height % 2 == 0) height += 1;
        return new Vector2(width, height);
    }

    #endregion
}
