using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{

    /// <summary>
    /// Main Inventory bar that list all items in your inventory
    /// </summary>

    public class InventoryPanel : ItemSlotPanel
    {
        private static List<InventoryPanel> panel_list = new List<InventoryPanel>();

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this);
            unfocus_when_out = true;

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].onPressKey += OnPressShortcut;
            }

            Hide(true);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            panel_list.Remove(this);
        }

        protected override void Start()
        {
            base.Start();
        }

        public override void InitPanel()
        {
            base.InitPanel();

            if (!IsInventorySet())
            {
                PlayerCharacter player = GetPlayer();
                if (player != null)
                {
                    bool has_inventory = PlayerData.Get().HasInventory(player.player_id);
                    if (has_inventory)
                    {
                        SetInventory(InventoryType.Inventory, player.InventoryData.uid, player.InventoryData.size);
                        SetPlayer(player);
                        Show(true);
                    }
                }
            }
        }

        private void OnPressShortcut(UISlot slot)
        {
            CancelSelection();
            PressSlot(slot.index);
        }

        public static InventoryPanel Get(int player_id=0)
        {
            foreach (InventoryPanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer();
                if (player != null && player.player_id == player_id)
                    return panel;
            }
            return null;
        }

        public static new List<InventoryPanel> GetAll()
        {
            return panel_list;
        }
    }

}