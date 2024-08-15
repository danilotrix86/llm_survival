using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SurvivalEngine.EditorTool
{

    /// <summary>
    /// Just the text on the UniqueID component in Unity inspector
    /// </summary>

    [CustomEditor(typeof(UniqueID)), CanEditMultipleObjects]
    public class UniqueIDEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            UniqueID myScript = target as UniqueID;

            DrawDefaultInspector();

            EditorGUILayout.Space();

            GUIStyle style = new GUIStyle();
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("Generate Unique ID", style);
            EditorGUILayout.LabelField("Click on Survival Engine->Generate UIDs to generate\nall empty UIDs in the scene.\nOr click below to generate this one.", GUILayout.Height(50));

            if (GUILayout.Button("Generate UID"))
            {
                Undo.RecordObject(myScript, "Generate UID");
                myScript.GenerateUIDEditor();
                EditorUtility.SetDirty(myScript);
            }

            EditorGUILayout.Space();
        }

    }

}
