using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine.WorldGen
{

    [CreateAssetMenu(fileName = "Biome", menuName = "SurvivalEngine/WorldGen/Biome", order = 100)]
    public class BiomeData : ScriptableObject
    {
        public string id;
        public float probability;
        public bool starting_zone;

        [Header("LLMInfo")]
        public string name;
        public string desc;
        public bool discoverable = true;

        [Header("Terrain")]
        public Material floor_material;
        public float elevation = 0f;
        public bool is_water;

        [Header("Spawns")]
        public BiomeSpawnData[] spawns;


    }

}