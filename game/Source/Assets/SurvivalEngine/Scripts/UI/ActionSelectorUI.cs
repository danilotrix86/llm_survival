using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// ActionSelectorUI is similar to ActionSelector, but for items in the player's inventory in the UI Canvas.
    /// </summary>

    public class ActionSelectorUI : UISlotPanel
    {
        private Animator animator;

        private PlayerUI parent_ui;
        private ItemSlot slot;

        private UISlotPanel prev_panel = null;

        private static List<ActionSelectorUI> selector_list = new List<ActionSelectorUI>();

        protected override void Awake()
        {
            base.Awake();

            selector_list.Add(this);
            animator = GetComponent<Animator>();
            parent_ui = GetComponentInParent<PlayerUI>();
            gameObject.SetActive(false);

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            selector_list.Remove(this);
        }

        protected override void Start()
        {
            base.Start();

            //PlayerControlsMouse.Get().onClick += OnMouseClick;
            PlayerControlsMouse.Get().onRightClick += OnMouseClick;

            onClickSlot += OnClick;
            onPressAccept += OnAccept;
            onPressCancel += OnCancel;
            onPressUse += OnCancel;
        }

        protected override void Update()
        {
            base.Update();

            //Auto focus
            UISlotPanel focus_panel = UISlotPanel.GetFocusedPanel();
            if (focus_panel != this && IsVisible() && PlayerControls.IsAnyGamePad())
            {
                prev_panel = focus_panel;
                Focus();
            }
        }

        private void RefreshSelector()
        {
            PlayerCharacter character = GetPlayer();

            foreach (ActionSelectorButton button in slots)
                button.Hide();

            if (slot != null)
            {
                int index = 0;
                foreach (SAction action in slot.GetItem().actions)
                {
                    if (index < slots.Length && !action.IsAuto() && action.CanDoAction(character, slot))
                    {
                        ActionSelectorButton button = (ActionSelectorButton) slots[index];
                        button.SetButton(action);
                        index++;
                    }
                }
            }
        }

        public void Show(ItemSlot slot)
        {
            PlayerCharacter character = GetPlayer();
            if (slot != null && character != null)
            {
                if (!IsVisible() || this.slot != slot)
                {
                    this.slot = slot;
                    RefreshSelector();
                    //animator.SetTrigger("Show");
                    transform.position = slot.transform.position;
                    gameObject.SetActive(true);
                    animator.Rebind();
                    animator.SetBool("Solo", CountActiveSlots() == 1);
                    selection_index = 0;
                    Show();
                }
            }
        }

        public override void Hide(bool instant = false)
        {
            if (IsVisible())
            {
                base.Hide(instant);
                animator.SetTrigger("Hide");
            }
        }

        private void OnClick(UISlot islot)
        {
            ActionSelectorButton button = (ActionSelectorButton)islot;
            OnClickAction(button.GetAction());
        }

        private void OnAccept(UISlot slot)
        {
            OnClick(slot);
            UISlotPanel.UnfocusAll();
            if (prev_panel != null)
                prev_panel.Focus();
        }

        private void OnCancel(UISlot slot)
        {
            ItemSlotPanel.CancelSelectionAll();
            Hide();
        }

        public void OnClickAction(SAction action)
        {
            if (IsVisible())
            {
                PlayerCharacter character = GetPlayer();
                if (action != null && slot != null && character != null)
                {
                    ItemSlot aslot = slot;

                    PlayerUI.Get(character.player_id)?.CancelSelection();
                    Hide();

                    if (action.CanDoAction(character, aslot))
                        action.DoAction(character, aslot);

                    
                }
            }
        }

        private void OnMouseClick(Vector3 pos)
        {
            Hide();
        }

        public PlayerCharacter GetPlayer()
        {
            return parent_ui ? parent_ui.GetPlayer() : PlayerCharacter.GetFirst();
        }

        public static ActionSelectorUI Get(int player_id=0)
        {
            foreach (ActionSelectorUI panel in selector_list)
            {
                PlayerCharacter player = panel.GetPlayer();
                if (player != null && player.player_id == player_id)
                    return panel;
            }
            return null;
        }

        public static new List<ActionSelectorUI> GetAll()
        {
            return selector_list;
        }
    }

}