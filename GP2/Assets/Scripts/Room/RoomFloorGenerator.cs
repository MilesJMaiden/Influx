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
    /// applies an offset of -2.5 to align the plane's center with the room's center, and sets its rotation.
    /// A NavMeshSurface component is also added.
    /// </summary>
    public GameObject GenerateFloor()
    {
        // Instantiate the floor plane as a child of the parent.
        GameObject floor = Object.Instantiate(floorPlanePrefab, parent);
        // For a built-in Plane, the default size is 10x10 units.
        // To cover a room that is roomWidth x roomHeight (in world units), scale X and Z accordingly.
        floor.transform.localScale = new Vector3(roomWidth / 10f, 1f, roomHeight / 10f);
        // Keep the offset: since we require an offset of 2.5 in X and Z,
        // position the floor so its center is at (roomWidth/2 - 2.5, roomHeight/2 - 2.5) in local space.
        floor.transform.localPosition = new Vector3(roomWidth / 2f - 2.5f, 0, roomHeight / 2f - 2.5f);
        // Do not change the upward normal if you require the offset to be preserved.
        // For a built-in Plane, the normal is already upward.
        floor.transform.localRotation = Quaternion.identity;

        // Add a NavMeshSurface component for navigation.
        NavMeshSurface navSurface = floor.AddComponent<NavMeshSurface>();
        navSurface.collectObjects = CollectObjects.Children;
        // (Any additional configuration for navSurface can be performed here.)

        return floor;
    }
}
