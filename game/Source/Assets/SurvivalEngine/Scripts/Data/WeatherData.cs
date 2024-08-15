using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    public enum WeatherEffect
    {
        None=0,
        Rain=10,
    }

    [CreateAssetMenu(fileName ="Weather", menuName = "SurvivalEngine/Weather", order =10)]
    public class WeatherData : ScriptableObject
    {
        public string id;
        public float probability = 1f;

        [Header("Gameplay")]
        public WeatherEffect effect;

        [Header("Visuals")]
        public GameObject weather_fx;
        public float light_mult = 1f;
    }
}
