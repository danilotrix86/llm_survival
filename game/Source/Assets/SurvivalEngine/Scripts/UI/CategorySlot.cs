using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurvivalEngine
{

    /// <summary>
    /// A button slot for one of the crafting categories
    /// </summary>

    public class CategorySlot : UISlot
    {
        public GroupData group;
        public Image icon;
        public Image highlight;

        protected override void Start()
        {
            base.Start();

            if (group != null && group.icon != null)
                icon.sprite = group.icon;

            if (highlight)
                highlight.enabled = false;
        }

        protected override void Update()
        {
            base.Update();

            if (highlight != null)
                highlight.enabled = selected || key_hover;
        }

        public void SetSlot(GroupData group)
        {
            this.group = group;
            icon.sprite = group.icon;
            Show();
        }

    }

}