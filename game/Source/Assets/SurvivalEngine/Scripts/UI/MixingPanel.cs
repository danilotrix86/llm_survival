using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SurvivalEngine
{
    /// <summary>
    /// Main UI panel for storages boxes (chest)
    /// </summary>

    public class MixingPanel : ItemSlotPanel
    {
        public ItemSlot result_slot;
        public Button mix_button;

        private PlayerCharacter player;
        private MixingPot mixing_pot;
        private ItemData crafed_item = null;

        private static MixingPanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;

            result_slot.onClick += OnClickResult;
        }

        protected override void RefreshPanel()
        {
            base.RefreshPanel();

            result_slot.SetSlot(crafed_item, 1);

            mix_button.interactable = CanMix();

            //Hide if too far
            Selectable select = mixing_pot?.GetSelectable();
            if (IsVisible() && player != null && select != null)
            {
                float dist = (select.transform.position - player.transform.position).magnitude;
                if (dist > select.GetUseRange(player) * 1.2f)
                {
                    Hide();
                }
            }
        }

        public void ShowMixing(PlayerCharacter player, MixingPot pot, string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                this.player = player;
                this.mixing_pot = pot;
                SetInventory(InventoryType.Storage, uid, pot.max_items);
                SetPlayer(player);
                RefreshPanel();
                Show();
            }
        }

        public bool CanMix()
        {
            bool at_least_one = false;
            foreach (ItemSlot slot in slots)
            {
                if (slot.GetItem() != null)
                    at_least_one = true;
            }
            return mixing_pot != null && at_least_one && result_slot.GetItem() == null;
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            SetInventory(InventoryType.Storage, "", 0);
            CancelSelection();
        }

        public bool HasItem(ItemData item, int quantity)
        {
            int count = 0;
            foreach (ItemSlot slot in slots)
            {
                if (slot.GetItem() == item)
                    count += slot.GetQuantity();
            }
            return count >= quantity;
        }

        public bool HasItemInGroup(GroupData group, int quantity)
        {
            int count = 0;
            foreach (ItemSlot slot in slots)
            {
                if (slot.GetItem() != null && slot.GetItem().HasGroup(group))
                    count += slot.GetQuantity();
            }
            return count >= quantity;
        }

        public void RemoveItem(ItemData item, int quantity)
        {
            foreach (ItemSlot slot in slots)
            {
                if (slot.GetItem() == item && quantity > 0)
                {
                    quantity -= slot.GetQuantity();
                    UseItem(slot, slot.GetQuantity());
                }
            }
        }

        public void RemoveItemInGroup(GroupData group, int quantity)
        {
            foreach (ItemSlot slot in slots)
            {
                if (slot.GetItem() != null && slot.GetItem().HasGroup(group) && quantity > 0)
                {
                    quantity -= slot.GetQuantity();
                    UseItem(slot, slot.GetQuantity());
                }
            }
        }

        public void RemoveAll()
        {
            foreach (ItemSlot slot in slots)
            {
                UseItem(slot, slot.GetQuantity());
            }
        }

        public bool CanCraft(CraftData item, bool skip_near = false)
        {
            if (item == null)
                return false;

            CraftCostData cost = item.GetCraftCost();
            bool can_craft = true;

            Dictionary<GroupData, int> item_groups = new Dictionary<GroupData, int>(); //Add to groups so that fillers are not same than items

            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                AddCraftCostItemsGroups(item_groups, pair.Key, pair.Value);
                if (!HasItem(pair.Key, pair.Value))
                    can_craft = false; //Dont have required items
            }

            foreach (KeyValuePair<GroupData, int> pair in cost.craft_fillers)
            {
                int value = pair.Value + CountCraftCostGroup(item_groups, pair.Key);
                if (!HasItemInGroup(pair.Key, value))
                    can_craft = false; //Dont have required items
            }

            return can_craft;
        }

        private void AddCraftCostItemsGroups(Dictionary<GroupData, int> item_groups, ItemData item, int quantity)
        {
            foreach (GroupData group in item.groups)
            {
                if (item_groups.ContainsKey(group))
                    item_groups[group] += quantity;
                else
                    item_groups[group] = quantity;
            }
        }

        private int CountCraftCostGroup(Dictionary<GroupData, int> item_groups, GroupData group)
        {
            if (item_groups.ContainsKey(group))
                return item_groups[group];
            return 0;
        }

        public void PayCraftingCost(CraftData item)
        {
            CraftCostData cost = item.GetCraftCost();
            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                RemoveItem(pair.Key, pair.Value);
            }
            foreach (KeyValuePair<GroupData, int> pair in cost.craft_fillers)
            {
                RemoveItemInGroup(pair.Key, pair.Value);
            }
        }

        public void MixItems()
        {
            ItemData item = null;
            foreach (ItemData recipe in mixing_pot.recipes)
            {
                if (item == null && CanCraft(recipe))
                {
                    item = recipe;
                    PayCraftingCost(recipe);
                }
            }

            if (item != null)
            {
                crafed_item = item;
                result_slot.SetSlot(item, 1);

                if (mixing_pot.clear_on_mix)
                    RemoveAll();
            }
        }

        public void OnClickMix()
        {
            if (CanMix())
            {
                MixItems();
            }
        }

        public void OnClickResult(UISlot slot)
        {
            if (player != null && result_slot.GetItem() != null)
            {
                player.Inventory.GainItem(result_slot.GetItem());
                result_slot.SetSlot(null, 0);
                crafed_item = null;
            }
        }

        public string GetStorageUID()
        {
            return inventory_uid;
        }

        public static MixingPanel Get()
        {
            return _instance;
        }
    }

}