using UnityEditor;
using UnityEngine;
using NaughtyAttributes;
namespace Shmackle.SoundMaterial
{
    [CustomEditor(typeof(MaterialSoundSystem))]
    public class MaterialSoundSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            MaterialSoundSystem soundSystem = (MaterialSoundSystem)target;
            if (soundSystem.soundDatabase != null)
            {
                if (GUILayout.Button("Go to Sound Database"))
                {
                    EditorGUIUtility.PingObject(soundSystem.soundDatabase);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Material Sound Database is not assigned.", MessageType.Warning);
            }
        }
    }
}