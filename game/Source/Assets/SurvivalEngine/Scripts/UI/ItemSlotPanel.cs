using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    /// <summary>
    /// Generic parent UI panel that manages multiple item slots (Inventory/Equip/Storage, etc)
    /// </summary>

    public class ItemSlotPanel : UISlotPanel
    {
        public bool limit_one_item = false; //If true, only 1 item per slot

        public UnityAction<ItemSlot> onSelectSlot;
        public UnityAction<ItemSlot, ItemSlot> onMergeSlot;

        protected PlayerCharacter current_player = null;
        protected InventoryType inventory_type;
        protected string inventory_uid;
        protected int inventory_size = 99;

        protected int selected_slot = -1;
        protected int selected_right_slot = -1;

        private static List<ItemSlotPanel> slot_panels = new List<ItemSlotPanel>();

        protected override void Awake()
        {
            base.Awake();
            slot_panels.Add(this);

            for (int i = 0; i < slots.Length; i++)
                ((ItemSlot) slots[i]).Hide();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            slot_panels.Remove(this);
        }

        protected override void Start()
        {
            base.Start();

            PlayerControlsMouse.Get().onRightClick += (Vector3) => { CancelSelection(); };

            onClickSlot += OnClick;
            onRightClickSlot += OnClickRight;
            onDoubleClickSlot += OnClickRight;
            onLongClickSlot += OnClickRight;
            onDragStart += OnDragStart;
            onDragEnd += OnDragEnd;
            onDragTo += OnDragTo;
            onPressAccept += OnClick;
            onPressUse += OnClickRight;
            onPressCancel += OnCancel;

            InitPanel();
        }

        protected override void Update()
        {
            base.Update();

            InitPanel(); //Try to init panel if its not already
        }

        public virtual void InitPanel()
        {
            if (!IsPlayerSet())
            {
                PlayerUI player_ui = GetComponentInParent<PlayerUI>();
                PlayerCharacter player = player_ui ? player_ui.GetPlayer() : PlayerCharacter.GetFirst();
                if (player != null && current_player == null)
                    current_player = player; //Set default player
            }
        }

        protected override void RefreshPanel()
        {
            base.RefreshPanel();

            InventoryData inventory = GetInventory();

            if (inventory != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    InventoryItemData invdata = inventory.GetInventoryItem(i);
                    ItemData idata = ItemData.Get(invdata?.item_id);
                    ItemSlot slot = (ItemSlot) slots[i];
                    if (invdata != null && idata != null)
                    {
                        slot.SetSlot(idata, invdata.quantity, selected_slot == slot.index || selected_right_slot == slot.index);
                        slot.SetDurability(idata.GetDurabilityPercent(invdata.durability), ShouldShowDurability(idata, invdata.durability));
                        slot.SetFilter(GetFilterLevel(idata, invdata.durability));
                    }
                    else if (i < inventory_size)
                    {
                        slot.SetSlot(null, 0, false);
                    }
                    else
                    {
                        slot.Hide();
                    }
                }

                ItemSlot sslot = GetSelectedSlot();
                if (sslot != null && sslot.GetItem() == null)
                    CancelSelection();
            }
        }

        protected bool ShouldShowDurability(ItemData idata, float durability)
        {
            int durabi = idata.GetDurabilityPercent(durability);
            return idata.HasDurability() && durabi < 100 && (idata.durability_type != DurabilityType.Spoilage || durabi <= 50);
        }

        protected int GetFilterLevel(ItemData idata, float durability)
        {
            int durabi = idata.GetDurabilityPercent(durability);
            if (idata.HasDurability() && durabi <= 40 && idata.durability_type == DurabilityType.Spoilage)
            {
                return durabi <= 20 ? 2 : 1;
            }
            return 0;
        }

        private void OnClick(UISlot uislot)
        {
            if (uislot != null)
            {
                //Cancel right click and action selector
                int previous_right_select = selected_right_slot;
                ActionSelectorUI.Get(GetPlayerID()).Hide();
                selected_right_slot = -1;

                int slot = uislot.index;
                ItemSlot selslot = GetSelectedSlotInAllPanels();

                //Cancel action selector
                if (slot == previous_right_select)
                {
                    CancelSelection();
                    return;
                }

                //Merge two slots
                ItemSlot islot = uislot as ItemSlot;
                if (islot != null && selslot != null)
                {
                    MergeSlots(selslot, islot);
                    if (onMergeSlot != null)
                        onMergeSlot.Invoke(selslot, islot);
                }
                //Select slot
                else if (islot.GetCraftable() != null)
                {
                    CancelSelectionAll();
                    selected_slot = slot;

                    ItemData idata = islot?.GetItem();
                    AAction aaction = idata?.FindAutoAction(GetPlayer(), islot);
                    aaction?.DoSelectAction(GetPlayer(), islot);

                    if (onSelectSlot != null)
                        onSelectSlot.Invoke(islot);
                }
            }
        }

        private void OnClickRight(UISlot uislot)
        {
            //Cancel select
            selected_slot = -1; 
            selected_right_slot = -1;
            ActionSelectorUI.Get(GetPlayerID()).Hide();

            //Run auto actions
            ItemSlot islot = uislot as ItemSlot;
            ItemData idata = islot?.GetItem();
            AAction aaction = idata?.FindAutoAction(GetPlayer(), islot);
            aaction?.DoAction(GetPlayer(), islot);

            //Show action selector
            if (idata != null && islot?.GetInventoryItem() != null && idata.actions.Length > 0)
            {
                selected_right_slot = islot.index;
                ActionSelectorUI.Get(GetPlayerID()).Show(islot);
            }
        }

        private void OnDragStart(UISlot slot)
        {
            CancelSelection();
        }

        private void OnDragEnd(UISlot aslot)
        {
            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            if (mouse.IsMouseOverUI())
                return;

            //Drag to selectable
            ItemSlot slot = aslot as ItemSlot;
            if (slot != null && slot.GetItem() != null)
            {
                PlayerCharacter player = GetPlayer();
                Selectable select = mouse.GetNearestRaycastList(mouse.GetPointingPos());
                MAction maction = slot.GetItem().FindMergeAction(select);
                if (player != null && maction != null 
                    && select.IsInUseRange(player) 
                    && maction.CanDoAction(player, slot, select))
                {
                    maction.DoAction(player, slot, select);
                }
            }
        }

        private void OnDragTo(UISlot slot, UISlot target)
        {
            if (slot != null && target != null)
            {
                ItemSlot islot = slot as ItemSlot;
                ItemSlot itarget = target as ItemSlot;
                MergeSlots(islot, itarget);
                if (onMergeSlot != null)
                    onMergeSlot.Invoke(islot, itarget);
            }
        }

        private void OnCancel(UISlot slot)
        {
            ItemSlotPanel.CancelSelectionAll();
            UISlotPanel.UnfocusAll();
        }

        public void SetInventory(InventoryType type, string uid, int size)
        {
            inventory_type = type;
            inventory_uid = uid;
            inventory_size = size;

            InventoryData idata = InventoryData.Get(type, uid);
            if(idata != null)
                idata.size = size;
        }

        public void SetPlayer(PlayerCharacter player)
        {
            current_player = player;
        }

        public int GetPlayerID()
        {
            return current_player ? current_player.player_id : 0;
        }

        public void MergeSlots(ItemSlot selected_slot, ItemSlot clicked_slot)
        {
            if (selected_slot != null && clicked_slot != null && current_player != null)
            {
                ItemSlot slot1 = selected_slot;
                ItemSlot slot2 = clicked_slot;
                ItemData item1 = slot1.GetItem();
                ItemData item2 = slot2.GetItem();
                
                if (slot1 == slot2)
                {
                    CancelSelection();
                    return;
                }

                //Check merge actions
                if (item1 != null && item2 != null)
                {
                    MAction action1 = item1.FindMergeAction(item2);
                    MAction action2 = item2.FindMergeAction(item1);

                    if (action1 != null && action1.CanDoAction(current_player, slot1, slot2))
                    {
                        DoMergeAction(action1, slot1, slot2);
                        return;
                    }

                    else if (action2 != null && action2.CanDoAction(current_player, slot2, slot1))
                    {
                        DoMergeAction(action2, slot2, slot1);
                        return;
                    }
                }

                //Move item
                MoveItem(slot1, slot2);
            }
        }

        private void DoMergeAction(MAction action, ItemSlot slot_action, ItemSlot slot_other)
        {
            if (slot_action == null || slot_other == null || current_player == null)
                return;

            action.DoAction(current_player, slot_action, slot_other);
            CancelPlayerSelection();
        }

        public void MoveItem(ItemSlot slot1, ItemSlot slot2)
        {
            ItemData item1 = slot1.GetItem();
            if (item1 == null || current_player == null)
                return;

            current_player.Inventory.MoveItem(slot1, slot2, limit_one_item);
            CancelPlayerSelection();
        }

        public void UseItem(ItemSlot slot, int quantity=1)
        {
            InventoryData inventory1 = slot.GetInventory();
            if (current_player != null && inventory1 != null)
                inventory1.RemoveItemAt(slot.index, quantity);
        }

        public void CancelSelection()
        {
            selected_slot = -1;
            selected_right_slot = -1;
        }

        public void CancelPlayerSelection()
        {
            CancelSelection();
            if (current_player != null)
            {
                PlayerUI player_ui = PlayerUI.Get(current_player.player_id);
                if (player_ui != null)
                    player_ui.CancelSelection();
            }
        }

        public bool HasSlotSelected()
        {
            return selected_slot >= 0;
        }

        public int GetSelectedSlotIndex()
        {
            return selected_slot;
        }

        public ItemSlot GetSlotByIndex(int slot_index)
        {
            foreach (ItemSlot slot in slots)
            {
                if (slot.index == slot_index)
                    return slot;
            }
            return null;
        }

        public ItemSlot GetSelectedSlot()
        {
            return GetSlotByIndex(selected_slot);
        }

        public Vector3 GetSlotWorldPosition(int slot)
        {
            ItemSlot islot = GetSlotByIndex(slot);
            if (islot != null)
            {
                RectTransform slotRect = islot.GetRect();
                return slotRect.position;
            }
            return Vector3.zero;
        }

        public string GetInventoryUID()
        {
            return inventory_uid;
        }

        public InventoryData GetInventory()
        {
            return InventoryData.Get(inventory_type, inventory_uid);
        }

        public bool IsInventorySet()
        {
            return inventory_type != InventoryType.None;
        }

        public bool IsPlayerSet()
        {
            return current_player != null;
        }

        public PlayerCharacter GetPlayer()
        {
            return current_player;
        }

        public static void CancelSelectionAll()
        {
            foreach (ItemSlotPanel panel in slot_panels)
                panel.CancelSelection();
        }

        public static ItemSlot GetSelectedSlotInAllPanels()
        {
            foreach (ItemSlotPanel panel in slot_panels)
            {
                ItemSlot slot = panel.GetSelectedSlot();
                if (slot != null)
                    return slot;
            }
            return null;
        }

        public static ItemSlot GetDragSlotInAllPanels()
        {
            foreach (ItemSlotPanel panel in slot_panels)
            {
                ItemSlot slot = panel.GetDragSlot();
                if (slot != null)
                    return slot;
            }
            return null;
        }

        public static ItemSlotPanel Get(InventoryType type)
        {
            foreach (ItemSlotPanel panel in slot_panels)
            {
                if (panel != null && panel.inventory_type == type)
                    return panel;
            }
            return null;
        }

        public static ItemSlotPanel Get(string inventory_uid)
        {
            foreach (ItemSlotPanel panel in slot_panels)
            {
                if (panel != null && panel.inventory_uid == inventory_uid)
                    return panel;
            }
            return null;
        }

        public static new List<ItemSlotPanel> GetAll()
        {
            return slot_panels;
        }
    }

}
