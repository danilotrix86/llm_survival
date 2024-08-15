using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Add this script to the player to be able to climb ladders
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterClimb : MonoBehaviour
    {
        public float climb_speed = 2f;
        public float climb_offset = 0.5f;

        private PlayerCharacter character;
        private Ladder climb_ladder = null;
        private bool climbing = false;
        private Vector3 move_vect;
        private Vector3 current_offset;

        private bool auto_move = false;
        private float auto_move_height = 0f;
        private float climb_timer = 0f;

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
        }

        private void Start()
        {
            PlayerControlsMouse controls = PlayerControlsMouse.Get();
            controls.onClickFloor += OnClickFloor;
        }

        void Update()
        {
            climb_timer += Time.deltaTime;

            if (IsClimbing())
            {
                //Climb up and down
                PlayerControls controls = PlayerControls.Get();
                Vector3 cmove = controls.GetMove();
                move_vect = Vector3.zero;
                if (cmove.magnitude > 0.1f)
                {
                    transform.position += Vector3.up * cmove.y * climb_speed * Time.deltaTime;
                    move_vect = Vector3.up * cmove.y * climb_speed;
                    auto_move = false;
                }

                //Auto move
                if (auto_move)
                {
                    Vector3 dir = Vector3.up * (auto_move_height - transform.position.y);
                    if (dir.magnitude > 0.1f)
                    {
                        transform.position += dir.normalized * climb_speed * Time.deltaTime;
                        move_vect = dir.normalized * climb_speed;
                    }
                }

                //Face ladder
                character.FaceTorward(transform.position - current_offset);

                //Snap to ladder
                if (climb_ladder != null)
                    transform.position = new Vector3(climb_ladder.transform.position.x, transform.position.y, climb_ladder.transform.position.z) + current_offset;

                //Stop climb with button
                if ((controls.IsPressAction() || controls.IsPressJump()) && climb_timer > 0.5f)
                    StopClimb();

                //Reach bottom and top
                if (character.IsGrounded() && move_vect.y < -0.1f)
                    StopClimb();
                if (climb_ladder != null && character.transform.position.y < climb_ladder.GetBounds().min.y && move_vect.y < -0.1f)
                    StopClimb();
                if (climb_ladder != null && character.transform.position.y > climb_ladder.GetBounds().max.y && move_vect.y > 0.1f)
                    StopClimbTop();
                if (climb_ladder == null)
                    StopClimb();
            }
        }

        private void OnClickFloor(Vector3 pos) {
            if (IsClimbing())
            {
                auto_move = true;
                auto_move_height = pos.y;
                if (auto_move_height > transform.position.y)
                    auto_move_height += 1f;
                if (auto_move_height < transform.position.y)
                    auto_move_height -= 1f;
            }
        }

        public void Climb(Ladder ladder)
        {
            if (ladder != null && !IsClimbing() && character.IsMovementEnabled() && climb_timer > 0.5f)
            {
                climb_ladder = ladder;
                character.StopMove();
                character.DisableMovement();
                Vector3 dir = ladder.GetOffsetDir(transform.position);
                if (ladder.IsSideBlocked(dir) && !ladder.IsSideBlocked(-dir))
                    dir = -dir; //Reverse offset if direction blocked
                current_offset = dir.normalized * climb_offset;
                transform.position = new Vector3(ladder.transform.position.x, transform.position.y, ladder.transform.position.z) + current_offset;
                transform.rotation = Quaternion.LookRotation(-current_offset, Vector3.up);
                auto_move = false;
                climbing = true;
                climb_timer = 0f;
            }
        }

        public void StopClimb()
        {
            if (IsClimbing())
            {
                climb_ladder = null;
                character.EnableMovement();
                character.StopMove();
                character.FaceTorward(transform.position - current_offset);
                auto_move = false;
                climbing = false;
                climb_timer = 0f;
            }
        }

        public void StopClimbTop()
        {
            if (IsClimbing())
            {
                Vector3 dir = climb_ladder.GetOffsetDir(transform.position);
                Vector3 jump_offset = Quaternion.LookRotation(-dir, Vector3.up) * climb_ladder.top_jump_offset;

                StopClimb();

                transform.position += jump_offset;
            }
        }

        public bool IsMoving()
        {
            return move_vect.magnitude > 0.1f;
        }

        public bool IsClimbing()
        {
            return climbing;
        }
    }
}
