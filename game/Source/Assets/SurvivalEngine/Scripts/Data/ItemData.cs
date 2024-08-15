using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    public enum ItemType
    {

        Basic = 0,
        Consumable = 10,
        Equipment = 20,

    }

    public enum WeaponType
    {
        None=0,
        WeaponMelee= 10,
        WeaponRanged=20,
    }

    public enum DurabilityType
    {
        None = 0,
        UsageCount = 5, //Each use (like attacking or receiving hit) reduces durability, value is in use count
        UsageTime = 8, //Similar to spoilage, but only reduces while equipped, value is in game-hours
        Spoilage = 10, //Reduces over time, even when not in inventory, value is in game-hours
    }

    public enum EquipSlot
    {
        None = 0,
        Hand = 10,
        Head = 20,
        Body = 30,
        Feet = 40,
        Backpack = 50,
        Accessory = 60,
        Shield = 70,

        //Generic slot for other parts, rename them to your own
        Slot8 = 80,
        Slot9 = 90,
        Slot10 = 100,
    }

    public enum EquipSide
    {
        Default = 0,
        Right = 2,
        Left = 4,
    }

    /// <summary>
    /// Data file for Items
    /// </summary>

    [CreateAssetMenu(fileName = "ItemData", menuName = "SurvivalEngine/ItemData", order = 2)]
    public class ItemData : CraftData
    {
        [Header("--- ItemData ------------------")]
        public ItemType type;

        [Header("Stats")]
        public int inventory_max = 20;
        public DurabilityType durability_type;
        public float durability = 0f; //0f means infinite, 1f per hour for consumable, 1f per hit for equipment

        [Header("Stats Equip")]
        public EquipSlot equip_slot;
        public EquipSide equip_side;
        public int armor = 0;
        public int bag_size = 0;
        public BonusEffectData[] equip_bonus;

        [Header("Stats Equip Weapon")]
        public WeaponType weapon_type;
        public int damage = 0;
        public float range = 1f;
        public float attack_speed = 1f; //Will multiply the animation/windup/windout by this value
        public float attack_cooldown = 1f; //Seconds of waiting in between each attack
        public int strike_per_attack = 0; //Minimum is 1, if set to 3, each attack will hit 3 times, or shoot 3 projectiles
        public float strike_interval = 0f; //Interval in seconds between each strike of a single attack

        [Header("Stats Consume")]
        public int eat_hp = 0;
        public int eat_hunger = 0;
        public int eat_thirst = 0;
        public int eat_happiness = 0;
        public BonusEffectData[] eat_bonus;
        public float eat_bonus_duration = 0f;

        [Header("Action")]
        public SAction[] actions;

        [Header("Shop")]
        public int buy_cost = 0;
        public int sell_cost = 0;

        [Header("Ref Data")]
        public ItemData container_data;
        public PlantData plant_data;
        public ConstructionData construction_data;
        public CharacterData character_data;
        public GroupData projectile_group;

        [Header("Prefab")]
        public GameObject item_prefab;
        public GameObject equipped_prefab;
        public GameObject projectile_prefab;


        private static List<ItemData> item_data = new List<ItemData>(); //For looping
        private static Dictionary<string, ItemData> item_dict = new Dictionary<string, ItemData>(); //Faster access

        public MAction FindMergeAction(ItemData other)
        {
            if (other == null)
                return null;

            foreach (SAction action in actions)
            {
                if (action is MAction)
                {
                    MAction maction = (MAction)action;
                    if (maction.merge_target == null || other.HasGroup(maction.merge_target))
                    {
                        return maction;
                    }
                }
            }
            return null;
        }

        public MAction FindMergeAction(Selectable other)
        {
            if (other == null)
                return null;

            foreach (SAction action in actions)
            {
                if (action != null && action is MAction)
                {
                    MAction maction = (MAction)action;
                    if (maction.merge_target == null || other.HasGroup(maction.merge_target))
                    {
                        return maction;
                    }
                }
            }
            return null;
        }

        public AAction FindAutoAction(PlayerCharacter character, ItemSlot islot)
        {
            foreach (SAction action in actions)
            {
                if (action != null && action is AAction)
                {
                    AAction aaction = (AAction)action;
                    if (aaction.CanDoAction(character, islot))
                        return aaction;
                }
            }
            return null;
        }

        public CraftData GetBuildData()
        {
            if (construction_data != null)
                return construction_data;
            else if (plant_data != null)
                return plant_data;
            else if (character_data != null)
                return character_data;
            return null;
        }

        public bool CanBeDropped()
        {
            return item_prefab != null;
        }

        public bool CanBeBuilt()
        {
            return construction_data != null || character_data != null || plant_data != null;
        }

        public bool IsWeapon()
        {
            return type == ItemType.Equipment && weapon_type != WeaponType.None;
        }

        public bool IsMeleeWeapon()
        {
            return type == ItemType.Equipment && weapon_type == WeaponType.WeaponMelee;
        }

        public bool IsRangedWeapon()
        {
            return type == ItemType.Equipment && weapon_type == WeaponType.WeaponRanged;
        }

        public bool HasDurability()
        {
            return durability_type != DurabilityType.None && durability >= 0.1f;
        }

        public bool IsBag()
        {
            return type == ItemType.Equipment && bag_size > 0;
        }

        //From 0 to 100
        public int GetDurabilityPercent(float current_durability)
        {
            float perc = durability > 0.01f ? Mathf.Clamp01(current_durability / durability) : 0f;
            return Mathf.RoundToInt(perc * 100f);
        }

        public static new void Load(string folder = "")
        {
            item_data.Clear();
            item_dict.Clear();
            item_data.AddRange(Resources.LoadAll<ItemData>(folder));

            foreach (ItemData item in item_data)
            {
                if (!item_dict.ContainsKey(item.id))
                    item_dict.Add(item.id, item);
                else
                    Debug.LogError("There are two items with this ID: " + item.id);
            }
        }

        public new static ItemData Get(string item_id)
        {
            if (item_id != null && item_dict.ContainsKey(item_id))
                return item_dict[item_id];
            return null;
        }

        public new static List<ItemData> GetAll()
        {
            return item_data;
        }
    }

    [System.Serializable]
    public struct ItemDataValue
    {
        public ItemData item;
        public int quantity;
    }
}