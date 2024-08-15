using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    /// <summary>
    /// Class that manage character inventory, and related actions
    /// Also spawn/unspawn equipment visual attachmments
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterInventory : MonoBehaviour
    {
        public int inventory_size = 15; //If you change this, make sure to change the UI
        public ItemData[] starting_items;

        public UnityAction<Item> onTakeItem;
        public UnityAction<Item> onDropItem;
        public UnityAction<ItemData> onGainItem;

        private PlayerCharacter character;

        private EquipAttach[] equip_attachments;

        private Dictionary<string, EquipItem> equipped_items = new Dictionary<string, EquipItem>();

		private GroupData activeGroup = null;

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            equip_attachments = GetComponentsInChildren<EquipAttach>();
        }

        private void Start()
        {
            bool has_inventory = PlayerData.Get().HasInventory(character.player_id);

            InventoryData.size = inventory_size; //This will also create the inventory
            EquipData.size = 99; //Create the inventory, size doesnt matter for equip

            //If new game, add starting items
            if (!has_inventory)
            {
                InventoryData invdata = InventoryData.Get(InventoryType.Inventory, character.player_id);
                foreach (ItemData item in starting_items)
                {
                    invdata.AddItem(item.id, 1, item.durability, UniqueID.GenerateUniqueID());
                }
            }
        }

        void Update()
        {
            HashSet<string> equipped_data = new HashSet<string>();
            List<string> remove_list = new List<string>();

            //Equip unequip
            foreach (KeyValuePair<int, InventoryItemData> item in EquipData.items)
            {
				if(TheGame.Get().IsNight() && item.Value.item_id == "torch"){
                    equipped_data.Add(item.Value.item_id);
                    if (!equipped_items.ContainsKey(item.Value.item_id))
                        EquipAddedItem(item.Value.item_id);
				}

                if (item.Value != null && item.Value.GetItem().HasGroup(activeGroup))
                {
                    equipped_data.Add(item.Value.item_id);
                    if (!equipped_items.ContainsKey(item.Value.item_id))
                        EquipAddedItem(item.Value.item_id);
                }
            }

            //Create remove list
            foreach (KeyValuePair<string, EquipItem> item in equipped_items)
            {
                if (!equipped_data.Contains(item.Key))
                    remove_list.Add(item.Key);
            }

            //Remove
            foreach (string item_id in remove_list)
            {
                UnequipRemovedItem(item_id);
            }
        }

        //------- Items ----------

        //Take an Item on the floor
        public void TakeItem(Item item)
        {
            if (BagData != null && !InventoryData.CanTakeItem(item.data.id, item.quantity) && !item.data.IsBag())
            {
                TakeItem(BagData, item); //Take into bag
            }
            else
            {
                TakeItem(InventoryData, item); //Take into main inventory
            }
        }

		void TakeDirect(InventoryData inventory, Item item){
			//Make sure wasnt destroyed during the 0.4 sec
			if (item != null && inventory.CanTakeItem(item.data.id, item.quantity))
			{
				PlayerData pdata = PlayerData.Get();
				DroppedItemData dropped_item = pdata.GetDroppedItem(item.GetUID());
				float durability = dropped_item != null ? dropped_item.durability : item.data.durability;
				int slot = inventory.AddItem(item.data.id, item.quantity, durability, item.GetUID()); //Add to inventory

				ItemTakeFX.DoTakeFX(item.transform.position, item.data, inventory.type, slot);

			}
		}

        public void TakeItem(InventoryData inventory, Item item)
        {
			bool isPlant = item.GetComponent<Plant>() != null;
            if (item != null && (!character.IsBusy() || isPlant) && inventory.CanTakeItem(item.data.id, item.quantity))
            {
                character.FaceTorward(item.transform.position);

                if (onTakeItem != null)
                    onTakeItem.Invoke(item);

				TakeDirect(inventory, item);
                item.TakeItem();
            }
        }

        //Auto take item, without animation, facing, action
        public void AutoTakeItem(Item item, bool ignoreBusy=false)
        {
            if (BagData != null && !InventoryData.CanTakeItem(item.data.id, item.quantity) && !item.data.IsBag())
            {
                AutoTakeItem(BagData, item, ignoreBusy); //Take into bag
            }
            else
            {
                AutoTakeItem(InventoryData, item, ignoreBusy); //Take into main inventory
            }
        }

        public void AutoTakeItem(InventoryData inventory, Item item, bool ignoreBusy = false)
        {
            if (item != null && (!character.IsBusy() || ignoreBusy) && inventory.CanTakeItem(item.data.id, item.quantity))
            {
                PlayerData pdata = PlayerData.Get();
                DroppedItemData dropped_item = pdata.GetDroppedItem(item.GetUID());
                float durability = dropped_item != null ? dropped_item.durability : item.data.durability;
                int slot = inventory.AddItem(item.data.id, item.quantity, durability, item.GetUID()); //Add to inventory

                ItemTakeFX.DoTakeFX(item.transform.position, item.data, inventory.type, slot);

                item.TakeItem(); //Destroy item
            }
        }

        //Gain an new item directly to inventory
        public void GainItem(ItemData item, int quantity=1)
        {
            GainItem(item, quantity, transform.position);
        }

        public void GainItem(ItemData item, int quantity, Vector3 source_pos)
        {
            if (BagData != null && !InventoryData.CanTakeItem(item.id, quantity) && !item.IsBag())
            {
                GainItem(BagData, item, quantity, source_pos); //Gain into bag
            }
            else
            {
                GainItem(InventoryData, item, quantity, source_pos); //Gain into main inventory
            }
        }

        //Gain into specified inventory
        public void GainItem(InventoryData inventory, ItemData item, int quantity=1)
        {
            GainItem(inventory, item, quantity, transform.position);
        }

        public void GainItem(InventoryData inventory, ItemData item, int quantity, Vector3 source_pos)
        {
            if (item != null)
            {
                if (inventory.CanTakeItem(item.id, quantity))
                {
                    if (inventory.type == InventoryType.Equipment)
                    {
                        EquipData.EquipItem(item.equip_slot, item.id, item.durability, UniqueID.GenerateUniqueID());
                        ItemTakeFX.DoTakeFX(source_pos, item, inventory.type, (int)item.equip_slot);
                    }
                    else
                    {
                        int islot = inventory.AddItem(item.id, quantity, item.durability, UniqueID.GenerateUniqueID());
                        ItemTakeFX.DoTakeFX(source_pos, item, inventory.type, islot);
                    }
                }
                else
                {
                    Item.Create(item, character.GetPosition(), quantity);
                }
            }
        }

        //Use an item in your inventory and build it immediately on the map (skipping build-mode)
        public void BuildItem(int slot)
        {
            BuildItem(InventoryData, slot);
        }

        public void BuildItem(InventoryData inventory, int slot)
        {
            character.Crafting.BuildItem(inventory, slot);
            PlayerUI.Get(character.player_id)?.CancelSelection();
        }

        //Eat item and gain its attributes
        public void EatItem(int slot)
        {
            EatItem(InventoryData, slot);
        }

        public void EatItem(InventoryData inventory, int slot)
        {
            InventoryItemData idata = inventory.GetInventoryItem(slot);
            ItemData item = ItemData.Get(idata?.item_id);
            if (item != null && item.type == ItemType.Consumable)
            {
                if (inventory.IsItemIn(item.id, slot))
                {
                    inventory.RemoveItemAt(slot, 1);
                    if (item.container_data)
                        inventory.AddItem(item.container_data.id, 1, item.container_data.durability, UniqueID.GenerateUniqueID());

                    character.StopSleep();
                    character.Attributes.AddAttribute(AttributeType.Health, item.eat_hp);
                    character.Attributes.AddAttribute(AttributeType.Hunger, item.eat_hunger);
                    character.Attributes.AddAttribute(AttributeType.Thirst, item.eat_thirst);
                    character.Attributes.AddAttribute(AttributeType.Happiness, item.eat_happiness);

                    foreach (BonusEffectData bonus in item.eat_bonus)
                    {
                        character.SaveData.AddTimedBonus(bonus.type, bonus.value, item.eat_bonus_duration);
                    }
                }
            }
        }

        //Drop item on the floor
        public void DropItem(int slot)
        {
            DropItem(InventoryData, slot);
        }

        public void DropItem(InventoryData inventory, int slot)
        {
            InventoryItemData invdata = inventory?.GetInventoryItem(slot);
            ItemData idata = ItemData.Get(invdata?.item_id);
            if (invdata != null && idata != null && invdata.quantity > 0)
            {
                if (idata.CanBeDropped())
                {
                    inventory.RemoveItemAt(slot, invdata.quantity);
                    Item iitem = Item.Create(idata, character.GetPosition(), invdata.quantity, invdata.durability, invdata.uid);

                    PlayerUI.Get(character.player_id)?.CancelSelection();

                    if (onDropItem != null)
                        onDropItem.Invoke(iitem);
                }
                else if (idata.CanBeBuilt())
                {
                    BuildItem(inventory, slot);
                }
            }
        }

        //Remove item directly from inventory, keeping its container
        public void UseItem(ItemData item, int quantity = 1)
        {
            if (item != null)
            {
                for (int i = 0; i < quantity; i++)
                {
                    if (InventoryData.HasItem(item.id, 1))
                        UseItem(InventoryData, item, 1);
                    else if (EquipData.HasItem(item.id, 1))
                        UseItem(EquipData, item, 1);
                    else if (BagData != null && BagData.HasItem(item.id, 1))
                        UseItem(BagData, item, 1);
                }
            }
        }

        //Remove item in one inventory, keeping its container
        public void UseItem(InventoryData inventory, ItemData item, int quantity = 1)
        {
            if (item != null)
            {
                inventory.RemoveItem(item.id, quantity);
                if (item.container_data)
                    inventory.AddItem(item.container_data.id, quantity, item.container_data.durability, UniqueID.GenerateUniqueID());
            }
        }

        //Remove items of group directly from inventory, keeping its container
        public void UseItemInGroup(GroupData group, int quantity = 1)
        {
            if (group != null)
            {
                for (int i = 0; i < quantity; i++)
                {
                    if (InventoryData.HasItemInGroup(group, 1))
                        UseItemInGroup(InventoryData, group, 1);
                    else if (EquipData.HasItemInGroup(group, 1))
                        UseItemInGroup(EquipData, group, 1);
                    else if (BagData != null && BagData.HasItemInGroup(group, 1))
                        UseItemInGroup(BagData, group, 1);
                }
            }
        }

        //Remove items of group in one inventory, keeping its container
        public void UseItemInGroup(InventoryData inventory, GroupData group, int quantity = 1)
        {
            if (group != null)
            {
                //Find which items should be used (by group)
                Dictionary<ItemData, int> remove_list = new Dictionary<ItemData, int>(); //Item, Quantity
                foreach (KeyValuePair<int, InventoryItemData> pair in inventory.items)
                {
                    ItemData idata = ItemData.Get(pair.Value?.item_id);
                    if (idata != null && idata.HasGroup(group) && pair.Value.quantity > 0 && quantity > 0)
                    {
                        int remove = Mathf.Min(quantity, pair.Value.quantity);
                        remove_list.Add(idata, remove);
                        quantity -= remove;
                    }
                }

                //Use those specific items
                foreach (KeyValuePair<ItemData, int> pair in remove_list)
                {
                    UseItem(inventory, pair.Key, pair.Value);
                }
            }
        }

        //Compatibility with older versions
        public void RemoveItem(ItemData item, int quantity = 1)
        {
            UseItem(item, quantity);
        }

        public void RemoveAll(InventoryData inventory)
        {
            inventory.RemoveAll();
        }

        // ---- Equipments -----

        //Equip inventory item
        public void EquipItem(int islot)
        {
            InventoryItemData item = InventoryData.GetInventoryItem(islot);
            ItemData idata = ItemData.Get(item?.item_id);
            if (idata != null && idata.type == ItemType.Equipment)
            {
                EquipItemTo(islot, idata.equip_slot);
            }
        }

        public void EquipItem(InventoryData inventory, int islot)
        {
            InventoryItemData item = inventory.GetInventoryItem(islot);
            ItemData idata = ItemData.Get(item?.item_id);
            if (idata != null && idata.type == ItemType.Equipment)
            {
                EquipItemTo(inventory, islot, idata.equip_slot);
            }
        }

        public void UnequipItem(EquipSlot eslot)
        {
            InventoryItemData invdata = EquipData.GetEquippedItem(eslot);
            ItemData idata = ItemData.Get(invdata?.item_id);

            if (invdata != null && InventoryData.CanTakeItem(invdata.item_id, 1))
            {
                EquipData.UnequipItem(eslot);
                InventoryData.AddItem(invdata.item_id, 1, invdata.durability, invdata.uid);
            }
            else if (invdata != null && BagData != null && BagData.CanTakeItem(invdata.item_id, 1) && !idata.IsBag())
            {
                EquipData.UnequipItem(eslot);
                BagData.AddItem(invdata.item_id, 1, invdata.durability, invdata.uid);
            }
        }

        public void RemoveEquipItem(EquipSlot eslot)
        {
            InventoryItemData invtem = EquipData.GetEquippedItem(eslot);
            ItemData idata = ItemData.Get(invtem?.item_id);
            if (idata != null)
            {
                EquipData.UnequipItem(eslot);
                if (idata.container_data)
                    EquipData.EquipItem(eslot, idata.container_data.id, idata.container_data.durability, UniqueID.GenerateUniqueID());
            }
        }

        public void EquipItemTo(int islot, EquipSlot eslot)
        {
            EquipItemTo(InventoryData, islot, eslot);
        }

        public void EquipItemTo(InventoryData inventory, int islot, EquipSlot eslot)
        {
            InventoryItemData invt_slot = inventory.GetInventoryItem(islot);
            InventoryItemData invt_equip = EquipData.GetEquippedItem(eslot);
            ItemData idata = ItemData.Get(invt_slot?.item_id);
            ItemData edata = ItemData.Get(invt_equip?.item_id);

            if (invt_slot != null && inventory != EquipData && invt_slot.quantity > 0 && idata != null && eslot > 0)
            {
                if (edata == null)
                {
                    //Equip only
                    EquipData.EquipItem(eslot, idata.id, invt_slot.durability, invt_slot.uid);
                    inventory.RemoveItemAt(islot, 1);
                }
                else if (invt_slot.quantity == 1 && idata.type == ItemType.Equipment)
                {
                    //Swap
                    inventory.RemoveItemAt(islot, 1);
                    EquipData.UnequipItem(eslot);
                    EquipData.EquipItem(eslot, idata.id, invt_slot.durability, invt_slot.uid);
                    inventory.AddItemAt(edata.id, islot, 1, invt_equip.durability, invt_equip.uid);
                }
            }
        }

        public void UnequipItemTo(EquipSlot eslot, int islot)
        {
            UnequipItemTo(InventoryData, eslot, islot);
        }

        public void UnequipItemTo(InventoryData inventory, EquipSlot eslot, int islot)
        {
            InventoryItemData invt_slot = inventory.GetInventoryItem(islot);
            InventoryItemData invt_equip = EquipData.GetEquippedItem(eslot);
            ItemData idata = ItemData.Get(invt_slot?.item_id);
            ItemData edata = ItemData.Get(invt_equip?.item_id);
            bool both_bag = inventory.type == InventoryType.Bag && edata.IsBag();

            if (edata != null && inventory != EquipData && !both_bag)
            {
                bool same_item = idata != null && invt_slot != null && invt_slot.quantity > 0 && idata.id == edata.id && invt_slot.quantity < idata.inventory_max;
                bool slot_empty = invt_slot == null || invt_slot.quantity <= 0;
                if (same_item || slot_empty)
                {
                    //Unequip
                    EquipData.UnequipItem(eslot);
                    inventory.AddItemAt(edata.id, islot, 1, invt_equip.durability, invt_equip.uid);
                }
                else if (idata != null && invt_slot != null && !same_item && idata.type == ItemType.Equipment && idata.equip_slot == edata.equip_slot && invt_slot.quantity == 1)
                {
                    //swap
                    inventory.RemoveItemAt(islot, 1);
                    EquipData.UnequipItem(eslot);
                    EquipData.EquipItem(eslot, idata.id, invt_slot.durability, invt_slot.uid);
                    inventory.AddItemAt(edata.id, islot, 1, invt_equip.durability, invt_equip.uid);
                }
            }
        }

        public void UpdateAllEquippedItemsDurability(bool weapon, float value)
        {
            //Durability
            foreach (KeyValuePair<int, InventoryItemData> pair in EquipData.items)
            {
                InventoryItemData invdata = pair.Value;
                ItemData idata = ItemData.Get(invdata?.item_id);
                if (idata != null && invdata != null && idata.IsWeapon() == weapon && idata.durability_type == DurabilityType.UsageCount)
                    invdata.durability += value;
            }
        }
		
		public void UpdateDurability(EquipSlot eslot, float value)
        {
            InventoryItemData inv = GetEquippedItem(eslot);
            if (inv != null)
                inv.durability += value;
        }

        public InventoryItemData GetEquippedItem(EquipSlot eslot)
        {
            InventoryItemData invt_equip = EquipData.GetEquippedItem(eslot);
            return invt_equip;
        }

        public ItemData GetEquippedItemData(EquipSlot eslot)
        {
            InventoryItemData invt_equip = EquipData.GetEquippedItem(eslot);
            return invt_equip != null ? ItemData.Get(invt_equip.item_id) : null;
        }

        public bool HasEquippedItem(EquipSlot eslot)
        {
            return GetEquippedItem(eslot) != null;
        }

        //---- Swaps/Combine items slot1=selected, slot2=clicked -----

        public void MoveItem(ItemSlot islot1, ItemSlot islot2, bool limit_one_item = false)
        {
            InventoryData inventory1 = islot1.GetInventory();
            InventoryData inventory2 = islot2.GetInventory();

            if (inventory1 == null || inventory2 == null)
                return;

            InventoryItemData iitem1 = islot1.GetInventoryItem();
            InventoryItemData iitem2 = islot2.GetInventoryItem();
            ItemData item1 = islot1.GetItem();
            ItemData item2 = islot2.GetItem();

            if (inventory2.type == InventoryType.Equipment)
            {
                EquipItem(inventory1, islot1.index);
            }
            else if (inventory1.type == InventoryType.Equipment)
            {
                EquipSlot eslot = (EquipSlot)islot1.index;
                UnequipItemTo(inventory2, eslot, islot2.index);
            }
            else if (item1 != null && item1 == item2 && !limit_one_item)
            {
                CombineItems(inventory1, islot1.index, inventory2, islot2.index);
            }
            else if (item1 != item2)
            {
                //Swap
                int quant1 = iitem1 != null ? iitem1.quantity : 0;
                int quant2 = iitem2 != null ? iitem2.quantity : 0;
                bool quantity_is_1 = quant1 <= 1 && quant2 <= 1;
                bool can_swap = !limit_one_item || quantity_is_1 || item2 == null;
                if (can_swap)
                {
                    SwapItems(inventory1, islot1.index, inventory2, islot2.index);
                }
            }
        }

        public void SwapItems(InventoryData inventory_data1, int slot1, InventoryData inventory_data2, int slot2)
        {
            PlayerData.Get().SwapInventoryItems(inventory_data1, slot1, inventory_data2, slot2);
        }

        public void CombineItems(InventoryData inventory_data1, int slot1, InventoryData inventory_data2, int slot2)
        {
            InventoryItemData invdata1 = inventory_data1?.GetInventoryItem(slot1);
            InventoryItemData invdata2 = inventory_data2?.GetInventoryItem(slot2);
            ItemData idata1 = ItemData.Get(invdata1?.item_id);
            if (idata1 != null && invdata1.item_id == invdata2.item_id && (invdata1.quantity + invdata2.quantity) <= idata1.inventory_max)
                PlayerData.Get().CombineInventoryItems(inventory_data1, slot1, inventory_data2, slot2);
        }

        //---- Getters (Multi inventory) ------

        //Has item in any player inventory
        public bool HasItem(ItemData item, int quantity = 1)
        {
            int nb = CountItem(item);
            return nb >= quantity;
        }

        public int CountItem(ItemData item)
        {
            return InventoryData.CountItem(item.id) 
                + EquipData.CountItem(item.id) 
                + (BagData != null ? BagData.CountItem(item.id) : 0);
        }

        //Check inventory / equip /bag
        public bool HasItemInGroup(GroupData group, int quantity = 1)
        {
            return CountItemInGroup(group) >= quantity;
        }

        public int CountItemInGroup(GroupData group)
        {
            return InventoryData.CountItemInGroup(group)
                + EquipData.CountItemInGroup(group)
                + (BagData != null ? BagData.CountItemInGroup(group) : 0);
        }

        //Check inventory / bag
        public bool HasEmptySlot()
        {
            return InventoryData.HasEmptySlot()
                || (BagData != null && BagData.HasEmptySlot());
        }

        public InventoryItemData GetFirstItemInGroup(GroupData group)
        {
            if (InventoryData.HasItemInGroup(group))
                return InventoryData.GetFirstItemInGroup(group);
            if (EquipData.HasItemInGroup(group))
                return EquipData.GetFirstItemInGroup(group);
            if (BagData != null && BagData.HasItemInGroup(group))
                return BagData.GetFirstItemInGroup(group);
            return null;
        }

        //Check both bag and inventory
        public bool CanTakeItem(ItemData item, int quantity = 1)
        {
            return item != null && (InventoryData.CanTakeItem(item.id, quantity) 
                || (BagData != null && BagData.CanTakeItem(item.id, quantity)));
        }

        //Return the best equipped bag (bag is an item that can contain other items)
        public InventoryItemData GetBestEquippedBag()
        {
            int best_size = 0;
            InventoryItemData bag = null;
            foreach (KeyValuePair<int, InventoryItemData> invdata in EquipData.items)
            {
                ItemData idata = invdata.Value?.GetItem();
                if(idata != null && idata.bag_size > best_size && !string.IsNullOrEmpty(invdata.Value.uid))
                {
                    best_size = idata.bag_size;
                    bag = invdata.Value;
                }
            }
            return bag;
        }

        //Return all equipped bags (bag is an item that can contain other items)
        public List<InventoryItemData> GetAllEquippedBags()
        {
            List<InventoryItemData> bags = new List<InventoryItemData>();
            foreach (KeyValuePair<int, InventoryItemData> invdata in EquipData.items)
            {
                ItemData idata = invdata.Value?.GetItem();
                if (idata != null && idata.IsBag() && !string.IsNullOrEmpty(invdata.Value.uid))
                {
                    bags.Add(invdata.Value);
                }
            }
            return bags;
        }

        //Return inventory that can take item (main one first, then bag)
        public InventoryData GetValidInventory(ItemData item, int quantity)
        {
            if (InventoryData.CanTakeItem(item.id, quantity))
                return InventoryData;
            else if(BagData != null && BagData.CanTakeItem(item.id, quantity))
                return BagData;
            return null;
        }

        //--- Equip Attachments

        private void EquipAddedItem(string item_id)
        {
            ItemData idata = ItemData.Get(item_id);
            if (idata != null && idata.equipped_prefab != null)
            {
                GameObject equip_obj = Instantiate(idata.equipped_prefab, transform.position, Quaternion.identity);
                EquipItem eitem = equip_obj.GetComponent<EquipItem>();
                if (eitem != null)
                {
                    eitem.data = idata;
                    eitem.target = GetEquipAttachment(idata.equip_slot, idata.equip_side);
                    if (eitem.child_left != null)
                        eitem.target_left = GetEquipAttachment(idata.equip_slot, EquipSide.Left);
                    if (eitem.child_right != null)
                        eitem.target_right = GetEquipAttachment(idata.equip_slot, EquipSide.Right);
                }
                equipped_items.Add(item_id, eitem);
            }
            else
            {
                equipped_items.Add(item_id, null);
            }
        }

        private void UnequipRemovedItem(string item_id)
        {
            if (equipped_items.ContainsKey(item_id))
            {
                EquipItem eitem = equipped_items[item_id];
                equipped_items.Remove(item_id);
                if (eitem != null)
                    Destroy(eitem.gameObject);
            }
        }

		public void SetActiveGroup(GroupData data){
			activeGroup = data;
		}

        //Get the EquipAttach on the character (positions on the body where the equipments can spawn)
        public EquipAttach GetEquipAttachment(EquipSlot slot, EquipSide side)
        {
            if (slot == EquipSlot.None)
                return null;

            foreach (EquipAttach attach in equip_attachments)
            {
                if (attach.slot == slot)
                {
                    if (attach.side == EquipSide.Default || side == EquipSide.Default || attach.side == side)
                        return attach;
                }
            }
            return null;
        }

        //Get the EquipItem (spawned model for equipment items), of the first equipped weapon
        public EquipItem GetEquippedWeaponMesh()
        {
            InventoryItemData invdata = EquipData.GetEquippedWeapon();
            ItemData equipped = ItemData.Get(invdata?.item_id);
            if (equipped != null)
            {
                foreach (KeyValuePair<string, EquipItem> item in equipped_items)
                {
                    if (item.Key == equipped.id)
                        return item.Value;
                }
            }
            return null;
        }

        //Get the EquipItem (spawned model for equipment items)
        public EquipItem GetEquippedItemMesh(EquipSlot slot)
        {
            InventoryItemData invdata = EquipData.GetEquippedItem(slot);
            ItemData equipped = ItemData.Get(invdata?.item_id);
            if (equipped != null)
            {
                foreach (KeyValuePair<string, EquipItem> item in equipped_items)
                {
                    if (item.Key == equipped.id)
                        return item.Value;
                }
            }
            return null;
        }
        
        //---- Shortcuts -----

        public InventoryData InventoryData
        {
            get { return InventoryData.Get(InventoryType.Inventory, character.player_id); }
        }

        public InventoryData EquipData
        {
            get { return InventoryData.Get(InventoryType.Equipment, character.player_id); }
        }

        public InventoryData BagData //Can be null if no bag
        {
            get {
                InventoryItemData bag = GetBestEquippedBag();
                return (bag != null) ? InventoryData.Get(InventoryType.Bag, bag.uid) : null;
            }
        }

        public PlayerCharacter GetCharacter()
        {
            return character;
        }
    }

}
