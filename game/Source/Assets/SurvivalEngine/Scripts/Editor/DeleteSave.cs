using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace SurvivalEngine.EditorTool
{
    /// <summary>
    /// Check if can apply any automatic fixes to issues that could be caused from changing asset version
    /// </summary>

    public class DeleteSave : ScriptableWizard
    {
        [MenuItem("Survival Engine/Delete Save File", priority = 350)]
        static void ScriptableWizardMenu()
        {
            ScriptableWizard.DisplayWizard<DeleteSave>("Delete Save File", "Delete");
        }

        void OnWizardCreate()
        {
            PlayerData.Delete(PlayerData.GetLastSave());
        }

        void OnWizardUpdate()
        {
            helpString = "Use this tool to delete the latest save file.";
        }
    }
}