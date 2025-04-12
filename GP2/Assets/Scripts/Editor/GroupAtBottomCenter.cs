using UnityEngine;
using UnityEditor;

public class GroupAtBottomCenter : MonoBehaviour
{
    [MenuItem("GameObject/Group/Group At Bottom Center", false, 1)]
    private static void GroupSelectedAtBottomCenter()
    {
        var selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0) return;

        Vector3 center = CalculateBottomCenter(selectedObjects);
        GameObject group = new GameObject("Group_BottomCenter");
        group.transform.position = center;
        group.transform.rotation = Quaternion.identity;
        group.transform.localScale = Vector3.one;

        Undo.RegisterCreatedObjectUndo(group, "Create Group At Bottom Center");

        foreach (GameObject obj in selectedObjects)
        {
            Undo.SetTransformParent(obj.transform, group.transform, "Reparent To Bottom Center Group");
        }

        Selection.activeGameObject = group;
    }

    private static Vector3 CalculateBottomCenter(GameObject[] objects)
    {
        if (objects.Length == 1)
        {
            Renderer rend = objects[0].GetComponentInChildren<Renderer>();
            return rend ? new Vector3(rend.bounds.center.x, rend.bounds.min.y, rend.bounds.center.z)
                        : objects[0].transform.position;
        }

        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasRenderer = false;

        foreach (GameObject obj in objects)
        {
            Renderer rend = obj.GetComponentInChildren<Renderer>();
            if (rend)
            {
                if (!hasRenderer)
                {
                    bounds = rend.bounds;
                    hasRenderer = true;
                }
                else
                {
                    bounds.Encapsulate(rend.bounds);
                }
            }
        }

        if (!hasRenderer)
            return objects[0].transform.position;

        return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }
}
