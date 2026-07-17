using System.Collections.Generic;
using Stratton.Audio;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(AudioLibrary))]
public class AudioLibraryEditor : Editor
{
    private AudioLibrary _audioLibrary;
    private int _selection = -1;

    public override void OnInspectorGUI()
    {
        _audioLibrary = (AudioLibrary)target;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("_poolSize"));

        EditorGUILayout.BeginHorizontal();
        var rect = GUILayoutUtility.GetRect(new GUIContent("Search AudioData"), EditorStyles.toolbarSearchField);
        if (GUI.Button(rect, new GUIContent("Search AudioData"), EditorStyles.toolbarSearchField))
        {
            var dropdown = new AudioDataDropdown(new AdvancedDropdownState(), this);
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
            DrawList(_audioLibrary.AudioData);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_audioData"), true);
        }
        else
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_audioData").GetArrayElementAtIndex(_selection), true);
        }
        
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawList(List<AudioData> data)
    {
        for (int i = 0; i < data.Count; i++)
        {
            data[i].Name = string.IsNullOrEmpty(data[i].AudioKey) ? "Provide AudioKey" : data[i].AudioKey;
            data[i].Name += data[i].AudioClip ? $" - {data[i].AudioClip.name}" : " - Provide AudioClip";
            data[i].Name += string.IsNullOrEmpty(data[i].AudioGroupName) ? "" : $" - {data[i].AudioGroupName}";
        }
    }

    #region SearchbarDrawer

    private class AudioDataDropdown : AdvancedDropdown
    {
        private AudioLibraryEditor _audioLibraryEditor;
        public AudioDataDropdown(AdvancedDropdownState state, AudioLibraryEditor audioLibraryEditor) : base(state)
        {
            _audioLibraryEditor = audioLibraryEditor;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("AudioData");
            root.AddChild(new AudioDataDropdownItem("All", null));
            for (int i = 0; i < _audioLibraryEditor._audioLibrary.AudioData.Count; i++)
            {
                var key = $"{_audioLibraryEditor._audioLibrary.AudioData[i].Name}";
                root.AddChild(new AudioDataDropdownItem(key, _audioLibraryEditor._audioLibrary.AudioData[i]));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            var audioData = (AudioDataDropdownItem)item;
            if (audioData.AudioData == null)
            {
                _audioLibraryEditor._selection = -1;
            }
            else
            {
                for (int i = 0; i < _audioLibraryEditor._audioLibrary.AudioData.Count; i++)
                {
                    if (_audioLibraryEditor._audioLibrary.AudioData[i] == audioData.AudioData)
                    {
                        _audioLibraryEditor._selection = i;
                        break;
                    }
                }
            }
        }

        private class AudioDataDropdownItem : AdvancedDropdownItem
        {
            public readonly AudioData AudioData;

            public AudioDataDropdownItem(string name, AudioData data) : base(name)
            {
                this.AudioData = data;
            }
        }
    }

    #endregion
}
