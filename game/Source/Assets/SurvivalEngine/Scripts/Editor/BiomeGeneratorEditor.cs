using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurvivalEngine.WorldGen.EditorTool
{

    /// <summary>
    /// Just the text on the BiomeGenerator component in Unity inspector
    /// </summary>

    [CustomEditor(typeof(BiomeGenerator)), CanEditMultipleObjects]
    public class BiomeGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            BiomeGenerator myScript = target as BiomeGenerator;

            DrawDefaultInspector();

            EditorGUILayout.Space();

            GUIStyle title_style = new GUIStyle();
            title_style.fontSize = 14;
            title_style.fontStyle = FontStyle.Bold;

            if (myScript.mode == WorldGeneratorMode.Runtime)
            {
                if (GUILayout.Button("Clear World"))
                {
                    myScript.ClearBiomeObjects();
                    EditorUtility.SetDirty(myScript);
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
                return;
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
