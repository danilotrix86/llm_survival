using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalEngine
{

    public class BlackPanel : UIPanel
    {
        public Text text;

        private static BlackPanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            if (this.text != null)
                this.text.text = "";
        }

        public void ShowText(string text, bool instant = false)
        {
            if(this.text != null)
                this.text.text = text;
            Show(instant);
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            if (this.text != null)
                this.text.text = "";
        }

        public static BlackPanel Get()
        {
            return _instance;
        }
    }

}
