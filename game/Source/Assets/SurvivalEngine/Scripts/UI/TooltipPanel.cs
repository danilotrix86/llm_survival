using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalEngine {

    [RequireComponent(typeof(CanvasGroup))]
    public class TooltipPanel : UIPanel
    {
        public RectTransform box;
        public GameObject icon_group;
        public GameObject text_only_group;

        public Text title;
        public Image icon;
        public Text desc;

        public Text title2;
        public Text desc2;

        private RectTransform rect;
        private Selectable target = null;

        private int start_width;
        private int start_height;
        private int start_text_size;

        private static TooltipPanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            rect = GetComponent<RectTransform>();
            start_width = Mathf.RoundToInt(box.sizeDelta.x);
            start_height = Mathf.RoundToInt(box.sizeDelta.y);
            start_text_size = desc.fontSize;
        }

        protected override void Start()
        {
            base.Start();

        }

        protected override void Update()
        {
            base.Update();

            RefreshTooltip();

            if (target == null)
                Hide();
        }

        void RefreshTooltip()
        {
            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            rect.anchoredPosition = TheUI.Get().ScreenPointToCanvasPos(mouse.GetMousePosition());

            if (target != null)
            {
                if (!target.IsHovered() || mouse.IsMovingMouse())
                    Hide();
            }
        }

        private void UpdateAnchoring()
        {
            if (box != null)
            {
                PlayerControlsMouse mouse = PlayerControlsMouse.Get();
                Vector2 pos = TheUI.Get().ScreenPointToCanvasPos(mouse.GetMousePosition());
                Vector2 csize = TheUI.Get().GetCanvasSize() * 0.5f;
                float pivotX = Mathf.Sign(pos.x - csize.x * 0.5f) * 0.5f + 0.5f;
                float pivotY = Mathf.Sign(pos.y + csize.y * 0.5f) * 0.5f + 0.5f;
                box.pivot = new Vector2(pivotX, pivotY);
                box.anchoredPosition = Vector2.zero;
                rect.anchoredPosition = pos;
            }
        }

        public void Set(CraftData data)
        {
            Set(null, data);
        }

        public void Set(string atitle, string adesc, Sprite aicon)
        {
            Set(null, atitle, adesc, aicon);
        }

        public void Set(Selectable target, CraftData data)
        {
            if (data == null)
                return;

            this.target = target;

            if (title != null)
                title.text = data.title;
            if (icon != null)
                icon.sprite = data.icon;
            if (desc != null)
                desc.text = data.desc;

            if(title2 != null)
                title2.text = data.title;
            if (desc2 != null)
                desc2.text = data.desc;

            if (text_only_group != null)
                text_only_group.SetActive(data.icon == null);
            if (icon_group != null)
                icon_group.SetActive(data.icon != null);

            Show();
            UpdateAnchoring();
            RefreshTooltip();
        }

        public void Set(Selectable target, string atitle, string adesc, Sprite aicon)
        {
            this.target = target;

            if (title != null)
                title.text = atitle;
            if (icon != null)
                icon.sprite = aicon;
            if (desc != null)
                desc.text = adesc;

            if (title2 != null)
                title2.text = atitle;
            if (desc2 != null)
                desc2.text = adesc;

            if (text_only_group != null)
                text_only_group.SetActive(aicon == null);
            if (icon_group != null)
                icon_group.SetActive(aicon != null);

            Show();
            UpdateAnchoring();
            RefreshTooltip();
        }

        public void SetSize(int width, int height, int text)
        {
            box.sizeDelta = new Vector2(width, height);
            desc.fontSize = text;
        }

        public void ResetSize()
        {
            box.sizeDelta = new Vector2(start_width, start_height);
            desc.fontSize = start_text_size;
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            target = null;
        }

        public Selectable GetTarget()
        {
            return target;
        }

        public static TooltipPanel Get()
        {
            return _instance;
        }
    }

}
