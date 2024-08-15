using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SurvivalEngine.EditorTool
{
    /// <summary>
    /// Editor script for GrassCircle
    /// </summary>

    [CustomEditor(typeof(GrassCircle)), CanEditMultipleObjects]
    public class GrassCircleEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            GrassCircle myScript = target as GrassCircle;

            DrawDefaultInspector();

            if (GUILayout.Button("Refresh Now"))
            {
                myScript.RefreshMesh();
            }

            EditorGUILayout.Space();
        }

    }

}
