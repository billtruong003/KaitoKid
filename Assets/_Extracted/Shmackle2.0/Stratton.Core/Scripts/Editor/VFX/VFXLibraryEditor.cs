using System.Collections.Generic;
using Stratton.VFX;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(VFXLibrary))]
public class VFXLibraryEditor : Editor
{
    private VFXLibrary _vfxLibrary;
    private int _selection = -1;

    public override void OnInspectorGUI()
    {
        _vfxLibrary = (VFXLibrary)target;

        EditorGUILayout.BeginHorizontal();
        var rect = GUILayoutUtility.GetRect(new GUIContent("Search VFXData"), EditorStyles.toolbarSearchField);
        if (GUI.Button(rect, new GUIContent("Search VFXData"), EditorStyles.toolbarSearchField))
        {
            var dropdown = new VFXDataDropdown(new AdvancedDropdownState(), this);
            dropdown.Show(rect);
        }
        EditorGUILayout.EndHorizontal();
        if (_selection != -1)
        {
            if (GUILayout.Button("Clear search"))
            {
                _selection = -1;
            }
        }

        if (_selection == -1)
        {
            DrawList(_vfxLibrary.VFXData);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_vfxData"), true);
        }
        else
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_vfxData").GetArrayElementAtIndex(_selection), true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawList(List<VFXData> data)
    {
        for (int i = 0; i < data.Count; i++)
        {
            data[i].Name = string.IsNullOrEmpty(data[i].VFXKey) ? "Provide VFXKey" : data[i].VFXKey;
            data[i].Name += data[i].VFXObject ? $" - {data[i].VFXObject.name}" : " - Provide VFXObject";
        }
    }

    #region SearchbarDrawer

    private class VFXDataDropdown : AdvancedDropdown
    {
        private VFXLibraryEditor _vfxLibraryEditor;
        public VFXDataDropdown(AdvancedDropdownState state, VFXLibraryEditor vfxLibraryEditor) : base(state)
        {
            _vfxLibraryEditor = vfxLibraryEditor;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("VFXData");
            root.AddChild(new VFXDataDropdownItem("All", null));
            for (int i = 0; i < _vfxLibraryEditor._vfxLibrary.VFXData.Count; i++)
            {
                var key = $"{_vfxLibraryEditor._vfxLibrary.VFXData[i].Name}";
                root.AddChild(new VFXDataDropdownItem(key, _vfxLibraryEditor._vfxLibrary.VFXData[i]));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            var vfxData = (VFXDataDropdownItem)item;
            if (vfxData.VFXData == null)
            {
                _vfxLibraryEditor._selection = -1;
            }
            else
            {
                for (int i = 0; i < _vfxLibraryEditor._vfxLibrary.VFXData.Count; i++)
                {
                    if (_vfxLibraryEditor._vfxLibrary.VFXData[i] == vfxData.VFXData)
                    {
                        _vfxLibraryEditor._selection = i;
                        break;
                    }
                }
            }
        }

        private class VFXDataDropdownItem : AdvancedDropdownItem
        {
            public readonly VFXData VFXData;

            public VFXDataDropdownItem(string name, VFXData data) : base(name)
            {
                this.VFXData = data;
            }
        }
    }

    #endregion
}

