using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    [RequireComponent(typeof(Selectable))]
    public class Door : MonoBehaviour
    {
        private Selectable select;
        private Animator animator;
        private Collider collide;

        private bool opened = false;

        void Start()
        {
            select = GetComponent<Selectable>();
            animator = GetComponentInChildren<Animator>();
            collide = GetComponentInChildren<Collider>();
            select.onUse += OnUse;
        }

        void OnUse(PlayerCharacter character)
        {
            opened = !opened;

            if (collide != null)
                collide.isTrigger = opened;

            if(animator != null)
                animator.SetBool("Open", opened);
        }
    }

}
