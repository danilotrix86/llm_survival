using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace SurvivalEngine.EditorTool
{

    /// <summary>
    /// Tool to replace all selected objects in the scene by a prefab. Much faster than doing it one by one
    /// </summary>

    public class ReplacePrefab : ScriptableWizard
    {
        public GameObject NewPrefab;

        [MenuItem("Survival Engine/Replace Prefab", priority = 304)]
        static void ScriptableWizardMenu()
        {
            ScriptableWizard.DisplayWizard<ReplacePrefab>("Replace Prefabs", "Replace Prefabs");
        }

        void OnWizardCreate()
        {
            if (NewPrefab != null)
            {
                List<GameObject> newObjs = new List<GameObject>();

                foreach (Transform transform in Selection.transforms)
                {
                    GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(NewPrefab);
                    Undo.RegisterCreatedObjectUndo(newObject, "created prefab");

                    newObject.transform.position = transform.position;
                    newObject.transform.rotation = transform.rotation;
                    newObject.transform.localScale = transform.localScale;
                    newObject.transform.parent = transform.parent;
                    newObjs.Add(newObject);

                    Undo.DestroyObjectImmediate(transform.gameObject);
                }

                Selection.objects = newObjs.ToArray();
            }
        }

        void OnWizardUpdate()
        {
            helpString = "Use this tool to replace all selected objects in the scene by a prefab.";
        }
    }

}