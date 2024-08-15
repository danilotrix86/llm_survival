using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalEngine
{
    /// <summary>
    /// FX that shows possible merge actions on mobile
    /// </summary>

    public class ItemMergeFX : MonoBehaviour
    {
        public GameObject icon_group;
        public SpriteRenderer icon;
        public Text title;

        [HideInInspector]
        public Selectable target;

        private static ItemMergeFX _instance;

        void Awake()
        {
            _instance = this;
            icon_group.SetActive(false);
        }

        void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            if (icon_group.activeSelf)
                icon_group.SetActive(false);

            if (!target.IsActive()) {
                return;
            }

            transform.position = target.transform.position;
            transform.rotation = Quaternion.LookRotation(TheCamera.Get().transform.forward, Vector3.up);

            ItemSlot selected = ItemSlotPanel.GetSelectedSlotInAllPanels();
            if (selected != null && selected.GetItem() != null)
            {
                MAction action = selected.GetItem().FindMergeAction(target);
                foreach (PlayerCharacter player in PlayerCharacter.GetAll())
                {
                    if (player != null && action != null && action.CanDoAction(player, selected, target))
                    {
                        icon.sprite = selected.GetItem().icon;
                        title.text = action.title;
                        icon_group.SetActive(true);
                    }
                }
            }
        }


    }

}