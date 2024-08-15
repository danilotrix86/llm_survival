using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine.WorldGen
{
    [ExecuteInEditMode]
    public class BiomeZone : MonoBehaviour
    {
        public BiomeData data;

        [Header("Biome Generator")]
        public int seed;
        public int iterations = 1000;

        [Header("Saved Values (Automatic)")]
        public Transform[] points;
        public GameObject floor;

        private List<GameObject> spawned_items = new List<GameObject>();
        private List<GameObject> spawned_items_group = new List<GameObject>();
        private Dictionary<GameObject, float> group_size = new Dictionary<GameObject, float>();
        private Dictionary<GameObject, float> collider_size = new Dictionary<GameObject, float>();

        private void Start()
        {
            //Add code to do at start

        }

        public void ClearTerrain()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                DestroyImmediate(child.gameObject);
            }
            floor = null;
        }

        public void GenerateTerrain()
        {
            if (AreObjectsGenerated())
                return;

            ClearTerrain();

            gameObject.name = data.id;
            floor = new GameObject("floor");
            floor.isStatic = true;
            floor.transform.SetParent(transform);
            floor.transform.position = transform.position;

            MeshRenderer render = floor.AddComponent<MeshRenderer>();
            MeshFilter mesh = floor.AddComponent<MeshFilter>();
            render.material = data.floor_material;
            floor.layer = 9; //Floor layer

            mesh.sharedMesh = new Mesh();
            CreateFloorMesh(mesh.sharedMesh);
            MeshCollider collide = floor.AddComponent<MeshCollider>();
            collide.convex = true;

            if (data.is_water)
            {
                GenerateWater();
            }
        }

        private void GenerateWater()
        {
            floor.layer = 4; //Water layer

            //Water collider
            GameObject fcollider = new GameObject("water-collider");
            fcollider.transform.SetParent(floor.transform);
            fcollider.transform.position = floor.transform.position;
            fcollider.isStatic = true;
            fcollider.layer = 14; //Water wall layer

            MeshRenderer crender = fcollider.AddComponent<MeshRenderer>();
            MeshFilter cmesh = fcollider.AddComponent<MeshFilter>();
            crender.enabled = false;

            cmesh.sharedMesh = new Mesh();
            CreateFloorMesh(cmesh.sharedMesh, 5f);
            MeshCollider ccollide = fcollider.AddComponent<MeshCollider>();
            ccollide.convex = true;

            //Drink Selectable
            GameObject fselect = new GameObject("water-drink");
            fselect.transform.SetParent(floor.transform);
            fselect.transform.position = floor.transform.position;
            fselect.layer = floor.layer;

            MeshRenderer srender = fselect.AddComponent<MeshRenderer>();
            MeshFilter smesh = fselect.AddComponent<MeshFilter>();
            srender.enabled = false;

            smesh.sharedMesh = new Mesh();
            CreateFloorMesh(smesh.sharedMesh, 1f, -1f);
            MeshCollider scollide = fselect.AddComponent<MeshCollider>();
            scollide.convex = true;
            scollide.isTrigger = true;

            Selectable selectable = fselect.AddComponent<Selectable>();
            selectable.type = SelectableType.InteractSurface;

            if (WorldGenerator.Get())
            {
                SAction action = WorldGenerator.Get().water_action;
                selectable.actions = new SAction[] { action };
                GroupData group = WorldGenerator.Get().water_group;
                selectable.groups = new GroupData[] { group };
            }
        }

        public void ClearBiomeObjects()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if(child.gameObject != floor)
                    DestroyImmediate(child.gameObject);
            }

            spawned_items.Clear();
            spawned_items_group.Clear();
            group_size.Clear();
            collider_size.Clear();
        }

        public void GenerateBiomeObjects()
        {
            if (!IsTerrainGenerated())
                return;

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

            float area_size = WorldGenTool.AreaSizePolygon(points);
            float density_dist = (150f - data.variance) / data.density; //Density determine minimum distance between each object of same group
            int spawn_max = Mathf.RoundToInt(data.density * area_size / (10f * data.variance)); //Determine max number of objects

            GameObject parent = new GameObject(data.name);
            parent.transform.SetParent(transform);
            parent.transform.localPosition = Vector3.zero;

            Vector3 min = WorldGenTool.GetPolygonMin(points);
            Vector3 max = WorldGenTool.GetPolygonMax(points);

            for (int i=0; i < iterations; i++){

                if (spawned_items_group.Count > spawn_max)
                    return;

                Vector3 pos = new Vector3(Random.Range(min.x, max.x), this.data.elevation, Random.Range(min.z, max.z));

                if (IsInsideZone(pos))
                {
                    GameObject prefab = data.PickRandomPrefab();
                    if (prefab != null) {

                        WorldGenObject properties = prefab.GetComponent<WorldGenObject>();
                        float gsize = (properties != null) ? properties.size_group : 0.25f; //Group size
                        float csize = (properties != null) ? properties.size : 0.25f; //Colliding size

                        if (!IsNearOther(pos, csize) && IsFitDensity(pos, density_dist, gsize)) {

                            bool is_valid;
                            if (properties != null && properties.type == WorldGenObjectType.AvoidEdge)
                                is_valid = !IsNearPolyEdge(pos, properties.edge_dist);
                            else if (properties != null && properties.type == WorldGenObjectType.NearEdge)
                                is_valid = IsNearPolyEdge(pos, properties.edge_dist) && !IsNearPolyEdge(pos, csize);
                            else
                                is_valid = !IsNearPolyEdge(pos, csize);

                            if (is_valid)
                            {
                                GameObject nobj = InstantiatePrefab(prefab, parent.transform);
                                nobj.transform.position = pos;
                                if(data.random_rotation)
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

        public bool IsTerrainGenerated()
        {
            return floor != null;
        }

        public bool AreObjectsGenerated()
        {
            return transform.childCount > 1;
        }

        //In world space
        private bool IsInsideZone(Vector3 pos)
        {
            return WorldGenTool.IsPointInPolygon(pos, points);
        }

        private bool IsNearPolyEdge(Vector3 pos, float size)
        {
            if (size < 0.01f)
                return false;

            for (int i=0; i<points.Length-1; i++)
            {
                if (WorldGenTool.GetEdgeDist(pos, points[i].position, points[i + 1].position) < size)
                    return true;
            }

            if (WorldGenTool.GetEdgeDist(pos, points[points.Length - 1].position, points[0].position) < size)
                return true;

            return false;
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

        private void CreateFloorMesh(Mesh aMesh, float offset=0f, float bottom=-10f)
        {
            AddMeshFace(aMesh, Vector3.up, data.elevation + offset, true);
            AddMeshFace(aMesh, Vector3.down, bottom, true);
            AddMeshEdge(aMesh);
            aMesh.RecalculateBounds();
        }

        private void AddMeshEdge(Mesh aMesh)
        {
            List<Vector3> vertices = new List<Vector3>(aMesh.vertices);
            List<Vector3> normals = new List<Vector3>(aMesh.normals);
            List<Vector2> uvs = new List<Vector2>(aMesh.uv);
            List<int> triangles = new List<int>(aMesh.triangles);
            Vector3 center = FindZoneCenter();
            int nb_vertices = aMesh.vertices.Length;
            int half = nb_vertices / 2 - 1;

            for (int j = 0; j < points.Length; j++)
            {
                int itop = j + 1;
                int ibottom = j + half + 2;
                int itopnext = j + 2;
                int ibottomnext = j + half + 3;

                if (j == points.Length - 1)
                {
                    itopnext = 1;
                    ibottomnext = half + 2;
                }

                Vector3 v1 = aMesh.vertices[itop];
                Vector3 v2 = aMesh.vertices[ibottom];
                Vector3 v3 = aMesh.vertices[itopnext];
                Vector3 v4 = aMesh.vertices[ibottomnext];
                Vector3 normal1 = (v1 - center);
                Vector3 normal2 = (v3 - center);
                normal1.y = 0f; normal2.y = 0f;
                Vector3 normal = (normal1.normalized + normal2.normalized).normalized;

                vertices.Add(v1); //Top
                vertices.Add(v2); //Bottom
                vertices.Add(v3); //Top next
                vertices.Add(v4); //bottom next
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                uvs.Add(FindUVEdge(v1));
                uvs.Add(FindUVEdge(v2));
                uvs.Add(FindUVEdge(v3));
                uvs.Add(FindUVEdge(v4));

                triangles.Add(vertices.Count - 4); //Top
                triangles.Add(vertices.Count - 3); //Bottom
                triangles.Add(vertices.Count - 1); //Bottom next
                triangles.Add(vertices.Count - 4); //Top
                triangles.Add(vertices.Count - 1); //Bottom next
                triangles.Add(vertices.Count - 2); //Top next
            }

            aMesh.vertices = vertices.ToArray();
            aMesh.triangles = triangles.ToArray();
            aMesh.normals = normals.ToArray();
            aMesh.uv = uvs.ToArray();
        }

        private void AddMeshFace(Mesh aMesh, Vector3 normal, float elevation, bool local)
        {
            List<Vector3> vertices = new List<Vector3>(aMesh.vertices);
            List<Vector3> normals = new List<Vector3>(aMesh.normals);
            List<Vector2> uvs = new List<Vector2>(aMesh.uv);
            List<int> triangles = new List<int>(aMesh.triangles);
            int nb_vertices = vertices.Count;

            Vector3 center = FindZoneCenter();
            if(local)
                center -= transform.position;
            center.y = elevation;

            vertices.Add(center);
            normals.Add(normal);
            uvs.Add(FindUV(center));

            for (int j = 0; j < points.Length; j++)
            {
                Vector3 point = points[j].position;
                if (local)
                    point -= transform.position;
                point.y = elevation;

                vertices.Add(point);
                normals.Add(normal);
                uvs.Add(FindUV(point));

                if (normal.y > 0f)
                {
                    if (j == points.Length - 1)
                    {
                        triangles.Add(nb_vertices);
                        triangles.Add(vertices.Count - 1);
                        triangles.Add(nb_vertices + 1);
                        
                    }
                    else
                    {
                        triangles.Add(nb_vertices);
                        triangles.Add(vertices.Count - 1);
                        triangles.Add(vertices.Count);
                    }
                }
                else
                {
                    if (j == points.Length - 1)
                    {
                        triangles.Add(nb_vertices);
                        triangles.Add(nb_vertices + 1);
                        triangles.Add(vertices.Count - 1);
                        
                    }
                    else
                    {
                        triangles.Add(nb_vertices);
                        triangles.Add(vertices.Count);
                        triangles.Add(vertices.Count - 1);
                    }
                }
            }

            aMesh.vertices = vertices.ToArray();
            aMesh.triangles = triangles.ToArray();
            aMesh.normals =  normals.ToArray();
            aMesh.uv =  uvs.ToArray();

            //aMesh.RecalculateNormals();
        }

        private Vector2 FindUV(Vector3 pos)
        {
            return new Vector2(pos.x, pos.z);
        }

        private Vector2 FindUVEdge(Vector3 pos)
        {
            return new Vector2((pos.x + pos.z), pos.y);
        }

        private Vector3 FindZoneCenter()
        {
            Vector3 center = Vector3.zero;
            for (int j = 0; j < points.Length; j++)
            {
                center += points[j].position;
            }
            center = center / points.Length;
            return center;
        }

        private void OnDrawGizmos()
        {
            if (IsTerrainGenerated())
                return;

            //Display the voronoi diagram
            Color random_color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            Gizmos.color = MaterialTool.HasColor(data.floor_material) ? data.floor_material.color : random_color;

            Mesh triangleMesh = new Mesh();
            AddMeshFace(triangleMesh, Vector3.up, 0f, false);

            Gizmos.DrawMesh(triangleMesh);

            //Display the sites
            //Gizmos.color = Color.white;
            //Gizmos.DrawSphere(center.transform.position, 0.2f);
        }
    }

}