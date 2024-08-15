using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace SurvivalEngine
{
    [RequireComponent(typeof(UniqueID))]
    public class Zone : MonoBehaviour
    {
        [HideInInspector]
        public UnityAction<PlayerCharacter> onEnter;
        [HideInInspector]
        public UnityAction<PlayerCharacter> onExit;
        
        private Collider collide;
        private Bounds bounds;
        private UniqueID unique_id;

        private static List<Zone> zone_list = new List<Zone>();

        private void Awake()
        {
            zone_list.Add(this);
            unique_id = GetComponent<UniqueID>();
            collide = GetComponent<Collider>();
            if (collide != null)
                bounds = collide.bounds;
        }
        
        private void OnDestroy()
        {
            zone_list.Remove(this);
        }

        private void OnEnter(PlayerCharacter player)
        {
            onEnter?.Invoke(player);
        }

        private void OnExit(PlayerCharacter player)
        {
            onExit?.Invoke(player);
        }

        void OnTriggerEnter(Collider coll)
        {
            PlayerCharacter player = coll.GetComponent<PlayerCharacter>();
            OnEnter(player);
        }

        void OnTriggerExit(Collider coll)
        {
            PlayerCharacter player = coll.GetComponent<PlayerCharacter>();
            OnExit(player);
        }

        public Vector3 PickRandomPosition()
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            float z = Random.Range(bounds.min.z, bounds.max.z);
            return new Vector3(x, y, z);
        }

        public bool IsInside(Vector3 position)
        {
            return (position.x > bounds.min.x && position.x < bounds.max.x 
                && position.y > bounds.min.y && position.y < bounds.max.y
                && position.z > bounds.min.z && position.z < bounds.max.z);
        }

        public bool IsInsideXZ(Vector3 position)
        {
            return (position.x > bounds.min.x && position.z > bounds.min.z && position.x < bounds.max.x && position.z < bounds.max.z);
        }

        public static Zone GetNearest(Vector3 pos, float range =999f)
        {
            float min_dist = range;
            Zone nearest = null;
            foreach (Zone zone in zone_list)
            {
                float dist = (pos - zone.transform.position).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = zone;
                }
            }
            return nearest;
        }

        public static Zone Get(string uid)
        {
            foreach (Zone zone in zone_list)
            {
                if (zone.unique_id.unique_id == uid)
                {
                    return zone;
                }
            }
            return null;
        }

        public static List<Zone> GetAll()
        {
            return zone_list;
        }
    }

}