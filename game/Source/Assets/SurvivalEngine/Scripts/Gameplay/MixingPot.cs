using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine {

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class MixingPot : MonoBehaviour
    {
        public ItemData[] recipes;
        public int max_items = 6;
        public bool clear_on_mix = false;

        private Selectable select;

        void Start()
        {
            select = GetComponent<Selectable>();

            select.onUse += OnUse;
        }

        private void OnUse(PlayerCharacter player)
        {
            if (!string.IsNullOrEmpty(select.GetUID()))
            {
                MixingPanel.Get().ShowMixing(player, this, select.GetUID());
            }
            else
            {
                Debug.LogError("You must generate the UID to use the mixing pot feature.");
            }
        }

        public Selectable GetSelectable()
        {
            return select;
        }
    }

}
