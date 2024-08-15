using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Spot that can be digged with a shovel, will gain loots from the destructible
    /// </summary>

    [RequireComponent(typeof(Destructible))]
    public class DigSpot : MonoBehaviour
    {
        private Destructible destruct;

        private static List<DigSpot> dig_list = new List<DigSpot>();

        void Awake()
        {
            dig_list.Add(this);
            destruct = GetComponent<Destructible>();
        }

        void OnDestroy()
        {
            dig_list.Remove(this);
        }

        public void Dig()
        {
            destruct.Kill();
        }

        public static DigSpot GetNearest(Vector3 pos, float range = 999f)
        {
            DigSpot nearest = null;
            float min_dist = range;
            foreach (DigSpot spot in dig_list)
            {
                float dist = (spot.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = spot;
                }
            }
            return nearest;
        }

        public static List<DigSpot> GetAll()
        {
            return dig_list;
        }
    }

}