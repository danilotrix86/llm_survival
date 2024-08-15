using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalEngine
{
    public class ActionProgress : MonoBehaviour
    {
        public Image fill;
        public float duration;

        [HideInInspector]
        public bool manual = false; //If true, set the value manually
        [HideInInspector]
        public float manual_value = 0f;

        private float timer = 0f;

        void Start()
        {

        }

        void Update()
        {
			if (TheGame.Get().IsPaused())
               return;
			
            Vector3 dir = TheCamera.Get().GetFacingFront();
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            if (manual)
            {
                fill.fillAmount = manual_value;
            }
            else
            {
                timer += Time.deltaTime;
                float value = timer / duration;
                fill.fillAmount = value;

                if (value > 1f)
                    Destroy(gameObject);
            }
        }
    }

}
