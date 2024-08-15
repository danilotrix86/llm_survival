using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{

    /// <summary>
    /// The top level crafting bar that contains all the crafting categories
    /// </summary>

    public class CraftPanel : UISlotPanel
    {
        [Header("Craft Panel")]
        public Animator animator;

        private PlayerUI parent_ui;

        private CraftStation current_staton = null;
        private int selected_slot = -1;
        private UISlot prev_slot;

        private List<GroupData> default_categories = new List<GroupData>();

        private static List<CraftPanel> panel_list = new List<CraftPanel>();

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this);
            parent_ui = GetComponentInParent<PlayerUI>();

            for (int i = 0; i < slots.Length; i++)
            {
                CategorySlot cslot = (CategorySlot)slots[i];
                if (cslot.group)
                    default_categories.Add(cslot.group);
            }

            if (animator != null)
                animator.SetBool("Visible", IsVisible());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            panel_list.Remove(this);
        }

        protected override void Start()
        {
            base.Start();

            PlayerControlsMouse.Get().onClick += (Vector3) => { CancelSubSelection(); };
            PlayerControlsMouse.Get().onRightClick += (Vector3) => { CancelSelection(); };

            onClickSlot += OnClick;
            onPressAccept += OnAccept;
            onPressCancel += OnCancel;

            RefreshCategories();
        }

        protected override void Update()
        {
            base.Update();

            PlayerControls controls = PlayerControls.Get(GetPlayerID());

            if (!controls.IsGamePad())
            {
                if (controls.IsPressAction() || controls.IsPressAttack())
                    CancelSubSelection();
            }
        }

        protected override void RefreshPanel()
        {
            base.RefreshPanel();

            PlayerCharacter player = GetPlayer();
            if (player != null)
            {
                CraftStation station = player.Crafting.GetCraftStation();
                if (current_staton != station)
                {
                    current_staton = station;
                    RefreshCategories();
                }
            }

            //Gamepad auto controls
            PlayerControls controls = PlayerControls.Get(GetPlayerID());
            CraftSubPanel sub_panel = CraftSubPanel.Get(GetPlayerID());
            UISlotPanel focus_panel = UISlotPanel.GetFocusedPanel();
            if (focus_panel != this && focus_panel != sub_panel && !TheUI.Get().IsBlockingPanelOpened()
                && controls.IsGamePad() && player != null && !player.Crafting.IsBuildMode())
            {
                Focus();
                CraftInfoPanel.Get(GetPlayerID())?.Hide();
            }
            if (focus_panel == this)
            {
                selection_index = Mathf.Clamp(selection_index, 0, CountActiveSlots() - 1);

                UISlot slot = GetSelectSlot();
                if (prev_slot != slot || !sub_panel.IsVisible())
                {
                    OnClick(slot);
                    sub_panel.selection_index = 0;
                    prev_slot = slot;
                }
            }
        }

        private void RefreshCategories()
        {
            foreach (CategorySlot slot in slots)
                slot.Hide();

            PlayerCharacter player = GetPlayer();
            if (player != null)
            {
                int index = 0;
                List<GroupData> groups = player.Crafting.GetCraftGroups();
                
                foreach (GroupData group in groups)
                {
                    if (index < slots.Length)
                    {
                        CategorySlot slot = (CategorySlot)slots[index];
                        List<CraftData> items = CraftData.GetAllCraftableInGroup(GetPlayer(), group);
                        if (items.Count > 0)
                        {
                            slot.SetSlot(group);
                            index++;
                        }
                    }
                }

                CraftSubPanel.Get(GetPlayerID())?.Hide();
            }
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);

            CancelSelection();
            if (animator != null)
                animator.SetBool("Visible", IsVisible());
            CraftSubPanel.Get(GetPlayerID())?.Hide();

            RefreshCategories();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);

            CancelSelection();
            if (animator != null)
                animator.SetBool("Visible", IsVisible());
            CraftSubPanel.Get(GetPlayerID())?.Hide();
        }

        private void OnClick(UISlot uislot)
        {
            if (uislot != null)
            {
                CategorySlot cslot = (CategorySlot)uislot;

                for (int i = 0; i < slots.Length; i++)
                    slots[i].UnselectSlot();

                if (cslot.group == CraftSubPanel.Get(GetPlayerID())?.GetCurrentCategory())
                {
                    CraftSubPanel.Get(GetPlayerID())?.Hide();
                }
                else
                {
                    selected_slot = uislot.index;
                    uislot.SelectSlot();
                    CraftSubPanel.Get(GetPlayerID())?.ShowCategory(cslot.group);
                }
            }
        }

        private void OnAccept(UISlot slot)
        {
            CraftSubPanel.Get(GetPlayerID())?.Focus();
        }

        private void OnCancel(UISlot slot)
        {
            Toggle();
            CraftSubPanel.Get(GetPlayerID())?.Hide();
            UISlotPanel.UnfocusAll();
        }

        public void CancelSubSelection()
        {
            CraftSubPanel.Get(GetPlayerID())?.CancelSelection();
        }

        public void CancelSelection()
        {
            selected_slot = -1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].UnselectSlot();
            }
            CancelSubSelection();
        }

        public int GetSelected()
        {
            return selected_slot;
        }

        public PlayerCharacter GetPlayer()
        {
            return parent_ui ? parent_ui.GetPlayer() : PlayerCharacter.GetFirst();
        }

        public int GetPlayerID()
        {
            PlayerCharacter player = GetPlayer();
            return player != null ? player.player_id : 0;
        }

        public static CraftPanel Get(int player_id=0)
        {
            foreach (CraftPanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer();
                if (player != null && player.player_id == player_id)
                    return panel;
            }
            return null;
        }

        public static new List<CraftPanel> GetAll()
        {
            return panel_list;
        }
    }

}
