using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Generates a grass mesh in a square shape
    /// </summary>

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class GrassMesh : MonoBehaviour
    {
        public float width = 1f;
        public float height = 1f;
        public float spacing = 1f;

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

            if (width < 0.01f || height < 0.01f || spacing < 0.01f)
                return m;

            int nbw = Mathf.RoundToInt(width / spacing) + 1;
            int nbh = Mathf.RoundToInt(height / spacing) + 1;
            Vector3[] verticles = new Vector3[nbh * nbh];
            Vector3[] normals = new Vector3[nbh * nbh];
            Vector4[] tangents = new Vector4[nbh * nbh];
            Vector2[] uvs = new Vector2[nbh * nbh];
            int nb_tri = (nbw - 1) * (nbh - 1) * 6;
            int[] triangles = new int[nb_tri];

            Vector3 normal = Vector3.up;
            Vector4 tangent = new Vector4(-1f, 0f, 0f, -1f);

            float offsetX = width / 2f;
            float offsetY = height / 2f;
            float posX = 0f;
            float posY = 0f;
            int index = 0;
            for (int y = 0; y < nbh; y++)
            {
                posX = 0f;
                for (int x = 0; x < nbw; x++)
                {
                    verticles[index] = new Vector3(posX - offsetX, 0f, posY - offsetY);
                    normals[index] = normal;
                    tangents[index] = tangent;
                    uvs[index] = new Vector2(posX / (float)width, posY / (float)height);
                    posX += spacing;
                    index++;
                }
                posY += spacing;
            }

            index = 0;
            for (int y = 0; y < nbh - 1; y++)
            {
                for (int x = 0; x < nbw - 1; x++)
                {
                    int vindex = y * nbw + x;
                    triangles[index + 0] = vindex + 0;
                    triangles[index + 1] = vindex + 1 + nbw;
                    triangles[index + 2] = vindex + 1;
                    triangles[index + 3] = vindex + 0;
                    triangles[index + 4] = vindex + nbw;
                    triangles[index + 5] = vindex + 1 + nbw;
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
