using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Manages all character animations
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterAnim : MonoBehaviour
    {
        public string move_anim = "Move";
        public string move_side_x = "MoveX";
        public string move_side_z = "MoveZ";
        public string attack_anim = "Attack";
        public string attack_speed = "AttackSpeed";
        public string take_anim = "Take";
        public string craft_anim = "Craft";
        public string build_anim = "Build";
        public string use_anim = "Use";
        public string damaged_anim = "Damaged";
        public string death_anim = "Death";
        public string sleep_anim = "Sleep";
        public string fish_anim = "Fish";
        public string dig_anim = "Dig";
        public string water_anim = "Water";
        public string hoe_anim = "Hoe";
        public string ride_anim = "Ride";
        public string swim_anim = "Swim";
        public string climb_anim = "Climb";

        private PlayerCharacter character;
        private Animator animator;

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            animator = GetComponentInChildren<Animator>();

            if (animator == null)
                enabled = false;
        }

        private void Start()
        {
            character.Inventory.onTakeItem += OnTake;
            character.Inventory.onDropItem += OnDrop;
            character.Crafting.onCraft += OnCraft;
            character.Crafting.onBuild += OnBuild;
            character.Combat.onAttack += OnAttack;
            character.Combat.onAttackHit += OnAttackHit;
            character.Combat.onDamaged += OnDamaged;
            character.Combat.onDeath += OnDeath;
            character.onTriggerAnim += OnTriggerAnim;

            if (character.Jumping)
                character.Jumping.onJump += OnJump;
        }

        void Update()
        {
            bool player_paused = TheGame.Get().IsPausedByPlayer();
            bool gameplay_paused = TheGame.Get().IsPausedByScript();
            animator.enabled = !player_paused;

            if (animator.enabled)
            {
                SetAnimBool(move_anim, !gameplay_paused && character.IsMoving());
                SetAnimBool(craft_anim, !gameplay_paused && character.Crafting.IsCrafting());
                SetAnimBool(sleep_anim, character.IsSleeping());
                SetAnimBool(fish_anim, character.IsFishing());
                SetAnimBool(ride_anim, character.IsRiding());
                SetAnimBool(swim_anim, character.IsSwimming());
                SetAnimBool(climb_anim, character.IsClimbing());

                Vector3 move_vect = character.GetMoveNormalized();
                float mangle = Vector3.SignedAngle(character.GetFacing(), move_vect, Vector3.up);
                Vector3 move_side = new Vector3(Mathf.Sin(mangle * Mathf.Deg2Rad), 0f, Mathf.Cos(mangle * Mathf.Deg2Rad));
                move_side = move_side * move_vect.magnitude;
                SetAnimFloat(move_side_x, move_side.x);
                SetAnimFloat(move_side_z, move_side.z);
            }
        }

        public void SetAnimBool(string id, bool value)
        {
            if (!string.IsNullOrEmpty(id))
                animator.SetBool(id, value);
        }

        public void SetAnimFloat(string id, float value)
        {
            if (!string.IsNullOrEmpty(id))
                animator.SetFloat(id, value);
        }

        public void SetAnimTrigger(string id)
        {
            if (!string.IsNullOrEmpty(id))
                animator.SetTrigger(id);
        }

        private void OnTake(Item item)
        {
            SetAnimTrigger(take_anim);
        }

        private void OnDrop(Item item)
        {
            //Add drop anim here
        }

        private void OnCraft(CraftData cdata)
        {
            //Add craft anim here
        }

        private void OnBuild(Buildable construction)
        {
            SetAnimTrigger(build_anim);
        }

        private void OnJump()
        {
            //Add jump animation here
        }

        private void OnDamaged()
        {
            SetAnimTrigger(damaged_anim);
        }

        private void OnDeath()
        {
            SetAnimTrigger(death_anim);
        }

        private void OnAttack(Destructible target, bool ranged)
        {
            string anim = attack_anim;
            float anim_speed = character.Combat.GetAttackAnimSpeed();

            //Replace anim based on current equipped item
            EquipItem equip = character.Inventory.GetEquippedWeaponMesh();
            if (equip != null)
            {
                if (!ranged && !string.IsNullOrEmpty(equip.attack_melee_anim))
                    anim = equip.attack_melee_anim;
                if (ranged && !string.IsNullOrEmpty(equip.attack_ranged_anim))
                    anim = equip.attack_ranged_anim;
            }

            SetAnimFloat(attack_speed, anim_speed);
            SetAnimTrigger(anim);
        }

        private void OnAttackHit(Destructible target)
        {

        }

        private void OnTriggerAnim(string anim, float duration)
        {
            SetAnimTrigger(anim);
        }
    }

}