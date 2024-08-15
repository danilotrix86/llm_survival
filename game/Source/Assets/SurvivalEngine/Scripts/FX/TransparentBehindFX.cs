using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Add to an object to make it transparent when behind
    /// </summary>
    
    public class TransparentBehindFX : MonoBehaviour
    {
        public float opacity = 0.5f;
        public float distance = 5f;
        public float refresh_rate = 0.25f;

        private Selectable select;
        private MeshRenderer[] renders;
        private float timer = 0f;

        private List<Material> materials = new List<Material>();
        private List<Material> materials_transparent = new List<Material>();

        private static List<TransparentBehindFX> see_list = new List<TransparentBehindFX>();

        void Awake()
        {
            see_list.Add(this);
            select = GetComponent<Selectable>();
            renders = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer render in renders)
            {
                foreach (Material material in render.sharedMaterials)
                {
                    bool valid_mat = material && MaterialTool.HasColor(material);
                    Material material_normal = valid_mat ? new Material(material) : null;
                    Material material_trans = valid_mat ? new Material(material) : null;
                    if (material_trans != null && valid_mat)
                    {
                        material_trans.color = new Color(material_trans.color.r, material_trans.color.g, material_trans.color.b, material_trans.color.a * opacity);
                        MaterialTool.ChangeRenderMode(material_trans, BlendMode.Fade);
                    }
                    materials.Add(material_normal);
                    materials_transparent.Add(material_trans);
                }
            }
        }

        private void OnDestroy()
        {
            see_list.Remove(this);
        }

        void Update()
        {
            if (select && !select.IsActive())
                return;

            timer += Time.deltaTime;

            if (timer > refresh_rate)
            {
                timer = 0f;
                UpdateSlow();
            }
        }

        private void UpdateSlow()
        {
            Vector3 pos = TheCamera.Get().GetTargetPos();
            Vector3 cam_dir = TheCamera.Get().GetFacingFront();
            Vector3 obj_dir = transform.position - pos;
            bool is_behind = Vector3.Dot(obj_dir.normalized, cam_dir) < 0f;
            bool is_near = (transform.position - pos).magnitude < distance;
            SetMaterial(is_behind && is_near);
        }

        private void SetMaterial(bool transparent)
        {
            int index = 0;
            foreach (MeshRenderer render in renders)
            {
                Material[] mesh_materials = render.sharedMaterials;
                for (int i = 0; i < mesh_materials.Length; i++)
                {
                    if (index < materials.Count && index < materials_transparent.Count)
                    {
                        Material mesh_mat = mesh_materials[i];
                        Material ref_mat = transparent ? materials_transparent[index] : materials[index];
                        if (ref_mat != mesh_mat && ref_mat != null)
                            mesh_materials[i] = ref_mat;
                    }
                    index++;
                }
                render.sharedMaterials = mesh_materials;
            }
        }

        public static List<TransparentBehindFX> GetAll()
        {
            return see_list;
        }
    }

}
