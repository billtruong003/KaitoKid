using UnityEditor;
using UnityEngine;
namespace Shmackle.SoundMaterial
{
    [CustomEditor(typeof(MaterialSoundDatabase))]
    public class MaterialSoundDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MaterialSoundDatabase database = (MaterialSoundDatabase)target;
            if (GUILayout.Button("Add New Mapping"))
            {
                ArrayUtility.Add(ref database.materialSoundMappings, new MaterialSoundDatabase.MaterialSoundMapping());
                EditorUtility.SetDirty(database);
            }

            if (database.materialSoundMappings != null)
            {
                for (int i = 0; i < database.materialSoundMappings.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Mapping {i + 1}");
                    if (GUILayout.Button("Remove"))
                    {
                        ArrayUtility.RemoveAt(ref database.materialSoundMappings, i);
                        EditorUtility.SetDirty(database);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}