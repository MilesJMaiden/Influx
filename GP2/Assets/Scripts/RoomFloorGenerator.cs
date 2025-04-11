using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class RoomFloorGenerator
{
    private GameObject floorPlanePrefab;
    private Transform parent;
    private float roomWidth;  // in world units
    private float roomHeight; // in world units

    /// <summary>
    /// Constructor for generating a floor plane.
    /// </summary>
    /// <param name="floorPlanePrefab">
    /// A prefab for the floor (e.g. a Plane) whose pivot is at the center.
    /// </param>
    /// <param name="parent">
    /// The parent transform (typically the room container, whose position is the room's bottom-left).
    /// </param>
    /// <param name="roomWidth">The room's width in world units.</param>
    /// <param name="roomHeight">The room's height in world units.</param>
    public RoomFloorGenerator(GameObject floorPlanePrefab, Transform parent, float roomWidth, float roomHeight)
    {
        this.floorPlanePrefab = floorPlanePrefab;
        this.parent = parent;
        this.roomWidth = roomWidth;
        this.roomHeight = roomHeight;
    }

    /// <summary>
    /// Instantiates the floor plane as a child of the parent, scales it to match the room dimensions,
    /// applies an offset of -2.5 to align the plane's center with the room's center, and sets its X rotation to 270°.
    /// A NavMeshSurface component is also added.
    /// </summary>
    public GameObject GenerateFloor()
    {
        // Instantiate the floor plane as a child of the parent.
        GameObject floor = Object.Instantiate(floorPlanePrefab, parent);
        // Scale the plane to the room's dimensions (assuming the prefab is 1x1 units).
        floor.transform.localScale = new Vector3(roomWidth, roomHeight, 1f);
        // Because the room container's position is the bottom-left of the room,
        // and to align the plane's center with the room's center, we offset by (roomWidth/2 - 2.5, 0, roomHeight/2 - 2.5).
        floor.transform.localPosition = new Vector3(roomWidth / 2f - 2.5f, 0, roomHeight / 2f - 2.5f);
        // Set the plane to lie flat. (X rotation = 270° so that it faces upward.)
        floor.transform.localRotation = Quaternion.Euler(270f, 0f, 0f);

        // Add a NavMeshSurface component for navigation.
        NavMeshSurface navSurface = floor.AddComponent<NavMeshSurface>();
        navSurface.collectObjects = CollectObjects.Children;
        // (Additional configuration on navSurface can be done here.)

        return floor;
    }
}
