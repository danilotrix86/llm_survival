using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine 
{
    /// <summary>
    /// SData is the base scriptable object data for this engine
    /// </summary>
    [System.Serializable]
    public abstract class SData : ScriptableObject { }

    /// <summary>
    /// IdData adds an ID to the class
    /// </summary>
    [System.Serializable]
    public abstract class IdData : SData { public string id; }

    /// <summary>
    /// This is a generic spawn data to spawn any generic prefabs that are not Constructions, Items, Plants or Characters
    /// Spawn() is called automatically during loading to respawn everything that was saved, use Create() to create a new object
    /// </summary>

    [CreateAssetMenu(fileName = "SpawnData", menuName = "SurvivalEngine/SpawnData", order = 5)]
    public class SpawnData : IdData
    {
        public GameObject prefab;

        private static List<SpawnData> spawn_data = new List<SpawnData>();

        public static void Load(string folder = "")
        {
            spawn_data.Clear();
            spawn_data.AddRange(Resources.LoadAll<SpawnData>(folder));
        }

        public static SpawnData Get(string id)
        {
            foreach (SpawnData data in spawn_data)
            {
                if (data.id == id)
                    return data;
            }
            return null;
        }

        public static List<SpawnData> GetAll()
        {
            return spawn_data;
        }
    }
}


