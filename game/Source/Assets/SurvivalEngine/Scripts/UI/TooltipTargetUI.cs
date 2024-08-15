using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SurvivalEngine
{

    public class TooltipTargetUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TooltipTargetType type;

        [Header("Custom")]
        public string title;
        [TextArea(5, 7)]
        public string desc;
        public Sprite icon;

        [Header("UI")]
        public float delay = 0.5f;
        public int text_size = 22;
        public int width = 400;
        public int height = 200;

        private ItemSlot slot;
        private Canvas canvas;
        private RectTransform rect;
        private float timer = 0f;
        private bool hover = false;

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            slot = GetComponent<ItemSlot>();
            rect = canvas.GetComponent<RectTransform>();
        }

        void Start()
        {

        }

        void Update()
        {
            if (TooltipPanel.Get() == null)
                return;

            if (hover && !TheGame.IsMobile())
            {
                timer += Time.deltaTime;
                if (timer > delay)
                {
                    if (type == TooltipTargetType.Custom)
                    {
                        SetTooltip(title, desc, icon);
                    }
                    else if(slot != null)
                    {
                        CraftData data = slot.GetCraftable();
                        SetTooltip(data);
                    }
                }
            }
        }

        private void SetTooltip(string title, string text, Sprite icon)
        {
            TooltipPanel.Get().Set(title, text, icon);
            TooltipPanel.Get().SetSize(width, height, text_size);
        }

        private void SetTooltip(CraftData data)
        {
            TooltipPanel.Get().Set(data);
            TooltipPanel.Get().SetSize(width, height, text_size);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            timer = 0f;
            hover = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            timer = 0f;
            hover = false;
        }

        void OnDisable()
        {
            hover = false;
        }

        public Canvas GetCanvas()
        {
            return canvas;
        }

        public RectTransform GetRect()
        {
            return rect;
        }

        public bool IsHover()
        {
            return hover;
        }
    }
}
