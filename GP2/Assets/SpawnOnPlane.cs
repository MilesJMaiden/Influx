using UnityEngine;
using System.Collections.Generic;

public class SpawnOnGrid : MonoBehaviour
{
    [System.Serializable]
    public class SpawnObject
    {
        [Tooltip("The prefab to spawn")]
        public GameObject prefab;

        [Tooltip("How many of this prefab to spawn")]
        public int quantity = 1;
    }

    [Header("What to spawn")]
    public SpawnObject[] spawnObjects;

    [Header("Spawn grid settings")]
    [Tooltip("Size of the spawning area (X = width, Y = length)")]
    public Vector2 gridSize = new Vector2(10000f, 10000f);

    [Tooltip("Fixed Y position for all spawns")]
    public float yPosition = -25f;

    [Tooltip("Center-to-center spacing (and grid cell size)")]
    public float minDistance = 3f;

    [Header("Scale settings")]
    [Tooltip("Minimum uniform scale for each object")]
    public float minScale = 5f;
    [Tooltip("Maximum uniform scale for each object")]
    public float maxScale = 22.5f;

    private List<Vector3> _slots;

    void Start()
    {
        BuildGridSlots();
        Shuffle(_slots);

        int slotIndex = 0;
        foreach (var entry in spawnObjects)
        {
            if (entry.prefab == null) continue;

            for (int i = 0; i < entry.quantity; i++)
            {
                if (slotIndex >= _slots.Count)
                {
                    Debug.LogWarning(
                        $"[SpawnOnGrid] Not enough slots for all objects! " +
                        $"Tried to place {entry.prefab.name} #{i + 1} but ran out of slots."
                    );
                    break;
                }

                var pos = _slots[slotIndex++];
                PlaceObject(entry.prefab, pos);
            }
        }
    }

    void BuildGridSlots()
    {
        _slots = new List<Vector3>();

        int countX = Mathf.FloorToInt(gridSize.x / minDistance);
        int countZ = Mathf.FloorToInt(gridSize.y / minDistance);

        // center grid on this GameObject’s position
        Vector3 origin = transform.position;
        float startX = origin.x - gridSize.x * 0.5f + minDistance * 0.5f;
        float startZ = origin.z - gridSize.y * 0.5f + minDistance * 0.5f;

        for (int ix = 0; ix < countX; ix++)
            for (int iz = 0; iz < countZ; iz++)
            {
                float x = startX + ix * minDistance;
                float z = startZ + iz * minDistance;
                _slots.Add(new Vector3(x, yPosition, z));
            }
    }

    void PlaceObject(GameObject prefab, Vector3 worldPos)
    {
        // Instantiate as a child of "this" transform,
        // keeping the world‐space position/rotation:
        var go = Instantiate(prefab, worldPos, Quaternion.identity, transform);

        // uniform random scale
        float s = Random.Range(minScale, maxScale);
        go.transform.localScale = Vector3.one * s;
    }

    // Fisher–Yates shuffle
    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}
