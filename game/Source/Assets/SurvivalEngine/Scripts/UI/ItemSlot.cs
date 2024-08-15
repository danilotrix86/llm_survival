using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurvivalEngine
{

    

    /// <summary>
    /// Item slot that shows a single item, in your inventory or equipped bar
    /// </summary>

    public class ItemSlot : UISlot
    {
        [Header("Item Slot")]
        public Image icon;
        public Text value;
        public Text title;
        public Text dura;

        [Header("Extra")]
        public Image default_icon;
        public Image highlight;
        public Image filter;

        private Animator animator;

        private CraftData item;
        private int quantity;
        private float durability;

        private float highlight_opacity = 1f;

        protected override void Start()
        {
            base.Start();

            animator = GetComponent<Animator>();

            if (highlight)
            {
                highlight.enabled = false;
                highlight_opacity = highlight.color.a;
            }

            if (dura)
                dura.enabled = false;
        }

        protected override void Update()
        {
            base.Update();

            if (highlight != null)
            {
                highlight.enabled = selected || key_hover;
                float alpha = selected ? highlight_opacity : (highlight_opacity * 0.8f);
                highlight.color = new Color(highlight.color.r, highlight.color.g, highlight.color.b, alpha);
            }
        }

        public void SetSlot(CraftData item, int quantity, bool selected=false)
        {
            if (item != null)
            {
                CraftData prev = this.item;
                int prevq = this.quantity;
                this.item = item;
                this.quantity = quantity;
                this.durability = 0f;
                icon.sprite = item.icon;
                icon.enabled = true;
                value.text = quantity.ToString();
                value.enabled = quantity > 1;
                this.selected = selected;

                if (title != null)
                {
                    title.enabled = selected;
                    title.text = item.title;
                }

                if (default_icon != null)
                    default_icon.enabled = false;

                if (dura != null)
                    dura.enabled = false;
                if (filter != null)
                    filter.enabled = false;

                if (prev != item || prevq != quantity)
                    AnimateGain();
            }
            else
            {
                this.item = null;
                this.quantity = 0;
                this.durability = 0f;
                icon.enabled = false;
                value.enabled = false;
                this.selected = false;

                if (dura != null)
                    dura.enabled = false;

                if (filter != null)
                    filter.enabled = false;

                if (title != null)
                    title.enabled = false;

                if (default_icon != null)
                    default_icon.enabled = true;
            }

            Show();
        }

        public void SetSlotCustom(Sprite sicon, string title, int quantity, bool selected=false)
        {
            this.item = null;
            this.quantity = quantity;
            this.durability = 0f;
            icon.enabled = sicon != null;
            icon.sprite = sicon;
            value.text = quantity.ToString();
            value.enabled = quantity > 1;
            this.selected = selected;

            if (this.title != null)
            {
                this.title.enabled = selected;
                this.title.text = title;
            }

            if (dura != null)
                dura.enabled = false;

            if (filter != null)
                filter.enabled = false;

            if (default_icon != null)
                default_icon.enabled = false;

            Show();
        }

        public void ShowTitle()
        {
            if (this.title != null)
                this.title.enabled = true;
        }

        public void SetDurability(int durability, bool show_value)
        {
            this.durability = durability;

            if (dura != null)
            {
                dura.enabled = show_value;
                dura.text = durability.ToString() + "%";
            }
        }

        public void SetFilter(int filter_level)
        {
            if (filter != null)
            {
                filter.enabled = filter_level > 0;
                filter.color = filter_level >= 2 ? TheUI.Get().filter_red : TheUI.Get().filter_yellow;
            }
        }

        public void Select()
        {
            this.selected = true;
            if (this.title != null)
                this.title.enabled = true;
        }

        public void Unselect()
        {
            this.selected = false;
            if (this.title != null)
                this.title.enabled = false;
        }

        public void AnimateGain()
        {
            if (animator != null)
                animator.SetTrigger("Gain");
        }

        public CraftData GetCraftable()
        {
            return item;
        }

        public ItemData GetItem()
        {
            if (item != null)
                return item.GetItem();
            return null;
        }

        public int GetQuantity()
        {
            return quantity;
        }

        public float GetDurability()
        {
            return durability; //This returns the DISPLAY value in %, not the actual durability value
        }

        public string GetInventoryUID()
        {
            ItemSlotPanel parent_item = parent as ItemSlotPanel;
            return parent_item?.GetInventoryUID();
        }

        public InventoryData GetInventory()
        {
            ItemSlotPanel parent_item = parent as ItemSlotPanel;
            return parent_item?.GetInventory();
        }

        public InventoryItemData GetInventoryItem()
        {
            InventoryData inventory = GetInventory();
            if (inventory != null)
                return inventory.GetInventoryItem(index);
            return null;
        }

    }

}