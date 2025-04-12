using UnityEngine;
using UnityEditor;

public class ReplaceWithPrefab : EditorWindow
{
    private GameObject prefab;

    [MenuItem("Tools/Replace With Prefab")]
    private static void ShowWindow()
    {
        GetWindow<ReplaceWithPrefab>("Replace With Prefab");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select Prefab to Replace With", EditorStyles.boldLabel);
        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

        if (GUILayout.Button("Replace Selected") && prefab != null)
        {
            ReplaceSelectedObjects();
        }
    }

    private void ReplaceSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected for replacement.");
            return;
        }

        foreach (GameObject selected in selectedObjects)
        {
            // Record the current transform data
            Vector3 position = selected.transform.position;
            Quaternion rotation = selected.transform.rotation;
            Vector3 scale = selected.transform.localScale;
            Transform parent = selected.transform.parent;

            // Instantiate the new prefab
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (newObject != null)
            {
                // Set the new object's transform to match the original
                newObject.transform.position = position;
                newObject.transform.rotation = rotation;
                newObject.transform.localScale = scale;
                newObject.transform.parent = parent;

                // Register the creation and deletion for undo operations
                Undo.RegisterCreatedObjectUndo(newObject, "Instantiate Prefab");
                Undo.DestroyObjectImmediate(selected);
            }
            else
            {
                Debug.LogError("Failed to instantiate prefab.");
            }
        }
    }
}
