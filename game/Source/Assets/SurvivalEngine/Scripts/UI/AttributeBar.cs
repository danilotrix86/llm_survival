using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Just a bar showing one of the attributes
    /// </summary>

    [RequireComponent(typeof(ProgressBar))]
    public class AttributeBar : MonoBehaviour
    {
        public AttributeType attribute;

        private PlayerUI parent_ui;
        private ProgressBar bar;

        void Awake()
        {
            parent_ui = GetComponentInParent<PlayerUI>();
            bar = GetComponent<ProgressBar>();
        }

        void Update()
        {
            PlayerCharacter character = GetPlayer();
            if (character != null)
            {
                bar.SetMax(Mathf.RoundToInt(character.Attributes.GetAttributeMax(attribute)));
                bar.SetValue(Mathf.RoundToInt(character.Attributes.GetAttributeValue(attribute)));
            }
        }

        public PlayerCharacter GetPlayer()
        {
            return parent_ui ? parent_ui.GetPlayer() : PlayerCharacter.GetFirst();
        }
    }

}