using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalEngine
{

    /// <summary>
    /// A filled bar that shows a value (like attributes)
    /// </summary>

    public class ProgressBar : MonoBehaviour
    {
        public Image bar_fill;
        public Text bar_text;

        private int max_value = 100;
        private int min_value = 0;

        private int target_value;
        private int current_value;
        private int start_value;
        private float current_value_float;
        private float timer = 0f;

        void Start()
        {
            start_value = current_value;
        }

        void Update()
        {
            if (current_value != target_value)
            {
                timer += Time.deltaTime;
                float val = Mathf.Clamp01(timer / 2f);
                current_value_float = start_value * (1f - val) + target_value * val;
                current_value = Mathf.RoundToInt(current_value_float);
            }
            else
            {
                start_value = current_value;
            }

            bar_fill.fillAmount = (current_value_float - min_value) / (float)(max_value - min_value);

            if (bar_text != null)
            {
                bar_text.text = current_value.ToString();
            }
        }

        public void SetMax(int val)
        {
            max_value = val;
        }

        public void SetMin(int val)
        {
            min_value = val;
        }

        public void SetValue(int val)
        {
            target_value = val;
            current_value = val;
            current_value_float = val;
            start_value = val;
            timer = 0f;
        }

        public void SetValueRoll(int val)
        {
            target_value = val;
            timer = 0f;
        }

    }

}
