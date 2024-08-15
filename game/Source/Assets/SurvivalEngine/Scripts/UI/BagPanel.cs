using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    /// <summary>
    /// Main UI panel for storages boxes (chest)
    /// </summary>

    public class BagPanel : ItemSlotPanel
    {
        private static List<BagPanel> panel_list = new List<BagPanel>();

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this);
            unfocus_when_out = true;

            onSelectSlot += OnSelectSlot;
            onMergeSlot += OnMergeSlot;
        }

        public void ShowBag(PlayerCharacter player, string uid, int max)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                SetInventory(InventoryType.Bag, uid, max);
                SetPlayer(player);
                SetVisible(true);
            }
        }

        public void HideBag()
        {
            SetInventory(InventoryType.Bag, "", 0);
            SetVisible(false);
        }

        private void OnSelectSlot(ItemSlot islot)
        {

        }

        private void OnMergeSlot(ItemSlot clicked_slot, ItemSlot selected_slot)
        {
            
        }

        public string GetStorageUID()
        {
            return inventory_uid;
        }

        public static BagPanel Get(int player_id=0)
        {
            foreach (BagPanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer();
                if (player != null && player.player_id == player_id)
                    return panel;
            }
            return null;
        }

        public static new List<BagPanel> GetAll()
        {
            return panel_list;
        }
    }

}