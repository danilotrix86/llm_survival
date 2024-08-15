using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Animation open/close for the chest
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    public class ChestAnim : MonoBehaviour
    {
        public Transform chest_lid;
        public Transform chest_lid_outline;

        private Quaternion start_rot;
        private Selectable select;

        void Start()
        {
            select = GetComponent<Selectable>();
            start_rot = chest_lid.localRotation;
        }

        void Update()
        {
            ItemSlotPanel chest_panel = ItemSlotPanel.Get(select.GetUID());
            bool open = chest_panel != null && chest_panel.IsVisible();
            Quaternion target = open ? Quaternion.Euler(-90f, 0f, 0f) * start_rot : start_rot;
            chest_lid.localRotation = Quaternion.Slerp(chest_lid.localRotation, target, 10f * Time.deltaTime);
            if (chest_lid_outline != null)
                chest_lid_outline.localRotation = Quaternion.Slerp(chest_lid_outline.localRotation, target, 10f * Time.deltaTime);
        }
    }

}