using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Crafting cost
    /// </summary>

    [System.Serializable]
    public class CraftCostData
    {
        public Dictionary<ItemData, int> craft_items = new Dictionary<ItemData, int>();
        public Dictionary<GroupData, int> craft_fillers = new Dictionary<GroupData, int>();
        public Dictionary<CraftData, int> craft_requirements = new Dictionary<CraftData, int>();
        public GroupData craft_near;
    }

    /// <summary>
    /// Parent data class for craftable items (items, constructions, plants)
    /// </summary>

    public class CraftData : IdData
    {
        [Header("Display")]
        public string title;
        public Sprite icon;
        [TextArea(3, 5)]
        public string desc;

        [Header("Groups")]
        public GroupData[] groups;

        [Header("Crafting")]
        public bool craftable; //Can be crafted? If false, can still be learn through the learn action
        public int craft_quantity = 1; //Does it craft more than 1?
        public float craft_duration = 0f; //How long to craft
        public int craft_sort_order = 0; //Which items appear first in crafting menu

        [Header("Crafting Cost")]
        public GroupData craft_near; //Group of selectable required near the player to craft this (ex: fire source, water source)
        public ItemData[] craft_items; //Items needed to craft this
        public GroupData[] craft_fillers; //Items needed to craft this (but that can be any item in group)
        public CraftData[] craft_requirements; //What needs to be built before you can craft this

        [Header("XP")]
        public int craft_xp = 0; //XP gained when crafting
        public string craft_xp_type;

        [Header("FX")]
        public AudioClip craft_sound;

        protected static List<CraftData> craft_data = new List<CraftData>();

        public bool HasGroup(GroupData group)
        {
            foreach (GroupData agroup in groups)
            {
                if (agroup == group)
                    return true;
            }
            return false;
        }

        public bool HasGroup(GroupData[] mgroups)
        {
            foreach (GroupData mgroup in mgroups)
            {
                foreach (GroupData agroup in groups)
                {
                    if (agroup == mgroup)
                        return true;
                }
            }
            return false;
        }


        public ItemData GetItem()
        {
            if (this is ItemData)
                return (ItemData)this;
            return null;
        }

        public ConstructionData GetConstruction()
        {
            if (this is ConstructionData)
                return (ConstructionData)this;
            return null;
        }

        public PlantData GetPlant()
        {
            if (this is PlantData)
                return (PlantData)this;
            return null;
        }

        public CharacterData GetCharacter()
        {
            if (this is CharacterData)
                return (CharacterData)this;
            return null;
        }

        public CraftCostData GetCraftCost()
        {
            CraftCostData cost = new CraftCostData();
            foreach (ItemData item in craft_items)
            {
                if (!cost.craft_items.ContainsKey(item))
                    cost.craft_items[item] = 1;
                else
                    cost.craft_items[item] += 1;
            }

            foreach (GroupData group in craft_fillers)
            {
                if (!cost.craft_fillers.ContainsKey(group))
                    cost.craft_fillers[group] = 1;
                else
                    cost.craft_fillers[group] += 1;
            }

            foreach (CraftData cdata in craft_requirements)
            {
                if (!cost.craft_requirements.ContainsKey(cdata))
                    cost.craft_requirements[cdata] = 1;
                else
                    cost.craft_requirements[cdata] += 1;
            }

            if (craft_near != null)
                cost.craft_near = craft_near;

            return cost;
        }

        public static void Load(string folder = "")
        {
            craft_data.Clear();
            craft_data.AddRange(Resources.LoadAll<CraftData>(folder));
        }

        public static List<CraftData> GetAllInGroup(GroupData group)
        {
            List<CraftData> olist = new List<CraftData>();
            foreach (CraftData item in craft_data)
            {
                if (item.HasGroup(group))
                    olist.Add(item);
            }
            return olist;
        }

        public static List<CraftData> GetAllCraftableInGroup(PlayerCharacter character, GroupData group)
        {
            List<CraftData> olist = new List<CraftData>();
            foreach (CraftData item in craft_data)
            {
                if (item.craft_quantity > 0 && item.HasGroup(group))
                {
                    bool learnt = item.craftable || character.SaveData.IsIDUnlocked(item.id);
                    if(learnt)
                        olist.Add(item);
                }
            }
            return olist;
        }

        public static CraftData Get(string id)
        {
            foreach (CraftData item in craft_data)
            {
                if (item.id == id)
                    return item;
            }
            return null;
        }

        public static List<CraftData> GetAll()
        {
            return craft_data;
        }

		public static CraftData GetByName(string name){
            foreach (CraftData item in craft_data)
            {
                if (item.title.ToLower() == name.ToLower())
                    return item;
            }
            return null;
		}

        //Count objects of type in scene
        public static int CountSceneObjects(CraftData data)
        {
            return Craftable.CountSceneObjects(data); //All objects in scene
        }

        public static int CountSceneObjects(CraftData data, Vector3 pos, float range)
        {
            return Craftable.CountSceneObjects(data, pos, range);
        }

        //Compability with older version
        public static int CountObjectInRadius(CraftData data, Vector3 pos, float radius) { return CountSceneObjects(data, pos, radius); }

        //Return all scenes objects with this data
        public static List<GameObject> GetAllObjectsOf(CraftData data)
        {
            return Craftable.GetAllObjectsOf(data);
        }

        public static GameObject Create(CraftData data, Vector3 pos)
        {
            return Craftable.Create(data, pos);
        }
    }
}