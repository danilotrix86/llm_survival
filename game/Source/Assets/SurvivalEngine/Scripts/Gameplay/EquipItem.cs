using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// An item that will appear on the player to display equipped item. Will be attached to a EquipAttach
    /// </summary>

    public class EquipItem : MonoBehaviour
    {
        public ItemData data;

        [Header("Weapons Anim")]
        public string attack_melee_anim = "Attack";
        public string attack_ranged_anim = "Shoot";

        [Header("Weapons Timing")]
        public bool override_timing = false; //If true, the character default windup/windout will be overriden by thefollowing values
        public float attack_windup = 0.7f;
        public float attack_windout = 0.4f;

        [Header("Children Mesh")]
        public GameObject child_left;
        public GameObject child_right;
        

        [HideInInspector]
        public EquipAttach target;
        [HideInInspector]
        public EquipAttach target_left;
        [HideInInspector]
        public EquipAttach target_right;

        private Vector3 start_scale;

        void Start()
        {
            start_scale = transform.localScale;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = target.transform.position;
            transform.rotation = target.transform.rotation;

            if (child_left == null && child_right == null)
            {
                transform.localScale = start_scale * target.scale;
            }

            if (child_right != null && target_right != null)
            {
                child_right.transform.position = target_right.transform.position;
                child_right.transform.rotation = target_right.transform.rotation;
                child_right.transform.localScale = start_scale * target_right.scale;
            }

            if (child_left != null && target_left != null)
            {
                child_left.transform.position = target_left.transform.position;
                child_left.transform.rotation = target_left.transform.rotation;
                child_left.transform.localScale = start_scale * target_left.scale;
            }

        }

        public PlayerCharacter GetCharacter()
        {
            if (target != null)
                return target.GetCharacter();
            return null;
        }
    }

}