using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Firepits can be fueled with wood or other materials. Will be lit until it run out of fuel
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Construction))]
    public class Firepit : MonoBehaviour
    {
        public GroupData fire_group;
        public GameObject fire_fx;
        public GameObject fuel_model;

        public float start_fuel = 10f;
        public float max_fuel = 50f;
        public float fuel_per_hour = 1f; //In Game hours
        public float wood_add_fuel = 2f;

        private Selectable select;
        private Construction construction;
        private Buildable buildable;
        private UniqueID unique_id;
        private HeatSource heat_source;

        private bool is_on = false;
        private float fuel = 0f;

        private static List<Firepit> firepit_list = new List<Firepit>();

        void Awake()
        {
            firepit_list.Add(this);
            select = GetComponent<Selectable>();
            construction = GetComponent<Construction>();
            buildable = GetComponent<Buildable>();
            unique_id = GetComponent<UniqueID>();
            heat_source = GetComponent<HeatSource>();
            if (fire_fx)
                fire_fx.SetActive(false);
            if (fuel_model)
                fuel_model.SetActive(false);
        }

        private void OnDestroy()
        {
            firepit_list.Remove(this);
        }

        private void Start()
        {
            //select.onUse += OnUse;
            select.RemoveGroup(fire_group);
            buildable.onBuild += OnBuild;

            if (!construction.was_spawned && !buildable.IsBuilding())
                fuel = start_fuel;
            if (PlayerData.Get().HasCustomFloat(GetFireUID()))
                fuel = PlayerData.Get().GetCustomFloat(GetFireUID());
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (is_on)
            {
                float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();
                fuel -= fuel_per_hour * game_speed * Time.deltaTime;

                PlayerData.Get().SetCustomFloat(GetFireUID(), fuel);
            }

            is_on = fuel > 0f;
            if (fire_fx)
                fire_fx.SetActive(is_on);
            if (fuel_model)
                fuel_model.SetActive(fuel > 0f);

            if (is_on)  
                select.AddGroup(fire_group);
            else
                select.RemoveGroup(fire_group);

            if (heat_source != null)
                heat_source.enabled = is_on;
        }

        public void AddFuel(float value)
        {
            fuel += value;
            is_on = fuel > 0f;

            PlayerData.Get().SetCustomFloat(GetFireUID(), fuel);
        }

        private void OnBuild()
        {
            fuel = start_fuel;
        }

        public string GetFireUID()
        {
            if(!string.IsNullOrEmpty(unique_id.unique_id))
                return unique_id.unique_id + "_fire";
            return "";
        }

        public bool IsOn()
        {
            return is_on;
        }

        public static Firepit GetNearest(Vector3 pos, float range=999f)
        {
            float min_dist = range;
            Firepit nearest = null;
            foreach (Firepit fire in firepit_list)
            {
                float dist = (pos - fire.transform.position).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = fire;
                }
            }
            return nearest;
        }

        public static List<Firepit> GetAll()
        {
            return firepit_list;
        }
    }

}
