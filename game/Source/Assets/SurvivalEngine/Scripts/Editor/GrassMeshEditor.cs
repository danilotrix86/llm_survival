using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SurvivalEngine.EditorTool
{
    /// <summary>
    /// Editor script for GrassMesh
    /// </summary>

    [CustomEditor(typeof(GrassMesh)), CanEditMultipleObjects]
    public class GrassMeshEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            GrassMesh myScript = target as GrassMesh;

            DrawDefaultInspector();

            if (GUILayout.Button("Refresh Now"))
            {
                myScript.RefreshMesh();
            }

            EditorGUILayout.Space();
        }

    }

}
