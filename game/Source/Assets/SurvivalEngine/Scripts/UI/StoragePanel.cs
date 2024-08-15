using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    /// <summary>
    /// Main UI panel for storages boxes (chest)
    /// </summary>

    public class StoragePanel : ItemSlotPanel
    {
        private static List<StoragePanel> panel_list = new List<StoragePanel>();

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this);
            unfocus_when_out = true;

            onSelectSlot += OnSelectSlot;
            onMergeSlot += OnMergeSlot;
            onPressCancel += OnCancel;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            panel_list.Remove(this);
        }

        protected override void Update()
        {
            base.Update();

            PlayerControls controls = PlayerControls.Get();
            if (IsVisible() && controls.IsPressMenuCancel())
                Hide();
        }

        protected override void RefreshPanel()
        {
            base.RefreshPanel();

            //Hide if too far
            Selectable select = Selectable.GetByUID(inventory_uid);
            PlayerCharacter player = GetPlayer();
            if (IsVisible() && player != null && select != null)
            {
                float dist = (select.transform.position - player.transform.position).magnitude;
                if (dist > select.GetUseRange(player) * 1.2f)
                {
                    Hide();
                }
            } 
        }

        public void ShowStorage(PlayerCharacter player, string uid, int max)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                SetInventory(InventoryType.Storage, uid, max);
                SetPlayer(player);
                RefreshPanel();
                Show();
            }
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            SetInventory(InventoryType.Storage, "", 0);
            CancelSelection();
        }

        private void OnSelectSlot(ItemSlot islot)
        {

        }

        private void OnMergeSlot(ItemSlot clicked_slot, ItemSlot selected_slot)
        {
           
        }

        private void OnCancel(UISlot slot)
        {
            Hide();
        }

        public string GetStorageUID()
        {
            return inventory_uid;
        }

        public static StoragePanel Get(int player_id=0)
        {
            foreach (StoragePanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer();
                if (player != null && player.player_id == player_id)
                    return panel;
            }
            return null;
        }

        public static bool IsAnyVisible()
        {
            foreach (StoragePanel panel in panel_list)
            {
                if (panel.IsVisible())
                    return true;
            }
            return false;
        }

        public static new List<StoragePanel> GetAll()
        {
            return panel_list;
        }
    }

}