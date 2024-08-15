using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Generic game data (only one file)
    /// </summary>

    [CreateAssetMenu(fileName = "GameData", menuName = "SurvivalEngine/GameData", order = 0)]
    public class GameData : ScriptableObject
    {
        [Header("Game")]
        public float game_time_mult = 24f; //A value of 1 means time follows real life time. Value of 24 means 1 hour of real time will be one day in game
        public float start_day_time = 6f; //Time at start of day
        public float end_day_time = 20f; //Time at end of day

        [Header("Day/Night")]
        public float day_light_dir_intensity = 1f; //Directional light at day
        public float day_light_ambient_intensity = 1f;  //Ambient light at day
        public float night_light_dir_intensity = 0.2f; //Directional light at night
        public float night_light_ambient_intensity = 0.5f; //Ambient light at night
        public float light_update_speed = 0.2f;
        public bool rotate_shadows = true; //Will rotate shadows during the day as if sun is rotating

        [Header("Optimization")]
        public float optim_refresh_rate = 0.5f; //In seconds, interval at which selectable are shown/hidden
        public float optim_distance_multiplier = 1f; //will make all selectable active_range multiplied
        public float optim_facing_offset = 10f; //active area will be offset by X in the direction the camera is facing
        public bool optim_turn_off_gameobjects = false; //If on, will turn off the whole gameObjects, otherwise will just turn off scripts

        public static GameData Get()
        {
            return TheData.Get().data;
        }
    }

}
