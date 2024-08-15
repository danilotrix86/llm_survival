using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Generates a grass mesh in a circle shape
    /// </summary>

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class GrassCircle : MonoBehaviour
    {
        public float radius = 1f;
        public float spacing = 1f;
        public int precision = 10;

        //public Transform[] paths;

        private MeshRenderer render;
        private MeshFilter mesh;

        void Awake()
        {
            mesh = GetComponent<MeshFilter>();
            render = gameObject.GetComponent<MeshRenderer>();
            RefreshMesh();
        }

        Mesh CreateMesh()
        {
            Mesh m = new Mesh();
            m.name = "GrassMesh";

            if (precision < 1 || radius < 0.01f || spacing < 0.01f)
                return m;

            int nbstep = Mathf.Max(Mathf.RoundToInt(radius / spacing), 1);
            int nbang = precision + 1;
            Vector3[] verticles = new Vector3[nbstep * nbang + 1];
            Vector3[] normals = new Vector3[nbstep * nbang + 1];
            Vector4[] tangents = new Vector4[nbstep * nbang + 1];
            Vector2[] uvs = new Vector2[nbstep * nbang + 1];
            int nb_tri = (nbstep - 1) * precision * 6 + precision * 3;
            int[] triangles = new int[nb_tri];

            Vector3 normal = Vector3.up;
            Vector4 tangent = new Vector4(-1f, 0f, 0f, -1f);

            //Center
            verticles[0] = Vector3.zero;
            normals[0] = normal;
            tangents[0] = tangent;
            uvs[0] = new Vector2(0.5f, 0.5f);

            int index = 1;
            for (int a = 0; a < nbang; a++)
            {
                float angle = (a * 360f / (float)precision) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                for (int x = 0; x < nbstep; x++)
                {
                    float dist = ((x + 1) / (float)nbstep) * radius;
                    verticles[index] = dir * dist;
                    normals[index] = normal;
                    tangents[index] = tangent;
                    uvs[index] = new Vector2(Mathf.Clamp01(dir.x * dist * 0.5f / radius + 0.5f), Mathf.Clamp01(dir.z * dist * 0.5f / radius + 0.5f));
                    index++;
                }
            }

            index = 0;
            for (int a = 0; a < nbang - 1; a++)
            {
                int vindex = a * nbstep + 1;
                triangles[index + 0] = 0; //Center
                triangles[index + 1] = vindex + nbstep;
                triangles[index + 2] = vindex;
                index += 3;

                for (int x = 0; x < nbstep - 1; x++)
                {
                    vindex = a * nbstep + x + 1;
                    triangles[index + 0] = vindex;
                    triangles[index + 1] = vindex + nbstep;
                    triangles[index + 2] = vindex + 1;
                    triangles[index + 3] = vindex + nbstep;
                    triangles[index + 4] = vindex + nbstep + 1;
                    triangles[index + 5] = vindex + 1;
                    index += 6;
                }
            }

            m.vertices = verticles;
            m.normals = normals;
            m.tangents = tangents;
            m.uv = uvs;
            m.triangles = triangles;

            return m;
        }

        public void RefreshMesh()
        {
            mesh.mesh = CreateMesh();
        }
    }

}