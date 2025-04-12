using UnityEngine;
using UnityEditor;

public class GroupAtCenter : MonoBehaviour
{
    [MenuItem("GameObject/Group/Group At Center", false, 0)]
    private static void GroupSelectedAtCenter()
    {
        var selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0) return;

        Vector3 center = CalculateCenterPoint(selectedObjects);
        GameObject group = new GameObject("Group");
        group.transform.position = center;
        group.transform.rotation = Quaternion.identity;
        group.transform.localScale = Vector3.one;

        Undo.RegisterCreatedObjectUndo(group, "Create Group At Center");

        foreach (GameObject obj in selectedObjects)
        {
            Undo.SetTransformParent(obj.transform, group.transform, "Reparent To Center Group");
        }

        Selection.activeGameObject = group;
    }

    private static Vector3 CalculateCenterPoint(GameObject[] objects)
    {
        if (objects.Length == 1)
            return objects[0].transform.position;

        Bounds bounds = new Bounds(objects[0].transform.position, Vector3.zero);
        foreach (GameObject obj in objects)
        {
            bounds.Encapsulate(obj.transform.position);
        }
        return bounds.center;
    }
}
