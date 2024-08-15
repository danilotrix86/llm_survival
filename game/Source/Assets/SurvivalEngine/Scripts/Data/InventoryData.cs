using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    public enum InventoryType
    {
        None=0,
        Inventory=5,
        Equipment=10,
        Storage=15,
        Bag=20,
    }

    [System.Serializable]
    public class InventoryItemData
    {
        public string item_id;
        public int quantity;
        public float durability;
        public string uid;

        public InventoryItemData(string id, int q, float dura, string uid) { item_id = id; quantity = q; durability = dura; this.uid = uid; }
        public ItemData GetItem() { return ItemData.Get(item_id); }
    }

    [System.Serializable]
    public class InventoryData
    {
        public Dictionary<int, InventoryItemData> items;
        public InventoryType type;
        public string uid;
        public int size = 99;

        public InventoryData(InventoryType type, string uid)
        {
            this.type = type;
            this.uid = uid;
            items = new Dictionary<int, InventoryItemData>();
        }

        public void FixData()
        {
            //Fix data to make sure old save files compatible with new game version
            if (items == null)
                items = new Dictionary<int, InventoryItemData>();
        }

        //---- Items -----
        public int AddItem(string item_id, int quantity, float durability, string uid)
        {
            if (!string.IsNullOrEmpty(item_id) && quantity > 0)
            {
                ItemData idata = ItemData.Get(item_id);
                int max = idata != null ? idata.inventory_max : 999;
                int slot = GetFirstItemSlot(item_id, max - quantity);

                if (slot >= 0)
                {
                    AddItemAt(item_id, slot, quantity, durability, uid);
                }
                return slot;
            }
            return -1;
        }

        public void RemoveItem(string item_id, int quantity)
        {
            if (!string.IsNullOrEmpty(item_id) && quantity > 0)
            {
                Dictionary<int, int> remove_list = new Dictionary<int, int>(); //Slot, Quantity
                foreach (KeyValuePair<int, InventoryItemData> pair in items)
                {
                    if (pair.Value != null && pair.Value.item_id == item_id && pair.Value.quantity > 0 && quantity > 0)
                    {
                        int remove = Mathf.Min(quantity, pair.Value.quantity);
                        remove_list.Add(pair.Key, remove);
                        quantity -= remove;
                    }
                }

                foreach (KeyValuePair<int, int> pair in remove_list)
                {
                    RemoveItemAt(pair.Key, pair.Value);
                }
            }
        }

        public void AddItemAt(string item_id, int slot, int quantity, float durability, string uid)
        {
            if (!string.IsNullOrEmpty(item_id) && slot >= 0 && quantity > 0)
            {
                InventoryItemData invt_slot = GetInventoryItem(slot);
                if (invt_slot != null && invt_slot.item_id == item_id)
                {
                    int amount = invt_slot.quantity + quantity;
                    float durabi = ((invt_slot.durability * invt_slot.quantity) + (durability * quantity)) / (float)amount;
                    items[slot] = new InventoryItemData(item_id, amount, durabi, uid);
                }
                else if (invt_slot == null || invt_slot.quantity <= 0)
                {
                    items[slot] = new InventoryItemData(item_id, quantity, durability, uid);
                }
            }
        }

        public void RemoveItemAt(int slot, int quantity)
        {
            if (slot >= 0 && quantity >= 0)
            {
                InventoryItemData invt_slot = GetInventoryItem(slot);
                if (invt_slot != null && invt_slot.quantity > 0)
                {
                    int amount = invt_slot.quantity - quantity;
                    if (amount <= 0)
                        items.Remove(slot);
                    else
                        items[slot] = new InventoryItemData(invt_slot.item_id, amount, invt_slot.durability, invt_slot.uid);
                }
            }
        }

        public void SwapItemSlots(int slot1, int slot2)
        {
            InventoryItemData invt_slot1 = GetInventoryItem(slot1);
            InventoryItemData invt_slot2 = GetInventoryItem(slot2);
            items[slot1] = invt_slot2;
            items[slot2] = invt_slot1;

            if (invt_slot2 == null)
                items.Remove(slot1);
            if (invt_slot1 == null)
                items.Remove(slot2);
        }

        public void AddItemDurability(int slot, float value)
        {
            if (items.ContainsKey(slot))
            {
                InventoryItemData invdata = items[slot];
                invdata.durability += value;
            }
        }

        public void UpdateAllDurability(float game_hours)
        {
            List<int> remove_items = new List<int>();

            foreach (KeyValuePair<int, InventoryItemData> pair in items)
            {
                InventoryItemData invdata = pair.Value;
                ItemData idata = ItemData.Get(invdata?.item_id);

                if (idata != null && invdata != null)
                {
                    if (idata.durability_type == DurabilityType.Spoilage)
                        invdata.durability -= game_hours;
                    if (idata.durability_type == DurabilityType.UsageTime && type == InventoryType.Equipment)
                        invdata.durability -= game_hours;
                }

                if (idata != null && invdata != null && idata.HasDurability() && invdata.durability <= 0f)
                    remove_items.Add(pair.Key);
            }

            foreach (int slot in remove_items)
            {
                InventoryItemData invdata = GetInventoryItem(slot);
                ItemData idata = ItemData.Get(invdata?.item_id);
                RemoveItemAt(slot, invdata.quantity);
                if (idata.container_data)
                    AddItemAt(idata.container_data.id, slot, invdata.quantity, idata.container_data.durability, UniqueID.GenerateUniqueID());
            }
            remove_items.Clear();
        }

        public void RemoveAll()
        {
            items.Clear();
        }

        // ----- Getters ------

        public bool HasItem(string item_id, int quantity = 1)
        {
            return CountItem(item_id) >= quantity;
        }

        public bool HasItemIn(int slot)
        {
            return items.ContainsKey(slot) && items[slot].quantity > 0;
        }

        public bool IsItemIn(string item_id, int slot)
        {
            return items.ContainsKey(slot) && items[slot].item_id == item_id && items[slot].quantity > 0;
        }

        public int CountItem(string item_id)
        {
            int value = 0;
            foreach (KeyValuePair<int, InventoryItemData> pair in items)
            {
                if (pair.Value != null && pair.Value.item_id == item_id)
                    value += pair.Value.quantity;
            }
            return value;
        }

        public bool HasEmptySlot()
        {
            return GetFirstEmptySlot() >= 0;
        }

        public int GetFirstEmptySlot()
        {
            for (int i = 0; i < size; i++)
            {
                InventoryItemData invdata = GetInventoryItem(i);
                if (invdata == null || invdata.quantity <= 0)
                    return i;
            }
            return -1;
        }

        public int GetFirstItemSlot(string item_id, int slot_max)
        {
            foreach (KeyValuePair<int, InventoryItemData> pair in items)
            {
                if (pair.Value != null && pair.Value.item_id == item_id && pair.Value.quantity <= slot_max)
                    return pair.Key;
            }
            return GetFirstEmptySlot();
        }

        public bool HasItemInGroup(GroupData group, int quantity=1)
        {
            return CountItemInGroup(group) >= quantity;
        }

        public int CountItemInGroup(GroupData group)
        {
            int value = 0;
            foreach (KeyValuePair<int, InventoryItemData> pair in items)
            {
                if (pair.Value != null) {

                    ItemData idata = ItemData.Get(pair.Value.item_id);
                    if(idata != null && idata.HasGroup(group))
                        value += pair.Value.quantity;
                }
            }
            return value;
        }

        public InventoryItemData GetFirstItemInGroup(GroupData group)
        {
            foreach (KeyValuePair<int, InventoryItemData> pair in items)
            {
                if (pair.Value != null)
                {
                    ItemData idata = ItemData.Get(pair.Value.item_id);
                    if (idata != null && pair.Value.quantity > 0)
                    {
                        if (idata.HasGroup(group))
                            return pair.Value;
                    }
                }
            }
            return null;
        }

        public InventoryItemData GetInventoryItem(int slot)
        {
            if (items.ContainsKey(slot))
                return items[slot];
            return null;
        }

        public ItemData GetItem(int slot)
        {
            InventoryItemData idata = GetInventoryItem(slot);
            return idata?.GetItem();
        }

        public bool CanTakeItem(string item_id, int quantity)
        {
            ItemData idata = ItemData.Get(item_id);
            int max = idata != null ? idata.inventory_max : 999;
            int slot = GetFirstItemSlot(item_id, max - quantity);
            return slot >= 0;
        }

        // ----- Equip Items ----

        public void EquipItem(EquipSlot equip_slot, string item_id, float durability, string uid)
        {
            int eslot = (int)equip_slot;
            InventoryItemData idata = new InventoryItemData(item_id, 1, durability, uid);
            items[eslot] = idata;
        }

        public void UnequipItem(EquipSlot equip_slot)
        {
            int eslot = (int)equip_slot;
            if (items.ContainsKey(eslot))
                items.Remove(eslot);
        }

        public bool HasEquippedItem(EquipSlot equip_slot)
        {
            return GetEquippedItem(equip_slot) != null;
        }

        //Return first equipped weapon, in any slot
        public InventoryItemData GetEquippedWeapon()
        {
            foreach (KeyValuePair<int, InventoryItemData> item in items)
            {
                ItemData idata = ItemData.Get(item.Value.item_id);
                if (idata && idata.IsWeapon())
                    return item.Value;
            }
            return null;
        }

        //Return first equipped weapon slot
        public EquipSlot GetEquippedWeaponSlot()
        {
            foreach (KeyValuePair<int, InventoryItemData> item in items)
            {
                ItemData idata = ItemData.Get(item.Value.item_id);
                if (idata && idata.IsWeapon())
                    return (EquipSlot) item.Key;
            }
            return EquipSlot.None;
        }

        public ItemData GetEquippedWeaponData()
        {
            InventoryItemData idata = GetEquippedWeapon();
            return idata?.GetItem();
        }

        public InventoryItemData GetEquippedItem(EquipSlot equip_slot)
        {
            int slot = (int)equip_slot;
            if (items.ContainsKey(slot))
                return items[slot];
            return null;
        }

        public ItemData GetEquippedItemData(EquipSlot equip_slot)
        {
            InventoryItemData idata = GetEquippedItem(equip_slot);
            return idata?.GetItem();
        }

        public static InventoryData Get(InventoryType type, string uid)
        {
            return PlayerData.Get().GetInventory(type, uid);
        }

        public static InventoryData Get(InventoryType type, int player_id)
        {
            return PlayerData.Get().GetInventory(type, player_id);
        }

        public static InventoryData GetEquip(InventoryType type, int player_id)
        {
            return PlayerData.Get().GetEquipInventory(type, player_id);
        }

        public static bool Exists(string uid)
        {
            return PlayerData.Get().HasInventory(uid);
        }
    }

    [System.Serializable]
    public class InventorySlot
    {
        public InventoryData inventory;
        public int slot;

        public InventorySlot() { }
        public InventorySlot(InventoryData inv, int s) { inventory = inv; slot = s; }

        public InventoryItemData GetInventoryItem()
        {
            return inventory.GetInventoryItem(slot);
        }

        public ItemData GetItem()
        {
            return inventory.GetItem(slot);
        }
    }
}
