using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PrefabSwapper))]
public class PrefabSwapperEditor : Editor
{
    SerializedProperty prefabListProperty;
    SerializedProperty selectedPrefabIndexProperty;

    private void OnEnable()
    {
        prefabListProperty = serializedObject.FindProperty("prefabList");
        selectedPrefabIndexProperty = serializedObject.FindProperty("selectedPrefabIndex");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Display the prefab list field in the inspector
        EditorGUILayout.PropertyField(prefabListProperty, new GUIContent("Prefab List"));
        PrefabSwapper manager = (PrefabSwapper)target;

        if (manager.prefabList != null && manager.prefabList.prefabList != null && manager.prefabList.prefabList.Count > 0)
        {
            // Create an array to hold the names of the prefabs
            string[] prefabNames = new string[manager.prefabList.prefabList.Count];
            for (int i = 0; i < manager.prefabList.prefabList.Count; i++)
            {
                prefabNames[i] = manager.prefabList.prefabList[i].name;
            }

            // Display a dropdown to select the prefab
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Select Prefab", selectedPrefabIndexProperty.intValue, prefabNames);
            if (EditorGUI.EndChangeCheck())
            {
                selectedPrefabIndexProperty.intValue = newIndex;
                serializedObject.ApplyModifiedProperties();

                // Change the prefab immediately after selecting it from the dropdown
                string selectedPrefabName = prefabNames[newIndex];
                manager.ChangePrefab(selectedPrefabName);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Prefab List is not assigned or empty.", MessageType.Warning);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
