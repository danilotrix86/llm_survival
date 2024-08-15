using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SurvivalEngine
{

    /// <summary>
    /// ActionSelector is the panel that popup and allow you to pick an action to do when you click on a selectable
    /// </summary>

    public class ActionSelector : UISlotPanel
    {
        private Animator animator;

        private PlayerCharacter character;
        private Selectable select;
        private Vector3 interact_pos;

        private static List<ActionSelector> selector_list = new List<ActionSelector>();

        protected override void Awake()
        {
            base.Awake();

            selector_list.Add(this);
            animator = GetComponent<Animator>();
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

            if (!IsVisible())
                return;

            if (character != null && select != null)
            {
                float dist = (interact_pos - character.transform.position).magnitude;
                if (dist > select.GetUseRange(character) * 1.2f)
                {
                    Hide();
                }
            }

            Vector3 dir = TheCamera.Get().GetFacingFront();
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            if (select == null)
                Hide();

            //Auto focus
            TheCamera cam = TheCamera.Get();
            bool gamepad = PlayerControls.IsAnyGamePad();
            UISlotPanel focus_panel = UISlotPanel.GetFocusedPanel();
            if (focus_panel != this && gamepad && !cam.IsFreeRotation())
                Focus();

            //Gamepad aiming
            PlayerControls controls = PlayerControls.Get();
            PlayerControlsMouse mcontrols = PlayerControlsMouse.Get();
            if (gamepad && cam.IsFreeRotation())
            {
                if (controls.IsPressAction())
                {
                    Vector3 mpos = mcontrols.GetCursorPosition();
                    List<RaycastResult> results = TheUI.RaycastAllUI(mpos);
                    List<GameObject> obj = new List<GameObject>();
                    foreach (RaycastResult res in results)
                    {
                        obj.Add(res.gameObject);
                    }
                    foreach (ActionSelectorButton button in slots)
                    {
                        if (button != null && obj.Contains(button.gameObject))
                            button.ClickSlot();
                    }
                }
            }
        }

        private void RefreshSelector()
        {
            foreach (ActionSelectorButton button in slots)
                button.Hide();

            if (select != null)
            {
                int index = 0;
                foreach (SAction action in select.actions)
                {
                    if (index < slots.Length && !action.IsAuto() && action.CanDoAction(character, select))
                    {
                        ActionSelectorButton button = (ActionSelectorButton) slots[index];
                        button.SetButton(action);
                        index++;
                    }
                }
            }
        }

        public void Show(PlayerCharacter character, Selectable select, Vector3 pos)
        {
            if (select != null && character != null)
            {
                if (!IsVisible() || this.select != select || this.character != character)
                {
                    this.select = select;
                    this.character = character;
                    RefreshSelector();
                    animator.Rebind();
                    //animator.SetTrigger("Show");
                    transform.position = pos;
                    interact_pos = pos;
                    gameObject.SetActive(true);
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
                select = null;
                character = null;
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
        }

        private void OnCancel(UISlot slot) {
            Hide();
        }

        public void OnClickAction(SAction action)
        {
            if (IsVisible())
            {
                if (action != null && select != null && character != null)
                {
                    character.FaceTorward(interact_pos);

                    if (action.CanDoAction(character, select))
                        action.DoAction(character, select);

                    Hide();
                }
            }
        }

        private void OnMouseClick(Vector3 pos)
        {
            Hide();
        }

        public Selectable GetSelectable()
        {
            return select;
        }

        public PlayerCharacter GetPlayer()
        {
            return character;
        }

        public static ActionSelector Get(int player_id=0)
        {
            foreach (ActionSelector panel in selector_list)
            {
                if (panel.character == null)
                {
                    panel.character = PlayerCharacter.Get(player_id); //Assign character
                }

                if (panel.character != null && panel.character.player_id == player_id)
                    return panel;
            }
            return null;
        }

        public static new List<ActionSelector> GetAll()
        {
            return selector_list;
        }
    }

}