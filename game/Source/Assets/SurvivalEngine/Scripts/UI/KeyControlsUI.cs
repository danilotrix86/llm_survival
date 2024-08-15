using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Allow keyboard/gamepad to control the UI
    /// </summary>

    public class KeyControlsUI : MonoBehaviour
    {
        public int player_id;

        public UISlotPanel default_top;
        public UISlotPanel default_down;
        public UISlotPanel default_left;
        public UISlotPanel default_right;

        private static List<KeyControlsUI> controls_ui_list = new List<KeyControlsUI>();

        void Awake()
        {
            controls_ui_list.Add(this);
        }

        private void OnDestroy()
        {
            controls_ui_list.Remove(this);
        }

        void Update()
        {
            PlayerControls controls = PlayerControls.Get(player_id);

            if (!controls.IsGamePad())
                return;

            if (controls.IsUIPressLeft())
                Navigate(Vector2.left);
            else if (controls.IsUIPressRight())
                Navigate(Vector2.right);
            else if(controls.IsUIPressUp())
                Navigate(Vector2.up);
            else if (controls.IsUIPressDown())
                Navigate(Vector2.down);

            //Stop navigate if out of focus
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel();
            if (selected_panel != null && !selected_panel.IsVisible())
                StopNavigate();
            else if (selected_panel != null && selected_panel.GetSelectSlot() != null && !selected_panel.GetSelectSlot().IsVisible())
                StopNavigate();

            //Controls
            if (controls.IsPressUISelect())
                OnPressSelect();

            if (controls.IsPressUIUse())
                OnPressUse();

            if (controls.IsPressUICancel())
                OnPressCancel();

            if (controls.IsPressAttack())
                OnPressAttack();
        }

        public void Navigate(Vector2 dir)
        {
            UISlotPanel selected_panel = GetFocusedPanel();
            Navigate(selected_panel, dir);
        }

        public void Navigate(UISlotPanel panel, Vector2 dir)
        {
            UISlot current = panel?.GetSelectSlot();
            if (panel == null || current == null)
            {
                if (IsLeft(dir))
                    panel = default_left;
                else if (IsRight(dir))
                    panel = default_right;
                else if (IsUp(dir))
                    panel = default_top;
                else if (IsDown(dir))
                    panel = default_down;
                panel.Focus();
            }
            else
            {
                if (IsLeft(dir) && current.left)
                    NavigateTo(current.left, dir);
                else if (IsRight(dir) && current.right)
                    NavigateTo(current.right, dir);
                else if (IsUp(dir) && current.top)
                    NavigateTo(current.top, dir);
                else if (IsDown(dir) && current.down)
                    NavigateTo(current.down, dir);
                else
                    NavigateAuto(panel, dir);

            }
        }

        public void NavigateAuto(UISlotPanel panel, Vector2 dir)
        {
            if (panel != null)
            {
                int slots_per_row = panel.slots_per_row;
                int prev_select = panel.selection_index;

                if (IsLeft(dir))
                    panel.selection_index--;
                else if (IsRight(dir))
                    panel.selection_index++;
                else if (IsUp(dir))
                    panel.selection_index -= slots_per_row;
                else if (IsDown(dir))
                    panel.selection_index += slots_per_row;

                if (panel.IsSelectedInvisible())
                    Navigate(panel, dir); //Continue same dir
                if (!panel.unfocus_when_out && !panel.IsSelectedValid())
                    panel.selection_index = prev_select; //Dont exit panel, set to previous
                if (panel.unfocus_when_out && !panel.IsSelectedValid())
                    UISlotPanel.UnfocusAll(); //Exit panel
            }
        }

        public void NavigateTo(UISlot slot, Vector2 dir)
        {
            UISlotPanel panel = slot?.GetParent();
            if (panel != null && panel.IsVisible())
            {
                panel.Focus();
                panel.selection_index = slot.index;
            }
            else
            {
                Navigate(panel, dir);
            }
        }

        public void StopNavigate()
        {
            ActionSelector.Get(player_id)?.Hide();
            ActionSelectorUI.Get(player_id)?.Hide();
            UISlotPanel.UnfocusAll();
        }

        private void OnPressSelect()
        {
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel();
            UISlot selected_slot = selected_panel?.GetSelectSlot();
            if (selected_slot != null)
            {
                selected_slot.KeyPressAccept();
            }

            if (ReadPanel.Get().IsFullyVisible())
                ReadPanel.Get().Hide();

        }

        private void OnPressUse()
        {
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel();
            UISlot selected_slot = selected_panel?.GetSelectSlot();
            if (selected_slot != null)
            {
                selected_slot.KeyPressUse();
            }

        }

        private void OnPressCancel()
        {
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel();
            UISlot selected_slot = selected_panel?.GetSelectSlot();
            if (selected_slot != null)
            {
                selected_slot.KeyPressCancel();
            }

            if (ReadPanel.Get().IsVisible())
                ReadPanel.Get().Hide();

        }

        private void OnPressAttack()
        {
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel();
            if (selected_panel is InventoryPanel || selected_panel is EquipPanel)
            {
                ItemSlotPanel.CancelSelectionAll();
                UISlotPanel.UnfocusAll();
            }
        }

        public UISlot GetSelectedSlot()
        {
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel();
            UISlot selected_slot = selected_panel?.GetSelectSlot();
            return selected_slot;
        }

        public UISlotPanel GetFocusedPanel()
        {
            return UISlotPanel.GetFocusedPanel();
        }

        public int GetSelectedIndex()
        {
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel();
            if (selected_panel != null)
                return selected_panel.selection_index;
            return -1;
        }

        /*public bool IsCraftPanelFocus()
        {
            if (selected_panel == null)
                return false;
            return selected_panel == CraftPanel.Get(player_id) || selected_panel == CraftSubPanel.Get(player_id) || selected_panel == CraftInfoPanel.Get(player_id);
        }*/

        public bool IsActionSelector()
        {
            return !ActionSelector.Get().IsFullyHidden() || !ActionSelectorUI.Get().IsFullyHidden();
        }

        public bool IsPanelFocus()
        {
            return GetFocusedPanel() != null || IsActionSelector();
        }

        public bool IsPanelFocusItem()
        {
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel();
            UISlot slot = selected_panel?.GetSelectSlot();
            ItemSlot islot = (slot != null && slot is ItemSlot) ? (ItemSlot)slot : null;
            return islot != null && islot.GetItem() != null;
        }

        public bool IsGamePad()
        {
            PlayerControls controls = PlayerControls.Get(player_id);
            return controls ? controls.IsGamePad() : false;
        }

        public bool IsLeft(Vector2 dir) { return dir.x < -0.1f; }
        public bool IsRight(Vector2 dir) { return dir.x > 0.1f; }
        public bool IsDown(Vector2 dir) { return dir.y < -0.1f; }
        public bool IsUp(Vector2 dir) { return dir.y > 0.1f; }

        public static KeyControlsUI Get(int player_id=0)
        {
            foreach (KeyControlsUI panel in controls_ui_list)
            {
                if (panel.player_id == player_id)
                    return panel;
            }
            return null;
        }

        public static List<KeyControlsUI> GetAll()
        {
            return controls_ui_list;
        }
    }

}