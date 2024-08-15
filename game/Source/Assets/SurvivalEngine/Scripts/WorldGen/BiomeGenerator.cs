using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine.WorldGen
{
    /// <summary>
    /// Use this script if you want to generate biomes objects ONLY and not generate the terrain.
    /// </summary>

    [ExecuteInEditMode]
    public class BiomeGenerator : MonoBehaviour
    {
        public BiomeData data;

        [Header("Biome Generator")]
        public WorldGeneratorMode mode;
        public Collider zone;
        public int seed;
        public int iterations = 1000;
        public LayerMask floor_layer = (1 << 9);

        private int original_seed = 0;
        private int world_seed = 0;

        private List<GameObject> spawned_items = new List<GameObject>();
        private List<GameObject> spawned_items_group = new List<GameObject>();
        private Dictionary<GameObject, float> group_size = new Dictionary<GameObject, float>();
        private Dictionary<GameObject, float> collider_size = new Dictionary<GameObject, float>();

        private void Awake()
        {
            original_seed = seed;
        }

        void Start()
        {
            if (mode == WorldGeneratorMode.Runtime && Application.isPlaying)
            {
                TheGame.Get().PauseScripts();
                BlackPanel.Get().Show(true);
                GeneratedOrLoadWorld();
                BlackPanel.Get().Hide();
                TheGame.Get().UnpauseScripts();
            }

            if (Application.isPlaying)
                zone.enabled = false;
        }

        public void GeneratedOrLoadWorld()
        {
            if (!PlayerData.Get().IsWorldGenerated())
            {
                GenerateRandomWorld();
            }
            else
            {
                GenerateRandomWorld(PlayerData.Get().world_seed);
            }
        }

        //Call this from script to generate the whole world
        public void GenerateRandomWorld()
        {
            GenerateRandomWorld(Random.Range(int.MinValue, int.MaxValue));
        }

        public void GenerateRandomWorld(int wseed)
        {
            world_seed = wseed;
            seed = original_seed + wseed;
            ClearBiomeObjects();
            GenerateBiomeObjects();
            GenerateBiomeUID();
            SaveWorld();
        }

        public void ClearBiomeObjects()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if(zone != null && child != zone.transform)
                    DestroyImmediate(child.gameObject);
            }

            spawned_items.Clear();
            spawned_items_group.Clear();
            group_size.Clear();
            collider_size.Clear();
        }

        public void GenerateBiomeObjects()
        {
            ClearBiomeObjects();

            int index = 0;
            foreach (BiomeSpawnData group in data.spawns)
            {
                SpawnBiomeGroup(group, index);
                index++;
            }
        }

        public void SpawnBiomeGroup(BiomeSpawnData data, int index)
        {
            Random.InitState(seed + index); //Each group should have its own seed, so if one group change the other is not affected
            spawned_items_group.Clear();

            float area_size = zone.bounds.size.x * zone.bounds.size.z;
            float density_dist = (150f - data.variance) / data.density; //Density determine minimum distance between each object of same group
            int spawn_max = Mathf.RoundToInt(data.density * area_size / (10f * data.variance)); //Determine max number of objects

            GameObject parent = new GameObject(data.name);
            parent.transform.SetParent(transform);
            parent.transform.localPosition = Vector3.zero;

            Vector3 min = zone.bounds.min;
            Vector3 max = zone.bounds.max;

            for (int i = 0; i < iterations; i++)
            {

                if (spawned_items_group.Count > spawn_max)
                    return;

                Vector3 pos = new Vector3(Random.Range(min.x, max.x), zone.bounds.center.y, Random.Range(min.z, max.z));
                bool found = PhysicsTool.FindGroundPosition(pos, zone.bounds.extents.y, floor_layer, out pos);

                if (found && IsInsideZone(pos))
                {
                    GameObject prefab = data.PickRandomPrefab();
                    if (prefab != null)
                    {

                        WorldGenObject properties = prefab.GetComponent<WorldGenObject>();
                        float gsize = (properties != null) ? properties.size_group : 0.25f; //Group size
                        float csize = (properties != null) ? properties.size : 0.25f; //Colliding size

                        if (!IsNearOther(pos, csize) && IsFitDensity(pos, density_dist, gsize))
                        {

                            bool is_valid;
                            if (properties != null && properties.type == WorldGenObjectType.AvoidEdge)
                                is_valid = !IsNearEdge(pos, properties.edge_dist);
                            else if (properties != null && properties.type == WorldGenObjectType.NearEdge)
                                is_valid = IsNearEdge(pos, properties.edge_dist) && !IsNearEdge(pos, csize);
                            else
                                is_valid = !IsNearEdge(pos, csize);

                            if (is_valid)
                            {
                                GameObject nobj = InstantiatePrefab(prefab, parent.transform);
                                nobj.transform.position = pos;
                                if (data.random_rotation)
                                    nobj.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                                if (data.random_scale > 0.01f)
                                    nobj.transform.localScale = Vector3.one * (1f + Random.Range(-data.random_scale, data.random_scale));

                                if (properties == null)
                                {
                                    Collider collide = nobj.GetComponentInChildren<Collider>();
                                    csize = collide != null ? collide.bounds.extents.magnitude : 0.25f;
                                }

                                spawned_items.Add(nobj);
                                spawned_items_group.Add(nobj);
                                group_size[nobj] = gsize;
                                collider_size[nobj] = csize;
                            }
                        }
                    }
                }
            }
        }

        private GameObject InstantiatePrefab(GameObject prefab, Transform parent)
        {
            GameObject nobj;
#if UNITY_EDITOR
            if (Application.isEditor && !Application.isPlaying)
                nobj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
            else
#endif
                nobj = Instantiate(prefab, parent);
            return nobj;
        }

        public void GenerateBiomeUID()
        {
            UniqueID[] all_uids = GetComponentsInChildren<UniqueID>();
            UniqueID.ClearAll(all_uids);

            Random.InitState(seed);
            UniqueID.GenerateAll(all_uids);
        }

        public void SaveWorld()
        {
            PlayerData.Get().world_seed = world_seed;
        }

        public bool AreObjectsGenerated()
        {
            return transform.childCount > 1;
        }

        //In world space
        private bool IsInsideZone(Vector3 pos)
        {
            return (zone.ClosestPoint(pos) - pos).magnitude < Mathf.Epsilon;
        }

        private bool IsNearEdge(Vector3 pos, float size)
        {
            if (size < 0.01f)
                return false;

            Vector3 closest = zone.ClosestPointOnBounds(pos);
            Vector3 dir = closest - pos;
            dir.y = 0f;
            Vector3 point = pos + dir.normalized * size;
            return !IsInsideZone(point);
        }

        private bool IsNearOther(Vector3 pos, float size)
        {
            bool too_close = false;
            foreach (GameObject item in spawned_items)
            {
                float dist = (item.transform.position - pos).magnitude;
                float other_size = collider_size[item];
                too_close = dist < (other_size + size);
                if (too_close)
                    return too_close;
            }
            return too_close;
        }

        private bool IsFitDensity(Vector3 pos, float density_dist, float size)
        {
            bool fit_density;
            foreach (GameObject item in spawned_items_group)
            {
                float dist = (item.transform.position - pos).magnitude;
                float other_size = group_size[item];
                fit_density = dist > density_dist && dist > (other_size + size);
                if (!fit_density)
                    return false;
            }
            return true;
        }
    }

}