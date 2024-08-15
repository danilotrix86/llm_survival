using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalEngine {

    /// <summary>
    /// Generic Menu button script (for handling with keyboard/gamepad)
    /// </summary>

    public class MenuButton : UISlot
    {
        [Header("Menu Button")]
        public string group;
        public GameObject menu_arrow;
        public bool starting = false;

        private RectTransform arrow;

        private bool is_leader = false;
        private int selection = 0;
        private float height;
        private int start_index = 0;
        private List<MenuButton> group_list = new List<MenuButton>();

        private static List<MenuButton> button_list = new List<MenuButton>();

        protected override void Awake()
        {
            base.Awake();

            if (GetGroup(group).Count == 0)
                is_leader = true;

            button_list.Add(this);
            button = GetComponent<Button>();
            rect = GetComponent<RectTransform>();
            height = rect.anchoredPosition.y;

            if (menu_arrow != null)
            {
                GameObject arro = Instantiate(menu_arrow, transform);
                arrow = arro.GetComponent<RectTransform>();
                arrow.anchoredPosition = Vector2.left * (rect.sizeDelta.x * 0.5f + arrow.sizeDelta.x * 0.5f);
                arrow.gameObject.SetActive(false);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            button_list.Remove(this);
        }

        protected override void Start()
        {
            base.Start();

            if (is_leader)
            {
                group_list = GetGroup(group);
                group_list.Sort((p1, p2) =>
                {
                    return p2.height.CompareTo(p1.height);
                });

                for (int i = 0; i < group_list.Count; i++) {
                    group_list[i].index = i;
                    if (group_list[i].starting)
                        selection = i;
                }

                MenuButton start = GetGroupStart(group);
                start_index = start ? start.index : 0;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!PlayerControls.IsAnyGamePad())
                return;

            if (is_leader)
            {
                if (IsVisible())
                {
                    foreach (PlayerControls controls in PlayerControls.GetAll())
                    {
                        if (controls.IsMenuPressUp())
                        {
                            selection--;
                            selection = Mathf.Clamp(selection, 0, group_list.Count - 1);
                        }

                        if (controls.IsMenuPressDown())
                        {
                            selection++;
                            selection = Mathf.Clamp(selection, 0, group_list.Count - 1);
                        }

                        if (controls.IsPressMenuAccept())
                        {
                            if (selection >= 0 && selection < group_list.Count)
                            {
                                MenuButton button = group_list[selection];
                                button.Click();
                            }
                        }
                    }

                    foreach (MenuButton button in group_list)
                    {
                        button.SetArrow(selection == button.index);
                    }
                }
                else
                {
                    selection = start_index;
                }
            }
        }

        public void Click()
        {
            if(button.enabled && button.interactable)
                button.onClick.Invoke();
        }

        public void SetArrow(bool visible)
        {
            if (arrow != null)
                arrow.gameObject.SetActive(visible);
        }

        public static MenuButton GetGroupStart(string group)
        {
            foreach (MenuButton button in button_list)
            {
                if (button.group == group && button.starting)
                    return button;
            }
            return null;
        }

        public static List<MenuButton> GetGroup(string group)
        {
            List<MenuButton> valid_button = new List<MenuButton>();
            foreach (MenuButton button in button_list)
            {
                if (button.group == group)
                    valid_button.Add(button);
            }
            return valid_button;
        }
    }

}