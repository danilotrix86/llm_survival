using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Generic physics functions
    /// </summary>

    public class PhysicsTool
    {
        //Detect if object is grounded, the ground normal, and ground distance from root
        public static bool DetectGround(Transform root, Vector3 center, float hdist, float radius, LayerMask ground_layer, out float ground_distance, out Vector3 ground_normal)
        {
            Vector3 p1 = center;
            Vector3 p2 = center + Vector3.left * radius;
            Vector3 p3 = center + Vector3.right * radius;
            Vector3 p4 = center + Vector3.forward * radius;
            Vector3 p5 = center + Vector3.back * radius;

            RaycastHit h1, h2, h3, h4, h5, hd;
            bool f1 = Physics.Raycast(p1, Vector3.down, out h1, hdist, ground_layer.value, QueryTriggerInteraction.Ignore);
            bool f2 = Physics.Raycast(p2, Vector3.down, out h2, hdist, ground_layer.value, QueryTriggerInteraction.Ignore);
            bool f3 = Physics.Raycast(p3, Vector3.down, out h3, hdist, ground_layer.value, QueryTriggerInteraction.Ignore);
            bool f4 = Physics.Raycast(p4, Vector3.down, out h4, hdist, ground_layer.value, QueryTriggerInteraction.Ignore);
            bool f5 = Physics.Raycast(p5, Vector3.down, out h5, hdist, ground_layer.value, QueryTriggerInteraction.Ignore);
            f1 = f1 && h1.collider.transform != root && !h1.collider.transform.IsChildOf(root);
            f2 = f2 && h2.collider.transform != root && !h2.collider.transform.IsChildOf(root);
            f3 = f3 && h3.collider.transform != root && !h3.collider.transform.IsChildOf(root);
            f4 = f4 && h4.collider.transform != root && !h4.collider.transform.IsChildOf(root);
            f5 = f5 && h5.collider.transform != root && !h5.collider.transform.IsChildOf(root);

            bool is_grounded = f1 || f2 || f3 || f4 || f5;
            ground_normal = Vector3.up;
            ground_distance = 0f;

            //Find ground distance, add extra longer raycast in case not enough are found (like on edge of slope)
            bool fd = Physics.Raycast(p1, Vector3.down, out hd, 1f + hdist, ground_layer.value, QueryTriggerInteraction.Ignore);
            if (is_grounded)
            {
                Vector3 hit_center = Vector3.zero;
                int nb = 0;
                if (f1) { hit_center += h1.point; nb++; }
                if (f2) { hit_center += h2.point; nb++; }
                if (f3) { hit_center += h3.point; nb++; }
                if (f4) { hit_center += h4.point; nb++; }
                if (f5) { hit_center += h5.point; nb++; }
                if (fd) { hit_center += hd.point; nb++; }
                hit_center = hit_center / nb;
                ground_distance = (hit_center - root.position).y;
            }

            //Find ground normal
            if (is_grounded)
            {
                Vector3 normal = Vector3.zero;
                int nb = 0;
                if (f1) { normal += FlipNormalUp(h1.normal); nb++; }
                if (f2) { normal += FlipNormalUp(h2.normal); nb++; }
                if (f3) { normal += FlipNormalUp(h3.normal); nb++; }
                if (f4) { normal += FlipNormalUp(h4.normal); nb++; }
                if (f5) { normal += FlipNormalUp(h5.normal); nb++; }
                if (fd) { normal += FlipNormalUp(hd.normal); nb++; }
                normal = normal / nb;
                ground_normal = normal.normalized;
            }

            //Debug.DrawRay(p1, Vector3.down * hdist);
            //Debug.DrawRay(p2, Vector3.down * hdist);
            //Debug.DrawRay(p3, Vector3.down * hdist);
            //Debug.DrawRay(p4, Vector3.down * hdist);
            //Debug.DrawRay(p5, Vector3.down * hdist);

            return is_grounded;
        }

        public static int[] DetectGroundLayers(Vector3 center, float hdist)
        {
            Vector3 p1 = center;
            RaycastHit[] hits = Physics.RaycastAll(p1, Vector3.down, hdist, ~0, QueryTriggerInteraction.Ignore);
            int[] layers = new int[hits.Length];

            for (int i = 0; i < hits.Length; i++)
                layers[i] = hits[i].collider.gameObject.layer;

            return layers;
        }

        public static bool FindGroundPosition(Vector3 pos, float max_y, out Vector3 ground_pos)
        {
            return FindGroundPosition(pos, max_y, ~0, out ground_pos); //All layers
        }

        //Find the elevation of the ground at position (within max_y dist)
        public static bool FindGroundPosition(Vector3 pos, float max_y, LayerMask ground_layer, out Vector3 ground_pos)
        {
            Vector3 start_pos = pos + Vector3.up * max_y;
            RaycastHit rhit;
            bool is_hit = Physics.Raycast(start_pos, Vector3.down, out rhit, max_y * 2f, ~0, QueryTriggerInteraction.Ignore);
            bool is_in_right_layer = is_hit && rhit.collider != null && IsLayerInLayerMask(rhit.collider.gameObject.layer, ground_layer.value);
            ground_pos = rhit.point;
            return is_hit && is_in_right_layer;
        }

        public static Vector3 FlipNormalUp(Vector3 normal)
        {
            if (normal.y < 0f)
                return -normal; //Face up
            return normal;
        }

        public static bool RaycastCollisionLayer(Vector3 pos, Vector3 dir, LayerMask layer, out RaycastHit hit) {
            //Debug.DrawRay(pos, dir);
            return Physics.Raycast(pos, dir.normalized, out hit, dir.magnitude, layer.value, QueryTriggerInteraction.Ignore);
        }

        public static bool RaycastCollision(Vector3 pos, Vector3 dir, out RaycastHit hit){
            //Debug.DrawRay(pos, dir);
            return Physics.Raycast(pos, dir.normalized, out hit, dir.magnitude, ~0, QueryTriggerInteraction.Ignore);
        }

        public static bool IsLayerInLayerMask(int layer, LayerMask mask)
        {
            return (LayerToLayerMask(layer).value & mask.value) > 0;
        }

        public static bool IsAnyLayerInLayerMask(int[] layers, LayerMask mask)
        {
            bool is_in_layer = false;
            for (int i = 0; i < layers.Length; i++)
                is_in_layer = is_in_layer || IsLayerInLayerMask(layers[i], mask);
            return is_in_layer;
        }

        public static LayerMask LayerToLayerMask(int layer)
        {
            return (LayerMask) 1 << layer;
        }

        public static List<int> LayerMaskToLayers(LayerMask mask)
        {
            uint bits = (uint)mask.value;
            List<int> layers = new List<int>();
            for (int i = 31; bits > 0; i--)
            {
                if ((bits >> i) > 0)
                {
                    bits = (bits << (32 - i)) >> (32 - i);
                    layers.Add(i);
                }
            }
            return layers;
        }
    }

}
