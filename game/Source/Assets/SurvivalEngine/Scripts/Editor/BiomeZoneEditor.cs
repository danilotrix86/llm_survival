using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurvivalEngine.WorldGen.EditorTool
{

    /// <summary>
    /// Just the text on the BiomeZone component in Unity inspector
    /// </summary>

    [CustomEditor(typeof(BiomeZone)), CanEditMultipleObjects]
    public class BiomeZoneEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            BiomeZone myScript = target as BiomeZone;

            DrawDefaultInspector();

            EditorGUILayout.Space();

            GUIStyle title_style = new GUIStyle();
            title_style.fontSize = 14;
            title_style.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField("Terrain Generator", title_style);

            if (GUILayout.Button("Clear Biome"))
            {
                myScript.ClearTerrain();
                
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            if (GUILayout.Button("Generate Biome Terrain"))
            {
                myScript.GenerateTerrain();
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            EditorGUILayout.LabelField("Objects Generator", title_style);

            if (GUILayout.Button("Clear Biome Objects"))
            {
                myScript.ClearBiomeObjects();
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            if (GUILayout.Button("Generate Biome Objects"))
            {
                myScript.GenerateBiomeObjects();
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            EditorGUILayout.LabelField("Finalizing", title_style);

            if (GUILayout.Button("Generate Biome UIDs"))
            {
                myScript.GenerateBiomeUID();
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

    }

}
