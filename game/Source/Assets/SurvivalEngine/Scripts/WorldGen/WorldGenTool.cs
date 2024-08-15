using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SurvivalEngine.WorldGen
{
    /// <summary>
    /// Helper tools for World Gen
    /// </summary>

    public class WorldGenTool
    {
        //Assign Icon to editor object
        public static void SetIcon(GameObject obj, int index)
        {
#if UNITY_EDITOR

#if UNITY_2019 || UNITY_2020 || UNITY_2021_1
            GUIContent icon = UnityEditor.EditorGUIUtility.IconContent("sv_icon_dot" + index + "_sml");
            System.Type editorUtiliy = typeof(UnityEditor.EditorGUIUtility);
            var flags = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic;
            MethodInfo method_set_icon = editorUtiliy.GetMethod("SetIconForObject", flags, null, new System.Type[] { typeof(UnityEngine.Object), typeof(Texture2D) }, null);
            var args = new object[] { obj, icon.image };
            method_set_icon.Invoke(null, args);
#elif UNITY_2021 || UNITY_2022 || UNITY_2023
           GUIContent icon = UnityEditor.EditorGUIUtility.IconContent("sv_icon_dot" + index + "_sml");
           if(icon != null)
               UnityEditor.EditorGUIUtility.SetIconForObject(obj, (Texture2D)icon.image);
#endif

#endif
        }

        //Get minimum distance between a position and a line that goes from a to b
        public static float GetEdgeDist(Vector3 pos, Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            pos.y = 0f;

            Ray ray = new Ray(a, b - a);
            return Vector3.Cross(ray.direction, pos - ray.origin).magnitude;
        }

        //Find if a point is inside a polygon (list of point is the polygon edge points)
        public static bool IsPointInPolygon(Vector3 point, Transform[] polygon)
        {
            Vector3 start;
            Vector3 end = polygon[polygon.Length - 1].position;

            int i = 0;
            bool is_inside = false;
            while (i < polygon.Length)
            {
                start = end;
                end = polygon[i++].position;
                is_inside ^= (end.z > point.z ^ start.z > point.z) 
                    && ((point.x - end.x) < (point.z - end.z) * (start.x - end.x) / (start.z - end.z));
            }
            return is_inside;
        }

        //Get area size of a polygon
        public static float AreaSizePolygon(Transform[] polygon)
        {
            float area_size = 0;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (i != polygon.Length - 1)
                {
                    float mA = polygon[i].position.x * polygon[i + 1].position.z;
                    float mB = polygon[i + 1].position.x * polygon[i].position.z;
                    area_size = area_size + (mA - mB);
                }
                else
                {
                    float mA = polygon[i].position.x * polygon[0].position.z;
                    float mB = polygon[0].position.x * polygon[i].position.z;
                    area_size = area_size + (mA - mB);
                }
            }
            return Mathf.Abs(area_size * 0.5f);
        }

        //Return minimum XZ of polygon
        public static Vector3 GetPolygonMin(Transform[] polygon)
        {
            Vector3 min = new Vector3(float.MaxValue, 0f, float.MaxValue);
            foreach (Transform point in polygon)
            {
                if (point.position.x < min.x)
                    min.x = point.position.x;
                if (point.position.z < min.z)
                    min.z = point.position.z;
            }
            return min;
        }

        //Return maximum XZ of polygon
        public static Vector3 GetPolygonMax(Transform[] polygon)
        {
            Vector3 max = new Vector3(float.MinValue, 0f, float.MinValue);
            foreach (Transform point in polygon)
            {
                if (point.position.x > max.x)
                    max.x = point.position.x;
                if (point.position.z > max.z)
                    max.z = point.position.z;
            }
            return max;
        }
    }

}
