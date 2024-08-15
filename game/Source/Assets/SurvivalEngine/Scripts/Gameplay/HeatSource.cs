using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Add this script to an object that emits heat (like fire) or an object that emits cold (ice)
    /// </summary>

    public class HeatSource : MonoBehaviour
    {
        public float heat = 50f; //What temperature this heat is, use positive value for heat, and negative value for cold
        public float heat_weight = 1f; //Weight of this heat
        public float heat_range = 5f; //How far from the source this heat is counted

        private static List<HeatSource> heat_list = new List<HeatSource>();

        void Awake()
        {
            heat_list.Add(this);
        }

        void OnDestroy()
        {
            heat_list.Remove(this);
        }

        public static HeatSource GetNearest(Vector3 pos, float range=999f)
        {
            HeatSource nearest = null;
            float min_dist = range;
            foreach (HeatSource heat in heat_list)
            {
                float dist = (heat.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = heat;
                }
            }
            return nearest;
        }

        public static List<HeatSource> GetAll()
        {
            return heat_list;
        }
    }

}
