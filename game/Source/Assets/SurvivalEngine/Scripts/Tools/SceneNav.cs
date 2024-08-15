using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivalEngine
{

    //Script to manage transitions between scenes
    public class SceneNav
    {
        public static void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public static void GoTo(string scene)
        {
            SceneManager.LoadScene(scene);
        }

        public static string GetCurrentScene()
        {
            return SceneManager.GetActiveScene().name;
        }

        public static bool DoSceneExist(string scene)
        {
            return Application.CanStreamedLevelBeLoaded(scene);
        }
    }

}