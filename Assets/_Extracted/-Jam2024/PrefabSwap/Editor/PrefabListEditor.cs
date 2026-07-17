using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PrefabList))]
public class PrefabListEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Reference to the target script (PrefabList)
        PrefabList prefabList = (PrefabList)target;

        // Add a button that calls the GetPrefabName method
        if (GUILayout.Button("Assign Prefab Names"))
        {
            prefabList.GetPrefabName();
            EditorUtility.SetDirty(prefabList); // Mark the object as dirty to save the changes
        }
        // Draw the default inspector first
        DrawDefaultInspector();

        
    }
}
