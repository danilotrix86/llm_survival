using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Generic object that can be spawned, but that is not craftable, all spawns are saved in the save file
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class Spawnable : SObject
    {
        public SpawnData data;

        private UniqueID unique_id;

        private static List<Spawnable> spawnable_list = new List<Spawnable>();

        protected virtual void Awake()
        {
            spawnable_list.Add(this);
            unique_id = GetComponent<UniqueID>();
        }

        protected virtual void OnDestroy()
        {
            spawnable_list.Remove(this);
        }

        public string GetUID()
        {
            return unique_id.unique_id;
        }

        public static Spawnable GetByUID(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (Spawnable spawnable in spawnable_list)
                {
                    if (spawnable.GetUID() == uid)
                        return spawnable;
                }
            }
            return null;
        }

        public static List<Spawnable> GetAllOf(CharacterData data)
        {
            List<Spawnable> valid_list = new List<Spawnable>();
            foreach (Spawnable spawnable in spawnable_list)
            {
                if (spawnable.data == data)
                    valid_list.Add(spawnable);
            }
            return valid_list;
        }

        public static List<Spawnable> GetAll()
        {
            return spawnable_list;
        }
        
        //Count SpawnData only
        public static int CountInRange(SpawnData data, Vector3 pos, float range)
        {
            int count = 0;
            foreach (Spawnable spawnable in GetAll())
            {
                if (spawnable.data == data)
                {
                    float dist = (spawnable.transform.position - pos).magnitude;
                    if (dist < range)
                        count++;
                }
            }
            return count;
        }

        //Spawn an object already saved in save file, when loading the scene
        public static GameObject Spawn(string uid, Transform parent = null)
        {
            SpawnedData sdata = PlayerData.Get().GetSpawnedObject(uid);
            if (sdata != null && sdata.scene == SceneNav.GetCurrentScene())
            {
                SpawnData data = SpawnData.Get(sdata.id);
                if (data != null)
                {
                    GameObject cobj = GameObject.Instantiate(data.prefab, sdata.pos, sdata.rot);
                    cobj.transform.parent = parent;
                    cobj.transform.localScale = cobj.transform.localScale * sdata.scale;

                    UniqueID unique_id = cobj.GetComponent<UniqueID>();
                    if (unique_id != null)
                        unique_id.unique_id = sdata.uid;
                    return cobj;
                }
            }
            return null;
        }

        //Create a new spawn object in save file and spawn it
        public static GameObject Create(SpawnData data, Vector3 pos, Quaternion rot, float scale)
        {
            SpawnedData sdata = PlayerData.Get().AddSpawnedObject(data.id, SceneNav.GetCurrentScene(), pos, rot, scale);
            GameObject obj = GameObject.Instantiate(data.prefab, pos, rot);
            obj.transform.localScale = obj.transform.localScale * scale;
            UniqueID unique_id = obj.GetComponent<UniqueID>();
            if (unique_id != null)
                unique_id.unique_id = sdata.uid;
            return obj;
        }

        public static GameObject Create(SpawnData data, Vector3 pos)
        {
            Quaternion rot = Quaternion.Euler(0f, 180f, 0f);
            GameObject obj = Create(data, pos, rot, 1f);
            obj.transform.rotation = rot;
            return obj;
        }
    }
}
