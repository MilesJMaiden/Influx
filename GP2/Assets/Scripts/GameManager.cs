//using UnityEngine;

//public class GameManager : MonoBehaviour
//{
//    [Header("Prefabs")]
//    [SerializeField] private GameObject floorPrefab;
//    [SerializeField] private GameObject wallPrefab;
//    [SerializeField] private GameObject doorPrefab;
//    [SerializeField] private GameObject windowWallPrefab;
//    [SerializeField] private GameObject wallDisplayPrefab;
//    [SerializeField] private GameObject containerPrefab;
//    [SerializeField] private GameObject computerPrefab;

//    // Using interfaces allows swapping implementations later
//    private IRoomGenerator roomGenerator;
//    private IRoomObjectSpawner objectSpawner;

//    private void Start()
//    {
//        // Create object pools using a simple factory method (or dependency injection if available)
//        ObjectPool floorPool = new ObjectPool(floorPrefab, transform, 100);
//        ObjectPool wallPool = new ObjectPool(wallPrefab, transform, 50);
//        ObjectPool windowWallPool = new ObjectPool(windowWallPrefab, transform, 10);
//        ObjectPool doorPool = new ObjectPool(doorPrefab, transform, 4);
//        ObjectPool wallDisplayPool = new ObjectPool(wallDisplayPrefab, transform, 10);
//        ObjectPool containerPool = new ObjectPool(containerPrefab, transform, 10);
//        ObjectPool computerPool = new ObjectPool(computerPrefab, transform, 10);

//        // Inject dependencies into the room generator
//        roomGenerator = new RoomGenerator(
//            width: 5,
//            height: 5,
//            floorPool: floorPool,
//            wallPool: wallPool,
//            windowWallPool: windowWallPool,
//            doorPool: doorPool,
//            wallDisplayPool: wallDisplayPool,
//            connections: new RoomConnections(top: false, bottom: false, left: false, right: false)
//        );

//        roomGenerator.GenerateRoom();

//        // Inject dependencies into the object spawner using 'width' and 'height' parameter names
//        objectSpawner = new RoomObjectSpawner(
//            width: 5,
//            height: 5,
//            containerPool: containerPool,
//            computerPool: computerPool,
//            wallDisplayPool: wallDisplayPool,
//            windowWallPositions: roomGenerator.GetWindowWallPositions());

//        objectSpawner.SpawnObjects();
//    }
//}
