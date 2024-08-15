using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Spawns random prefabs in the scene at an interval.
    /// </summary>

    public class Spawner : MonoBehaviour
    {
        public float spawn_interval = 8f; //In game hours
        public float spawn_radius = 10f; //Circle radius of the spawn zone, keep it big enough so it can keep track of the already spawned ones.
        public int spawn_max = 1; //If there are more than this already in the radius, will stop spawning.
        public float spawn_max_radius = 10f; //If there are more than this already in this radius, will stop spawning.
        public LayerMask valid_floor_layer = (1 << 9); //Floor that this can be spawned on
        public CraftData[] spawn_data; //The objects to spawn

        private float spawn_timer = 0f;
        private UniqueID unique_id;

        void Awake()
        {
            unique_id = GetComponent<UniqueID>();
        }

        private void Start()
        {
            if (PlayerData.Get().HasCustomFloat(GetTimerUID()))
                spawn_timer = PlayerData.Get().GetCustomFloat(GetTimerUID());
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();
            spawn_timer += game_speed * Time.deltaTime;

            PlayerData.Get().SetCustomFloat(GetTimerUID(), spawn_timer);

            if (spawn_timer > spawn_interval)
            {
                spawn_timer = 0f;
                SpawnIfNotMax();
            }
        }

        public void SpawnIfNotMax()
        {
            if (!IsFull())
            {
                Spawn();
            }
        }

        public void Spawn()
        {
            CraftData data = spawn_data[Random.Range(0, spawn_data.Length)];
            if (data != null)
            {
                float radius = Random.Range(0f, spawn_radius);
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Vector3 pos = transform.position + offset;
                Vector3 ground_pos;
                bool found = PhysicsTool.FindGroundPosition(pos, 100f, valid_floor_layer.value, out ground_pos);
                if (found)
                {
                    CraftData.Create(data, ground_pos);
                }
            }
        }

        public bool IsFull()
        {
            return CountObjectsInRange() >= spawn_max;
        }

        public int CountObjectsInRange()
        {
            int count = 0;
            foreach (CraftData data in spawn_data)
            {
                count += CraftData.CountObjectInRadius(data, transform.position, spawn_max_radius);
            }
            return count;
        }

        public string GetTimerUID()
        {
            if (unique_id != null && !string.IsNullOrEmpty(unique_id.unique_id))
                return unique_id.unique_id + "_timer";
            return "";
        }
    }

}