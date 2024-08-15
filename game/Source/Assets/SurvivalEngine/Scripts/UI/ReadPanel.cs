using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalEngine
{
    
    public class ReadPanel : UIPanel
    {
        public int panel_id = 0;
        public Text title;
        public Text desc;
        public Image image;
    
        private static Dictionary<int, ReadPanel> panel_list = new Dictionary<int, ReadPanel>();

        protected override void Awake()
        {
            base.Awake();
            panel_list[panel_id] = this;
        }

        private void OnDestroy()
        {
            if (panel_list.ContainsKey(panel_id))
                panel_list.Remove(panel_id);
        }

        protected override void Update()
        {
            base.Update();

        }

        public void ShowPanel(string title, string desc)
        {
            this.title.text = title;
            if (this.desc != null)
                this.desc.text = desc;
            if (this.image != null)
                image.enabled = false;

            Show();
        }

        public void ShowPanel(string title, Sprite sprite)
        {
            this.title.text = title;
            if (this.desc != null)
                this.desc.text = "";

            if (this.image != null)
            {
                image.enabled = true;
                image.sprite = sprite;
            }

            Show();
        }

        public void ClickOK()
        {
            Hide();
        }

        public static ReadPanel Get(int id=0)
        {
            if (panel_list.ContainsKey(id))
                return panel_list[id];
            return null;
        }

        public static bool IsAnyVisible()
        {
            foreach (KeyValuePair<int, ReadPanel> pair in panel_list)
            {
                if (pair.Value.IsVisible())
                    return true;
            }
            return false;
        }
    }

}
