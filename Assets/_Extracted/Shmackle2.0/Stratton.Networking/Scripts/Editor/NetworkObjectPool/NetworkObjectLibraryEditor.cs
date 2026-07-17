using Fusion;
using Stratton.Networking;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(NetworkObjectLibrary))]
public class NetworkObjectLibraryEditor : Editor
{
    private SerializedProperty _networkObjectsProp;
    private ReorderableList _reorderableList;
    private Dictionary<Object, NetworkObjectGuid> _networkObjectGuids = new();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        _reorderableList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
        Rect listRect = GUILayoutUtility.GetLastRect();
        listRect.y += _reorderableList.headerHeight;
    }

    private void OnEnable()
    {
        _networkObjectGuids.Clear();
        foreach (var prefabSource in NetworkProjectConfig.Global.PrefabTable.Prefabs)
        {
            var prefabSourceResource = prefabSource as NetworkPrefabSourceStaticLazy;
            if (prefabSourceResource == null) continue;
            _networkObjectGuids[prefabSourceResource.EditorInstance] = prefabSourceResource.AssetGuid;
        }
        _networkObjectsProp = serializedObject.FindProperty("_networkObjectsData");
        _reorderableList = new ReorderableList(serializedObject, _networkObjectsProp, true, true, true, true)
        {
            drawHeaderCallback = DrawHeader,
            drawElementCallback = DrawElement,
            elementHeightCallback = GetElementHeight
        };
    }

    private void DrawHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "NetworkObjects Library");
    }

    private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        if (_networkObjectsProp.arraySize == 0)
            return;
        var element = _networkObjectsProp.GetArrayElementAtIndex(index);
        var keyProp = element.FindPropertyRelative("NetworkObjectType");
        var nameProp = keyProp.FindPropertyRelative("_name");
        var networkObjectPrefabProp = element.FindPropertyRelative("NetworkObjectPrefab");
        var networkObjectGuidProp = element.FindPropertyRelative("NetworkObjectGuid");
        var poolableProp = element.FindPropertyRelative("Poolable");
        var prewarmedInstancesProp = element.FindPropertyRelative("PrewarmedInstances");
        var maxInstancesProp = element.FindPropertyRelative("MaxInstances");

        rect.y += 2;
        rect.height -= 4;

        EditorGUI.indentLevel++;

        // Get the position for the foldout and its label
        var foldoutPosition = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
        var labelPosition = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

        var style = new GUIStyle(GUIStyle.none);
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 14;
        var foldoutlabel = nameProp.stringValue;
        if (networkObjectPrefabProp.objectReferenceValue == null)
        {
            foldoutlabel += " (NetworkObject Prefab missing)";
            style.normal.textColor = Color.red;
            networkObjectGuidProp.stringValue = string.Empty;
        }
        else
        {
            style.normal.textColor = Color.white;
            if (_networkObjectGuids.ContainsKey(networkObjectPrefabProp.objectReferenceValue))
            {
                networkObjectGuidProp.stringValue = _networkObjectGuids[networkObjectPrefabProp.objectReferenceValue].ToString();
            }
        }
        element.isExpanded = EditorGUI.Foldout(foldoutPosition, element.isExpanded, new GUIContent(foldoutlabel), true, style);
        labelPosition.y += 2;

        if (element.isExpanded)
        {
            EditorGUI.indentLevel++;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight), rect.width, EditorGUIUtility.singleLineHeight), keyProp);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight + 2) * 2, rect.width, EditorGUIUtility.singleLineHeight), networkObjectPrefabProp);
            EditorGUI.LabelField(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight + 2) * 3, rect.width, EditorGUIUtility.singleLineHeight), "Network Object Guid", networkObjectGuidProp.stringValue);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight + 2) * 4, rect.width, EditorGUIUtility.singleLineHeight), poolableProp);

            if (poolableProp.boolValue)
            {
                EditorGUI.PropertyField(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight + 2) * 5, rect.width, EditorGUIUtility.singleLineHeight), prewarmedInstancesProp);

                EditorGUI.PropertyField(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight + 2) * 6, rect.width, EditorGUIUtility.singleLineHeight), maxInstancesProp);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.indentLevel = 0;
    }

    private float GetElementHeight(int index)
    {
        if (_networkObjectsProp.arraySize == 0)
            return 0;
        var element = _networkObjectsProp.GetArrayElementAtIndex(index);
        var poolableProp = element.FindPropertyRelative("Poolable");
        if (element.isExpanded)
        {
            if (poolableProp.boolValue)
            {
                return 140;
            }
            else
            {
                return 100;
            }
        }
        else
        {
            return 30;
        }
    }
}



