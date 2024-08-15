using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Add this script to a contruction to turn it into a craft station
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    public class CraftStation : MonoBehaviour
    {
        public GroupData[] craft_groups;
        public float range = 3f;

        private Selectable select;
        private Buildable buildable; //Can be null

        private static List<CraftStation> station_list = new List<CraftStation>();

        void Awake()
        {
            station_list.Add(this);
            select = GetComponent<Selectable>();
            buildable = GetComponent<Buildable>();
            select.onUse += OnUse;
        }

        private void OnDestroy()
        {
            station_list.Remove(this);
        }

        private void OnUse(PlayerCharacter character)
        {
            CraftPanel panel = CraftPanel.Get(character.player_id);
            if (panel != null && !panel.IsVisible())
                panel.Show();
        }

        public bool HasCrafting()
        {
            return craft_groups.Length > 0;
        }

        public static CraftStation GetNearestInRange(Vector3 pos)
        {
            float min_dist = 99f;
            CraftStation nearest = null;
            foreach (CraftStation station in station_list)
            {
                if (station.buildable == null || !station.buildable.IsBuilding())
                {
                    float dist = (pos - station.transform.position).magnitude;
                    if (dist < min_dist && dist < station.range)
                    {
                        min_dist = dist;
                        nearest = station;
                    }
                }
            }
            return nearest;
        }

        public static List<CraftStation> GetAll()
        {
            return station_list;
        }
    }

}
