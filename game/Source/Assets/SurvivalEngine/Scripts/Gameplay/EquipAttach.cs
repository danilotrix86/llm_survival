using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// A location on a character to attach equipment (like hand, head, feet, ...)
    /// </summary>

    public class EquipAttach : MonoBehaviour
    {
        public EquipSlot slot;
        public EquipSide side;
        public float scale = 1f;

        private PlayerCharacter character;

        private void Awake()
        {
            character = GetComponentInParent<PlayerCharacter>();
        }

        public PlayerCharacter GetCharacter()
        {
            return character;
        }

    }

}