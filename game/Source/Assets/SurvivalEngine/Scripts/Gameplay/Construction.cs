using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
     
    /// <summary> 
    /// Constructions are objects that can be placed on the map by the player (by crafting or with items)
    /// </summary> 

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Buildable))]
    [RequireComponent(typeof(UniqueID))]
    public class Construction : Craftable
    {
        [Header("Construction")]
        public ConstructionData data;

        [HideInInspector]
        public bool was_spawned = false; //If true, means it was crafted or loaded from save file

        private Selectable selectable; //Can be nulls
        private Destructible destruct; //Can be nulls
        private Buildable buildable;
        private UniqueID unique_id;

        private static List<Construction> construct_list = new List<Construction>();

        protected override void Awake()
        {
            base.Awake();
            construct_list.Add(this);
            selectable = GetComponent<Selectable>();
            buildable = GetComponent<Buildable>();
            destruct = GetComponent<Destructible>();
            unique_id = GetComponent<UniqueID>();

            buildable.onBuild += OnBuild;

            if (selectable != null)
            {
                selectable.onDestroy += OnDeath;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            construct_list.Remove(this);
        }

        void Start()
        {
            if (!was_spawned && PlayerData.Get().IsObjectRemoved(GetUID()))
            {
                Destroy(gameObject);
                return;
            }
        }

        public void Kill()
        {
            if (destruct != null)
                destruct.Kill();
            else if (selectable != null)
                selectable.Destroy();
            else
                Destroy(gameObject);
        }

        private void OnBuild()
        {
            if (data != null)
            {
                BuiltConstructionData cdata = PlayerData.Get().AddConstruction(data.id, SceneNav.GetCurrentScene(), transform.position, transform.rotation, data.durability);
                unique_id.unique_id = cdata.uid;
            }
        }

        private void OnDeath()
        {
            if (data != null)
            {
                foreach (PlayerCharacter character in PlayerCharacter.GetAll())
                    character.SaveData.AddKillCount(data.id); //Add kill count
            }

            PlayerData.Get().RemoveConstruction(GetUID());
            if (!was_spawned)
                PlayerData.Get().RemoveObject(GetUID());
        }

        public bool IsBuilt()
        {
            return !IsDead() && !buildable.IsBuilding();
        }

        public bool IsDead()
        {
            if(destruct)
                return destruct.IsDead();
            return false;
        }

        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id.unique_id);
        }

        public string GetUID()
        {
            return unique_id.unique_id;
        }

        public bool HasGroup(GroupData group)
        {
            if (data != null)
                return data.HasGroup(group) || selectable.HasGroup(group);
            return selectable.HasGroup(group);
        }

        public Selectable GetSelectable()
        {
            return selectable; //May be null
        }

        public Destructible GetDestructible()
        {
            return destruct; //May be null
        }

        public Buildable GetBuildable()
        {
            return buildable;
        }

        public BuiltConstructionData SaveData
        {
            get { return PlayerData.Get().GetConstructed(GetUID()); }  //Can be null if not built or spawned
        }

        public static new Construction GetNearest(Vector3 pos, float range = 999f)
        {
            Construction nearest = null;
            float min_dist = range;
            foreach (Construction construction in construct_list)
            {
                float dist = (construction.transform.position - pos).magnitude;
                if (dist < min_dist && construction.IsBuilt())
                {
                    min_dist = dist;
                    nearest = construction;
                }
            }
            return nearest;
        }

        public static int CountInRange(Vector3 pos, float range)
        {
            int count = 0;
            foreach (Construction construct in GetAll())
            {
                float dist = (construct.transform.position - pos).magnitude;
                if (dist < range && construct.IsBuilt())
                    count++;
            }
            return count;
        }

        public static int CountInRange(ConstructionData data, Vector3 pos, float range)
        {
            int count = 0;
            foreach (Construction construct in GetAll())
            {
                if (construct.data == data && construct.IsBuilt())
                {
                    float dist = (construct.transform.position - pos).magnitude;
                    if (dist < range)
                        count++;
                }
            }
            return count;
        }

        public static Construction GetByUID(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (Construction construct in construct_list)
                {
                    if (construct.GetUID() == uid)
                        return construct;
                }
            }
            return null;
        }

        public static List<Construction> GetAllOf(ConstructionData data)
        {
            List<Construction> valid_list = new List<Construction>();
            foreach (Construction construct in construct_list)
            {
                if (construct.data == data)
                    valid_list.Add(construct);
            }
            return valid_list;
        }

        public static new List<Construction> GetAll()
        {
            return construct_list;
        }

        //Spawn an existing one in the save file (such as after loading)
        public static Construction Spawn(string uid, Transform parent = null)
        {
            BuiltConstructionData bdata = PlayerData.Get().GetConstructed(uid);
            if (bdata != null && bdata.scene == SceneNav.GetCurrentScene())
            {
                ConstructionData cdata = ConstructionData.Get(bdata.construction_id);
                if (cdata != null)
                {
                    GameObject build = Instantiate(cdata.construction_prefab, bdata.pos, bdata.rot);
                    build.transform.parent = parent;

                    Construction construct = build.GetComponent<Construction>();
                    construct.data = cdata;
                    construct.was_spawned = true;
                    construct.unique_id.unique_id = uid;
                    return construct;
                }
            }
            return null;
        }

        //Create a totally new one that will be added to save file, but only after constructed by the player
        public static Construction CreateBuildMode(ConstructionData data, Vector3 pos)
        {
            GameObject build = Instantiate(data.construction_prefab, pos, data.construction_prefab.transform.rotation);
            Construction construct = build.GetComponent<Construction>();
            construct.data = data;
            construct.was_spawned = true;
            return construct;
        }

        //Create a totally new one that will be added to save file, already constructed
        public static Construction Create(ConstructionData data, Vector3 pos)
        {
            Construction construct = CreateBuildMode(data, pos);
            construct.buildable.FinishBuild();
            return construct;
        }

        public static Construction Create(ConstructionData data, Vector3 pos, Quaternion rot)
        {
            Construction construct = CreateBuildMode(data, pos);
            construct.transform.rotation = rot;
            construct.buildable.FinishBuild();
            return construct;
        }
    }

}