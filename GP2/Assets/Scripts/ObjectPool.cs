using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic Object Pool for reusing GameObjects efficiently.
/// Ensures all objects are parented under a designated transform.
/// </summary>
public class ObjectPool
{
    private readonly Queue<GameObject> pool = new();
    private readonly GameObject prefab;
    private readonly Transform parent;

    /// <summary>
    /// Initializes the object pool.
    /// </summary>
    /// <param name="prefab">Prefab used for instantiation.</param>
    /// <param name="parent">Parent transform to group pooled objects.</param>
    /// <param name="initialSize">Initial number of objects to instantiate.</param>
    public ObjectPool(GameObject prefab, Transform parent, int initialSize = 10)
    {
        this.prefab = prefab;
        this.parent = parent;
        ExpandPool(initialSize);
    }

    /// <summary>
    /// Retrieves an object from the pool, activating it for use.
    /// </summary>
    public GameObject Get()
    {
        if (pool.Count == 0)
        {
            ExpandPool(1);
        }

        GameObject obj = pool.Dequeue();
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// Returns an object back to the pool, deactivating it.
    /// </summary>
    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(parent); // Ensure it remains organized
        pool.Enqueue(obj);
    }

    /// <summary>
    /// Expands the pool by instantiating new objects.
    /// </summary>
    private void ExpandPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject obj = Object.Instantiate(prefab, parent);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
}
