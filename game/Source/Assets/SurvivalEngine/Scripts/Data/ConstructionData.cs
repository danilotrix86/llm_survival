using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Data file for Constructions
    /// </summary>

    [CreateAssetMenu(fileName = "ConstructionData", menuName = "SurvivalEngine/ConstructionData", order = 4)]
    public class ConstructionData : CraftData
    {
        [Header("--- ConstructionData ------------------")]

        public GameObject construction_prefab; //Prefab spawned when the construction is built

        [Header("Ref Data")]
        public ItemData take_item_data; //For constructions that can be picked (trap, lure) what is the matching item

        [Header("Stats")]
        public DurabilityType durability_type;
        public float durability;

        private static List<ConstructionData> construction_data = new List<ConstructionData>();

        public bool HasDurability()
        {
            return durability_type != DurabilityType.None && durability >= 0.1f;
        }

        public static new void Load(string folder = "")
        {
            construction_data.Clear();
            construction_data.AddRange(Resources.LoadAll<ConstructionData>(folder));
        }

        public new static ConstructionData Get(string construction_id)
        {
            foreach (ConstructionData item in construction_data)
            {
                if (item.id == construction_id)
                    return item;
            }
            return null;
        }

        public new static List<ConstructionData> GetAll()
        {
            return construction_data;
        }
    }

}
