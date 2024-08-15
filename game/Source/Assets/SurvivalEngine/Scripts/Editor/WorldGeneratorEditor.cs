using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurvivalEngine.WorldGen.EditorTool
{

    /// <summary>
    /// Just the text on the WorldGenerator component in Unity inspector
    /// </summary>

    [CustomEditor(typeof(WorldGenerator))]
    public class WorldGeneratorEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            WorldGenerator myScript = target as WorldGenerator;

            DrawDefaultInspector();

            EditorGUILayout.Space();

            GUIStyle title_style = new GUIStyle();
            title_style.fontSize = 14;
            title_style.fontStyle = FontStyle.Bold;

            GUIStyle text_style = new GUIStyle();
            text_style.fontSize = 12;
            text_style.fontStyle = FontStyle.Normal;

            if (myScript.mode == WorldGeneratorMode.Runtime)
            {
                if (GUILayout.Button("Clear World"))
                {
                    myScript.ClearWorld();
                    EditorUtility.SetDirty(myScript);
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
                return;
            }

            EditorGUILayout.LabelField("World Zones Generator", title_style);

            if (GUILayout.Button("Clear World"))
            {
                myScript.ClearWorld();
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            if (GUILayout.Button("Generate Zones"))
            {
                myScript.GenerateZones();
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Terrain Generator", title_style);

            if (GUILayout.Button("Clear All Terrain"))
            {
                myScript.ClearTerrain();
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            if (GUILayout.Button("Generate All Terrain"))
            {
                myScript.GenerateAllTerrain();
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            if (GUILayout.Button("Generate Walls"))
            {
                myScript.GenerateWalls();
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Biomes Objects Generator", title_style);

            if (GUILayout.Button("Clear All Biome Objects"))
            {
                myScript.ClearAllBiomes();
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            if (GUILayout.Button("Generate All Biome Objects"))
            {
                myScript.GenerateAllBiomesObjects();
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Finalizing", title_style);

            if (GUILayout.Button("Generate All UIDs"))
            {
                myScript.GenerateAllUID();
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            if (GUILayout.Button("Generate Navmesh"))
            {
                myScript.GenerateNavmesh();
                EditorUtility.SetDirty(myScript);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

    }

}
