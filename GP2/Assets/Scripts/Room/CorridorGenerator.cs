using UnityEngine;
using Unity.AI.Navigation;

/// <summary>
/// Generates a corridor connecting two door positions.
/// Assumes the corridor prefab is a flat quad with a pivot at its center.
/// </summary>
public class CorridorGenerator
{
    private readonly GameObject corridorPrefab;
    private readonly Transform parent;

    /// <summary>
    /// Initializes the corridor generator.
    /// </summary>
    /// <param name="corridorPrefab">Prefab used for the corridor floor.</param>
    /// <param name="parent">Parent transform for organizational purposes.</param>
    public CorridorGenerator(GameObject corridorPrefab, Transform parent)
    {
        this.corridorPrefab = corridorPrefab;
        this.parent = parent;
    }

    /// <summary>
    /// Generates a corridor between two door positions.
    /// </summary>
    /// <param name="start">Starting door position.</param>
    /// <param name="end">Ending door position.</param>
    /// <returns>The instantiated corridor GameObject.</returns>
    public GameObject GenerateCorridor(Vector3 start, Vector3 end)
    {
        // Calculate the direction and length of the corridor.
        Vector3 direction = end - start;
        float length = direction.magnitude;
        Vector3 midPoint = (start + end) / 2f;
        // Use the same width as your TileSize (or choose a different width if desired).
        float corridorWidth = LevelGenerator.TileSize;

        // Instantiate the corridor prefab.
        GameObject corridor = Object.Instantiate(corridorPrefab, parent);
        corridor.transform.position = midPoint;

        // Assume the corridorPrefab is a unit quad lying in the X-Z plane with a pivot at the center.
        // Scale it so its X axis equals the corridor width and its Z axis equals the corridor length.
        corridor.transform.localScale = new Vector3(corridorWidth, 1f, length);

        // Determine the angle (in degrees) between the positive X-axis and the corridor direction.
        float angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
        // Rotate so that the corridor extends along the direction vector.
        // The 90° adjustment rotates the quad to lie flat (if your prefab was modeled for a room floor).
        corridor.transform.rotation = Quaternion.Euler(90f, -angle, 0f);

        // Optionally, add a NavMeshSurface for navigation if one isn’t already attached.
        NavMeshSurface navSurface = corridor.GetComponent<NavMeshSurface>();
        if (navSurface == null)
            corridor.AddComponent<NavMeshSurface>().collectObjects = CollectObjects.Children;
        navSurface.BuildNavMesh();

        return corridor;
    }
}
