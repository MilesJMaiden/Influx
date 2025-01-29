using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public GameObject floorPrefab, wallPrefab, doorPrefab, windowWallPrefab, wallDisplayPrefab;
    public GameObject containerPrefab, computerPrefab;

    private RoomGenerator roomGenerator;
    private RoomObjectSpawner objectSpawner;

    private void Start()
    {
        // Initialize object pools
        ObjectPool floorPool = new ObjectPool(floorPrefab, transform, 100);
        ObjectPool wallPool = new ObjectPool(wallPrefab, transform, 50);
        ObjectPool windowWallPool = new ObjectPool(windowWallPrefab, transform, 10);
        ObjectPool doorPool = new ObjectPool(doorPrefab, transform, 4);
        ObjectPool wallDisplayPool = new ObjectPool(wallDisplayPrefab, transform, 10);

        ObjectPool containerPool = new ObjectPool(containerPrefab, transform, 10);
        ObjectPool computerPool = new ObjectPool(computerPrefab, transform, 10);

        // Generate the room
        roomGenerator = new RoomGenerator(5, 5, floorPool, wallPool, windowWallPool, doorPool, wallDisplayPool);
        roomGenerator.GenerateRoom();

        // Get window wall positions
        HashSet<Vector3> windowWallPositions = roomGenerator.GetWindowWallPositions();

        // Pass windowWallPositions to RoomObjectSpawner
        objectSpawner = new RoomObjectSpawner(5, 5, containerPool, computerPool, wallDisplayPool, windowWallPositions);

        objectSpawner.SpawnObjects();
    }
}
