using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    public enum TooltipTargetType
    {
        Automatic = 0,
        Custom = 10,
    }

    [RequireComponent(typeof(Selectable))]
    public class TooltipTarget : MonoBehaviour
    {
        public TooltipTargetType type;

        [Header("Custom")]
        public string title;
        public Sprite icon;
        [TextArea(3, 5)]
        public string text;

        [Header("UI")]
        public int text_size = 22;
        public int width = 400;
        public int height = 200;

        private Selectable select;
        private Construction construct;
        private Plant plant;
        private Item item;
        private Character character;

        void Awake()
        {
            select = GetComponent<Selectable>();

            construct = GetComponent<Construction>();
            plant = GetComponent<Plant>();
            item = GetComponent<Item>();
            character = GetComponent<Character>();
        }

        void Update()
        {
            if (TooltipPanel.Get() == null)
                return;
            if (TheGame.IsMobile())
                return;

            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            if (select.IsHovered() && !mouse.IsMovingMouse(0.25f) && TooltipPanel.Get().GetTarget() != select)
            {

                if (type == TooltipTargetType.Custom)
                {
                    SetTooltip(select, title, text, icon);
                }
                else
                {
                    if (construct != null)
                        SetTooltip(select, construct.data);
                    else if (plant != null)
                        SetTooltip(select, plant.data);
                    else if (item != null)
                        SetTooltip(select, item.data);
                    else if (character != null)
                        SetTooltip(select, character.data);
                    else
                        SetTooltip(select, title, text, icon);
                }
            }
        }

        private void SetTooltip(Selectable target, string title, string text, Sprite icon)
        {
            TooltipPanel.Get().Set(target, title, text, icon);
            TooltipPanel.Get().SetSize(width, height, text_size);
        }

        private void SetTooltip(Selectable target, CraftData data)
        {
            TooltipPanel.Get().Set(target, data);
            TooltipPanel.Get().SetSize(width, height, text_size);
        }
    }

}