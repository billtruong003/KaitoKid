using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShmackleFingerBender))]
public class ShmackleFingerBenderEditor : Editor
{
    ShmackleFingerBender bender;
    void OnEnable() {
        bender = target as ShmackleFingerBender;
    }
    
    public override void OnInspectorGUI() {
        EditorUtility.SetDirty(bender);

        DrawDefaultInspector();
        EditorGUILayout.Space();
        if(bender.Hand != null) {
            if(bender.bendOffsets.Length != bender.Hand.fingers.Length)
                bender.bendOffsets = new float[bender.Hand.fingers.Length];
            for(int i = 0; i < bender.Hand.fingers.Length; i++) {
                var layout = EditorGUILayout.GetControlRect();
                layout.width /= 2;
                var text = new GUIContent(bender.Hand.fingers[i].name + " Offset", "0 is no bend, 0.5 is half bend, 1 is full bend, -1 to stiffen finger from sway");
                EditorGUI.LabelField(layout, text);
                layout.x += layout.width;
                bender.bendOffsets[i] = EditorGUI.FloatField(layout, bender.bendOffsets[i]);
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
}
