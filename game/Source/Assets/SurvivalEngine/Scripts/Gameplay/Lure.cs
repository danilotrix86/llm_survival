using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// A lure that will attract nearby animals
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    public class Lure : MonoBehaviour
    {
        public float range = 10f;

        private Selectable selectable;

        private static List<Lure> lure_list = new List<Lure>();

        void Awake()
        {
            lure_list.Add(this);
            selectable = GetComponent<Selectable>();
        }

        private void OnDestroy()
        {
            lure_list.Remove(this);
        }

        public void Kill()
        {
            selectable.Destroy();
        }

        public static Lure GetNearestInRange(Vector3 pos)
        {
            Lure nearest = null;
            float min_dist = 999f;
            foreach (Lure lure in lure_list)
            {
                float dist = (lure.transform.position - pos).magnitude;
                if (dist < min_dist && dist < lure.range)
                {
                    min_dist = dist;
                    nearest = lure;
                }
            }
            return nearest;
        }

        public static Lure GetNearest(Vector3 pos, float range = 999f)
        {
            Lure nearest = null;
            float min_dist = range;
            foreach (Lure lure in lure_list)
            {
                float dist = (lure.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = lure;
                }
            }
            return nearest;
        }

        public static List<Lure> GetAll()
        {
            return lure_list;
        }
    }

}
