using System.Collections;
using System.Collections.Generic;
using SurvivalEngine.WorldGen;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Helps to generate unique ids for each individual instance of objects in the scene. 
    /// Unique IDs are mostly used in the save file to keep track of the state of an object.
    /// </summary>

    public class UniqueID : MonoBehaviour
    {
        public LLMBiome parentCell;

        public string uid_prefix; //Will be added to the front of every ID of this type of object, set in the prefab

        [TextArea(1, 2)]
        public string unique_id; //The unique ID, should be empty in the prefab. Should only be added to instances in the scene. Can be automatically generated

        private Dictionary<string, string> sub_dict = new Dictionary<string, string>();

        private static Dictionary<string, UniqueID> dict_id = new Dictionary<string, UniqueID>();

        void Awake()
        {
            if (!string.IsNullOrEmpty(unique_id))
            {
                dict_id[unique_id] = this;
            }
        }

        private void OnDestroy()
        {
            dict_id.Remove(unique_id);
        }

        private void Start()
        {
            if (HasUID() && PlayerData.Get().IsObjectHidden(unique_id))
                gameObject.SetActive(false);

            if (!HasUID() && Time.time < 0.1f)
                Debug.LogWarning("UID is empty on " + gameObject.name + ". Make sure to generate UIDs with SurvivalEngine->Generate UID");

            parentCell = LLMBiome.getNearestCell(transform.position);
        }

        public void Hide()
        {
            PlayerData.Get().HideObject(unique_id);
            gameObject.SetActive(false);
        }

        public void Show()
        {
            PlayerData.Get().ShowObject(unique_id);
            gameObject.SetActive(true);
        }

        public void SetUID(string uid)
        {
            if (dict_id.ContainsKey(unique_id))
                dict_id.Remove(unique_id);
            unique_id = uid;
            if (!string.IsNullOrEmpty(unique_id))
                dict_id[unique_id] = this;
            sub_dict.Clear();
        }

        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id);
        }

        public void GenerateUID()
        {
            SetUID(uid_prefix + GenerateUniqueID());
        }

        public void GenerateUIDEditor()
        {
            unique_id = uid_prefix + GenerateUniqueID(); //Dont save to dict in editor mode
        }

        public string GetSubUID(string sub_tag)
        {
            if (sub_dict.ContainsKey(sub_tag))
                return sub_dict[sub_tag]; //Dict prevents GC alloc
            if (string.IsNullOrEmpty(unique_id))
                return ""; //No UID

            string sub_uid = unique_id + "_" + sub_tag;
            sub_dict[sub_tag] = sub_uid;
            return sub_uid;
        }
		
		public void RemoveAllSubUIDs()
        {
            PlayerData pdata = PlayerData.Get();
            foreach (KeyValuePair<string, string> pair in sub_dict)
            {
                string subuid = pair.Value;
                pdata.RemoveAllCustom(subuid);
            }
            sub_dict.Clear();
        }
		
		public void SetCustomInt(string sub_id, int val) { PlayerData.Get().SetCustomInt(GetSubUID(sub_id), val); }
        public void SetCustomFloat(string sub_id, float val) { PlayerData.Get().SetCustomFloat(GetSubUID(sub_id), val); }
        public void SetCustomString(string sub_id, string val) { PlayerData.Get().SetCustomString(GetSubUID(sub_id), val); }

        public int GetCustomInt(string sub_id) { return PlayerData.Get().GetCustomInt(GetSubUID(sub_id)); }
        public float GetCustomFloat(string sub_id) { return PlayerData.Get().GetCustomFloat(GetSubUID(sub_id)); }
        public string GetCustomString(string sub_id) { return PlayerData.Get().GetCustomString(GetSubUID(sub_id)); }

        public bool HasCustomInt(string sub_id) { return PlayerData.Get().HasCustomInt(GetSubUID(sub_id)); }
        public bool HasCustomFloat(string sub_id) { return PlayerData.Get().HasCustomFloat(GetSubUID(sub_id)); }
        public bool HasCustomString(string sub_id) { return PlayerData.Get().HasCustomString(GetSubUID(sub_id)); }

        public static string GenerateUniqueID(int min=11, int max=17)
        {
            int length = Random.Range(min, max);
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            string unique_id = "";
            for (int i = 0; i < length; i++)
            {
                unique_id += chars[Random.Range(0, chars.Length - 1)];
            }
            return unique_id;
        }

        public static void GenerateAll(UniqueID[] objs)
        {
            HashSet<string> existing_ids = new HashSet<string>();

            foreach (UniqueID uid_obj in objs)
            {
                if (uid_obj.unique_id != "")
                {
                    if (existing_ids.Contains(uid_obj.unique_id))
                        uid_obj.unique_id = "";
                    else
                        existing_ids.Add(uid_obj.unique_id);
                }
            }

            foreach (UniqueID uid_obj in objs)
            {
                if (uid_obj.unique_id == "")
                {
                    //Generate new ID
                    string new_id = "";
                    while (new_id == "" || existing_ids.Contains(new_id))
                    {
                        new_id = UniqueID.GenerateUniqueID();
                    }

                    //Add new id
                    uid_obj.unique_id = uid_obj.uid_prefix + new_id;
                    existing_ids.Add(new_id);

#if UNITY_EDITOR
                    if (Application.isEditor && !Application.isPlaying)
                        UnityEditor.EditorUtility.SetDirty(uid_obj);
#endif
                }
            }
        }

        public static void ClearAll(UniqueID[] objs)
        {
            foreach (UniqueID uid_obj in objs)
            {
                uid_obj.unique_id = "";

#if UNITY_EDITOR
                if (Application.isEditor && !Application.isPlaying)
                    UnityEditor.EditorUtility.SetDirty(uid_obj);
#endif
            }
        }

        public static bool HasID(string id)
        {
            return dict_id.ContainsKey(id);
        }

        public static GameObject GetByID(string id)
        {
            if (dict_id.ContainsKey(id))
            {
                return dict_id[id].gameObject;
            }
            return null;
        }
    }

}