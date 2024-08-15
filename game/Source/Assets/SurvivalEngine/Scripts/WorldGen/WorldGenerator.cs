using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace SurvivalEngine.WorldGen
{
    public enum WorldGeneratorMode
    {
        Editor=0,
        Runtime=10,
    }

    /// <summary>
    /// Generator of random world
    /// </summary>

    public class WorldGenerator : MonoBehaviour
    {
        public WorldGeneratorMode mode;

        [Header("World Generator")]
        public int seed = 0; //Randomizer
        public float map_size = 200f;
        public float nb_zones = 100;

		[SerializeField] bool determBiomes = false;
        
        [Header("Biomes")]
        public BiomeData[] biomes;

        [Header("Objects Generator")]
        public int iterations = 1000; //How long it will try to place objects 

        [Header("References")]
        public Material walls_mat;
        public SAction water_action;
        public GroupData water_group;

        [Header("Saved Values (Automatic)")]
        public GameObject world;
        public BiomeZone[] zones;
        public Transform[] points;
        public GameObject[] walls;

        public UnityAction afterWorldGen;

        private Dictionary<Vector3, GameObject> points_link = new Dictionary<Vector3, GameObject>();

        private static WorldGenerator _instance;
        
        private void Start()
        {
            if (mode == WorldGeneratorMode.Runtime && Application.isPlaying)
            {
                TheGame.Get().PauseScripts();
                BlackPanel.Get().Show(true);
                GeneratedOrLoadWorld();
                BlackPanel.Get().Hide();
                TheGame.Get().UnpauseScripts();
            }
        }

        //Either generate new world, or load previously generated
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

        public void GenerateRandomWorld(int seed)
        {
            this.seed = seed;
            ClearWorld();
            GenerateZones();
            GenerateAllTerrain();
            GenerateWalls();
            GenerateAllBiomesObjects();
            GenerateAllUID();
            GenerateNavmesh();
            SaveWorld();

            if (afterWorldGen != null)
                afterWorldGen.Invoke();
        }

        public void ClearWorld()
        {
            zones = new BiomeZone[0];
            points = new Transform[0];
            points_link.Clear();
            for(int i= transform.childCount-1; i>=0; i--)
            {
                Transform child = transform.GetChild(i);
                DestroyImmediate(child.gameObject);
            }

            if (world != null)
                DestroyImmediate(world);
            world = null;
        }
        
        public void GenerateZones()
        {
            ClearWorld();

            //Generate random numbers with a seed
            Random.InitState(seed);

            Voronoi.Generate(map_size, nb_zones);

            List<Transform> all_points = new List<Transform>();
            List<BiomeZone> all_zones = new List<BiomeZone>();

            world = new GameObject("World");

            points_link.Clear();
			int i = 0;
            foreach (VoronoiCell cell in Voronoi.cells) {

                if (cell.borderCoordinates.Count >= 3)
                {
                    BiomeZone zone = AddZone(world, cell.sitePos);
                    zone.data = determBiomes ? GetDeterministicBiome(i++) : GetRandomBiome();
                    zone.name = zone.data.id;
                    zone.iterations = iterations;
                    zone.seed = Random.Range(int.MinValue, int.MaxValue);
                    List<Transform> zone_points = new List<Transform>();
                    foreach (Vector3 edge in cell.borderCoordinates)
                    {
                        GameObject point = AddEdgePoint(gameObject, edge);
                        zone_points.Add(point.transform);
                        zone.points = zone_points.ToArray();

                        if (!all_points.Contains(point.transform))
                            all_points.Add(point.transform);
                    }

                    all_zones.Add(zone);
                }
            }

            zones = all_zones.ToArray();
            points = all_points.ToArray();

            FixStartingZone();
        }

        private void FixStartingZone()
        {
            List<BiomeData> starting_zones = new List<BiomeData>();
            foreach (BiomeData bdata in biomes)
            {
                if (bdata.starting_zone)
                    starting_zones.Add(bdata);
            }

            //Random start zone
            BiomeData start_biome = null; 
            if(starting_zones.Count > 0)
                start_biome = starting_zones[Random.Range(0, starting_zones.Count)];

            //Find closest zone
            PlayerCharacter player = PlayerCharacter.GetFirst();
            Vector3 player_pos = player ? player.transform.position : Vector3.zero;
            float min_dist = 999f;
            BiomeZone nearest = null;
            foreach (BiomeZone zone in zones)
            {
                float dist = (zone.transform.position - player_pos).magnitude;
                if (dist < min_dist)
                {
                    nearest = zone;
                    min_dist = dist;
                }
            }

            if (nearest != null && !nearest.data.starting_zone && start_biome != null)
            {
                nearest.data = start_biome;
                nearest.name = start_biome.id;
            }
        }

        public void ClearTerrain()
        {
            foreach (BiomeZone zone in zones)
            {
                zone.ClearTerrain();
            }

            foreach (GameObject wall in walls)
            {
                DestroyImmediate(wall);
            }
            walls = new GameObject[0];
        }

        public void GenerateAllTerrain()
        {
            if (!AreZonesGenerated())
                return;

            foreach (BiomeZone zone in zones)
            {
                zone.iterations = iterations;
                zone.GenerateTerrain();
            }
        }

        public void GenerateWalls()
        {
            if (!AreZonesGenerated())
                return;

            foreach (GameObject wall in walls)
                DestroyImmediate(wall);

            GameObject w1 = GenerateWall(new Vector3(map_size, 0f, 0f), 20f, 20f, map_size * 2f + 40f);
            GameObject w2 = GenerateWall(new Vector3(-map_size, 0f, 0f), 20f, 20f, map_size * 2f + 40f);
            GameObject w3 = GenerateWall(new Vector3(0f, 0f, map_size), map_size * 2f, 20f, 20f);
            GameObject w4 = GenerateWall(new Vector3(0f, 0f, -map_size), map_size * 2f, 20f, 20f);
            walls = new GameObject[] { w1, w2, w3, w4 };
        }

        public void ClearAllBiomes()
        {
            foreach (BiomeZone zone in zones)
            {
                zone.ClearBiomeObjects();
            }
        }

        public void GenerateAllBiomesObjects()
        {
            ClearAllBiomes();

            int nb = zones.Length;
            int index = 0;
            foreach (BiomeZone zone in zones)
            {
#if UNITY_EDITOR
                if (Application.isEditor && !Application.isPlaying)
                    UnityEditor.EditorUtility.DisplayProgressBar("World Generator", "Generating Biome Objects", index / (float) nb);
#endif
                zone.iterations = iterations;
                zone.GenerateBiomeObjects();
                index++;
            }

#if UNITY_EDITOR
            if (Application.isEditor && !Application.isPlaying)
                UnityEditor.EditorUtility.ClearProgressBar();
#endif
        }

        public void GenerateAllUID()
        {
            foreach (BiomeZone zone in zones)
            {
                zone.GenerateBiomeUID();
            }
        }

        public void GenerateNavmesh()
        {
            NavMeshSurface surface = FindObjectOfType<NavMeshSurface>();
            if (surface == null)
            {
                GameObject sobj = new GameObject("NavMesh");
                sobj.transform.SetParent(world.transform);
                surface = sobj.AddComponent<NavMeshSurface>();
                surface.ignoreTriggers = true;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.staticOnly = true;
            }

            if(surface != null)
                surface.BuildNavMesh();
        }

        public bool AreZonesGenerated()
        {
            return zones.Length > 0 && world != null;
        }

        private GameObject GenerateWall(Vector3 edge_pos, float sizeX, float sizeY, float sizeZ)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "wall";
            wall.isStatic = true;
            wall.transform.SetParent(world.transform);

            Vector3 dir = edge_pos.normalized;
            Vector3 pos = edge_pos += new Vector3(dir.x * sizeX * 0.5f, 0f, dir.z * sizeZ * 0.5f);
            wall.transform.position = pos;
            wall.transform.localScale = new Vector3(sizeX, sizeY, sizeZ);

            MeshRenderer render = wall.GetComponent<MeshRenderer>();
            if(walls_mat != null)
                render.material = walls_mat;

            return wall;
        }

		private BiomeData GetDeterministicBiome(int i)
		{
			int index = i % biomes.Length;
			return biomes[i++];
		}

        private BiomeData GetRandomBiome()
        {
            float biome_total_prob = 0f;
            foreach (BiomeData biome in biomes)
                biome_total_prob += biome.probability;

            float value = Random.Range(0f, biome_total_prob);
            foreach (BiomeData biome in biomes)
            {
                if (value < biome.probability)
                {
                    return biome;
                }
                else
                {
                    value -= biome.probability;
                }
            }
            return null;
        }

        private BiomeZone AddZone(GameObject parent, Vector3 center)
        {
            GameObject point = new GameObject("zone");
            point.transform.SetParent(parent.transform);
            point.transform.position = center;
            BiomeZone zone = point.AddComponent<BiomeZone>();
            point.AddComponent<LLMBiome>();
            return zone;
        }

        private GameObject AddEdgePoint(GameObject parent, Vector3 pos) {
            if (points_link.ContainsKey(pos))
            {
                return points_link[pos];
            }
            else
            {
                GameObject point = new GameObject("edge");
                point.transform.SetParent(parent.transform);
                point.transform.position = pos;
                points_link[pos] = point;
                WorldGenTool.SetIcon(point, 0);
                return point;
            }
        }

        public void SaveWorld()
        {
            PlayerData.Get().world_seed = seed;
        }

        public static WorldGenerator Get()
        {
            if (_instance == null)
                _instance = FindObjectOfType<WorldGenerator>();
            return _instance;
        }

    }

}