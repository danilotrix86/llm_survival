using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Put this script in each scene with a list of possible weathers in that scene
    /// </summary>

    public class WeatherSystem : MonoBehaviour
    {
        [Header("Weather")]
        public WeatherData default_weather;
        public WeatherData[] weathers;

        [Header("Weather Group")]
        public string group; //Scenes with the same group will have synchronized weather

        [Header("Weather Settings")]
        public float weather_change_time = 6f; //Time of the day the weather changes
        
        private WeatherData current_weather;
        private GameObject current_weather_fx;
        private float update_timer = 0f;

        private static WeatherSystem instance;

        private void Awake()
        {
            instance = this;
            current_weather = null;
            if (default_weather == null)
                enabled = false;
        }

        void Start()
        {
            if (PlayerData.Get().HasCustomString("weather_" + group))
            {
                string weather_id = PlayerData.Get().GetCustomString("weather_" + group);
                ChangeWeather(GetWeather(weather_id));
            }
            else
            {
                ChangeWeather(default_weather);
            }
        }

        void Update()
        {
            update_timer += Time.deltaTime;
            if (update_timer > 1f)
            {
                update_timer = 0f;
                SlowUpdate();
            }
        }

        void SlowUpdate()
        {
            //Check if new day
            int day = PlayerData.Get().day;
            float time = PlayerData.Get().day_time;
            int prev_day = PlayerData.Get().GetCustomInt("weather_day_" + group);
            if (day > prev_day && time >= weather_change_time)
            {
                ChangeWeatherRandom();
                PlayerData.Get().SetCustomInt("weather_day_" + group, day);
            }
        }

        public void ChangeWeatherRandom()
        {
            if (weathers.Length > 0)
            {
                float total = 0f;
                foreach (WeatherData aweather in weathers)
                {
                    total += aweather.probability;
                }

                float value = Random.Range(0f, total);
                WeatherData weather = null;
                foreach (WeatherData aweather in weathers)
                {
                    if (weather == null && value < aweather.probability)
                        weather = aweather;
                    else
                        value -= aweather.probability;
                }

                if (weather == null)
                    weather = default_weather;

                ChangeWeather(weather);
            }
        }

        public void ChangeWeather(WeatherData weather)
        {
            if (weather != null && current_weather != weather)
            {
                current_weather = weather;
                PlayerData.Get().SetCustomString("weather_" + group, weather.id);
                if (current_weather_fx != null)
                    Destroy(current_weather_fx);
                if (current_weather.weather_fx != null)
                    current_weather_fx = Instantiate(current_weather.weather_fx, TheCamera.Get().GetTargetPos(), Quaternion.identity);
            }
        }

        public WeatherData GetWeather(string id)
        {
            foreach (WeatherData weather in weathers)
            {
                if (weather.id == id)
                    return weather;
            }
            return null;
        }

        public float GetLightMult()
        {
            if (current_weather != null)
                return current_weather.light_mult;
            return 1f;
        }

        public WeatherEffect GetWeatherEffect()
        {
            if (current_weather != null)
                return current_weather.effect;
            return WeatherEffect.None;
        }

        public bool HasWeatherEffect(WeatherEffect effect)
        {
            if(current_weather != null)
                return current_weather.effect == effect;
            return false;
        }

        public static WeatherSystem Get()
        {
            return instance;
        }
    }
}
