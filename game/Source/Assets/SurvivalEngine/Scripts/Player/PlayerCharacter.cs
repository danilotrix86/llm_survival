using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace SurvivalEngine
{

    public enum PlayerInteractBehavior
    {
        MoveAndInteract = 0, //When clicking on object, will auto move to it, then interact with it
        InteractOnly = 10, //When clicking on object, will interact only if in range (will not auto move)
    }

    /// <summary>
    /// Main character script, contains code for movement and for player controls/commands
    /// </summary>

    [RequireComponent(typeof(PlayerCharacterCombat))]
    [RequireComponent(typeof(PlayerCharacterAttribute))]
    [RequireComponent(typeof(PlayerCharacterInventory))]
    [RequireComponent(typeof(PlayerCharacterCraft))]
    public class PlayerCharacter : MonoBehaviour
    {
        public int player_id = 0;

        [Header("Movement")]
        public bool move_enabled = true; //Disable this if you want to use your own character controller
        public float move_speed = 4f;
        public float move_accel = 8; //Acceleration
        public float rotate_speed = 180f;
        public float fall_speed = 20f; //Falling speed
        public float fall_gravity = 40f; //Falling acceleration
        public float slope_angle_max = 45f; //Maximum angle, in degrees that the character can climb up
        public float moving_threshold = 0.15f; //Move threshold is how fast the character need to move before its considered movement (triggering animations, etc)
        public float ground_detect_dist = 0.1f; //Margin distance between the character and the ground, used to detect if character is grounded.
        public LayerMask ground_layer = ~0; //What is considered ground?
        public bool use_navmesh = false;

        [Header("Interact")]
        public PlayerInteractBehavior interact_type = PlayerInteractBehavior.MoveAndInteract;
        public float interact_range = 0f; //Added to selectable use_range
        public float interact_offset = 0f; //Dont interact with center of character, but with offset in front
        public bool action_ui;           //Show action timer UI when performing actions

        public UnityAction<string, float> onTriggerAnim;

        private CapsuleCollider collide;
        private PlayerCharacterAttribute character_attr;
        private PlayerCharacterCombat character_combat;
        private PlayerCharacterCraft character_craft;
        private PlayerCharacterInventory character_inventory;
        private PlayerCharacterJump character_jump;
        private PlayerCharacterSwim character_swim;
        private PlayerCharacterClimb character_climb;
        private PlayerCharacterRide character_ride;
        private PlayerCharacterAnim character_anim;

		private LLMCharacter llm_character;

        private Vector3 move;
        private Vector3 facing;
        private Vector3 move_average;
        private Vector3 prev_pos;
        private Vector3 fall_vect;

        private bool auto_move = false;
        private Vector3 auto_move_pos;
        private Vector3 auto_move_pos_next;
        private Selectable auto_move_target = null;
        private Destructible auto_move_attack = null;

        private int auto_move_drop = -1;
        private InventoryData auto_move_drop_inventory;
        private float auto_move_timer = 0f;

        private Vector3 ground_normal = Vector3.up;
        private bool controls_enabled = true;
        private bool movement_enabled = true;

        private bool is_grounded = false;
        private bool is_fronted = false;
        private bool is_busy = false;
        private bool is_sleep = false;

        private Vector3 controls_move;
        private Vector3 controls_freelook;

        private ActionSleep sleep_target = null;
        private Coroutine action_routine = null;
        private GameObject action_progress = null;
        private SAction busy_action = null;
        private bool can_cancel_action = false;

        private Vector3[] nav_paths = new Vector3[0];
        private int path_index = 0;
        private bool calculating_path = false;
        private bool path_found = false;

        private static PlayerCharacter player_first = null;
        private static List<PlayerCharacter> players_list = new List<PlayerCharacter>();

        void Awake()
        {
            if (player_first == null || player_id < player_first.player_id)
                player_first = this;

            players_list.Add(this);
            collide = GetComponentInChildren<CapsuleCollider>();
            character_attr = GetComponent<PlayerCharacterAttribute>();
            character_combat = GetComponent<PlayerCharacterCombat>();
            character_craft = GetComponent<PlayerCharacterCraft>();
            character_inventory = GetComponent<PlayerCharacterInventory>();
            character_jump = GetComponent<PlayerCharacterJump>();
            character_swim = GetComponent<PlayerCharacterSwim>();
            character_climb = GetComponent<PlayerCharacterClimb>();
            character_ride = GetComponent<PlayerCharacterRide>();
            character_anim = GetComponent<PlayerCharacterAnim>();
            facing = transform.forward;
            prev_pos = transform.position;
            fall_vect = Vector3.down * fall_speed;
        }

        private void OnDestroy()
        {
            players_list.Remove(this);
        }

        private void Start()
        {
            PlayerControlsMouse mouse_controls = PlayerControlsMouse.Get();
            mouse_controls.onClickFloor += OnClickFloor;
            mouse_controls.onClickObject += OnClickObject;
            mouse_controls.onClick += OnClick;
            mouse_controls.onRightClick += OnRightClick;
            mouse_controls.onHold += OnMouseHold;
            mouse_controls.onRelease += OnMouseRelease;

            TheGame.Get().onPause += OnPause;

            if (player_id < 0)
                Debug.LogError("Player ID should be 0 or more: -1 is reserved to indicate neutral (no player)");
        }

        private void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            //Save position
            SaveData.position = GetPosition();

            if (IsDead() || !move_enabled)
                return;

            //Check if reached end of movement
            UpdateEndAutoMove();
            UpdateEndActions();

            UpdateControls();
        }

        void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())
                return;

            //Update the automove target position based on navmesh path, or moving target
            //UpdateAutoMoveTarget();

            //Check if grounded
            DetectGrounded();
            DetectFronted();

			move = GetComponent<NavMeshAgent>().velocity;

            //Apply the move calculated previously

            //Find facing direction
            Vector3 tfacing = FindFacingDirection();
            if (tfacing.magnitude > 0.5f)
                facing = tfacing;

            //Apply the facing
            Quaternion targ_rot = Quaternion.LookRotation(facing, Vector3.up);

            //Check the average traveled movement (allow to check if character is stuck)
            Vector3 last_frame_travel = transform.position - prev_pos;
            move_average = Vector3.MoveTowards(move_average, last_frame_travel, 1f * Time.fixedDeltaTime);
            prev_pos = transform.position;

            //Stop auto move
            bool stuck_somewhere = move_average.magnitude < 0.02f && auto_move_timer > 1f;
            if (stuck_somewhere)
                auto_move = false;
        }

        private void UpdateControls()
        {
            if (!IsControlsEnabled())
                return;

            //Controls
            PlayerControls controls = PlayerControls.Get(player_id);
            PlayerControlsMouse mcontrols = PlayerControlsMouse.Get();
            JoystickMobile joystick = JoystickMobile.Get();
            KeyControlsUI ui_controls = KeyControlsUI.Get(player_id);

            Vector2 cmove = controls.GetMove();
            Vector2 cfree = controls.GetFreelook();
            controls_move = new Vector3(cmove.x, 0f, cmove.y);
            controls_freelook = new Vector3(cfree.x, 0f, cfree.y);

            bool joystick_active = joystick != null && joystick.IsActive();
            if (joystick_active && !character_craft.IsBuildMode())
                controls_move += new Vector3(joystick.GetDir().x, 0f, joystick.GetDir().y);
            if (!controls.IsGamePad())
                controls_freelook = Vector3.zero;

            //Rotate
            controls_move = TheCamera.Get().GetFacingRotation() * controls_move;
            controls_freelook = TheCamera.Get().GetFacingRotation() * controls_freelook;

            //Check if panel is focused
            bool panel_focus = controls.gamepad_controls && ui_controls != null && ui_controls.IsPanelFocus();
            if (!panel_focus && !is_busy)
            {
                //Press Action button
                if (controls.IsPressAction())
                {
                    if (character_craft.CanBuild())
                        character_craft.StartCraftBuilding();
                    else
                        InteractWithNearest();
                }

                //Press attack
                if (Combat.CanAttack() && controls.IsPressAttack())
                    Attack();

                //Press jump
                if (character_jump != null && controls.IsPressJump())
                    character_jump.Jump();
            }

            //Start Building
            if (controls.IsPressUISelect() && !is_busy && character_craft.CanBuild())
                character_craft.StartCraftBuilding();

            //Stop the click auto move when moving with keyboard/joystick/gamepad
            if (controls.IsMoving() || mcontrols.IsDoubleTouch() || joystick_active)
                StopAutoMove();

            //Cancel action if moving
            bool is_moving_controls = auto_move || controls.IsMoving() || joystick_active;
            if (is_busy && can_cancel_action && is_moving_controls)
                CancelBusy();
        }

        private void UpdateEndAutoMove()
        {
            if (!auto_move || is_busy)
                return;

            Vector3 move_dir = auto_move_pos - GetInteractCenter();
            Buildable current_buildable = character_craft.GetCurrentBuildable();
            if (auto_move_target != null)
            {
                //Activate Selectable when near
                if (move_dir.magnitude < auto_move_target.GetUseRange(this))
                {
                    auto_move = false;
                    auto_move_target.Use(this, auto_move_pos);
                    auto_move_target = null;
                }
            }
            else if (current_buildable != null && character_craft.ClickedBuild())
		{
                //Finish construction when near clicked spot
                if (current_buildable != null 
						&& Vector3.Distance(transform.position, current_buildable.transform.position) < current_buildable.GetBuildRange(this) + 0.5f)
                {
                    auto_move = false;
                    character_craft.StartCraftBuilding(current_buildable.transform.position);
                }
            }
            else if (move_dir.magnitude < moving_threshold * 2f)
            {
                LLMInterface.Instance().StopCommand("explore");
                //Stop move & drop when near clicked spot
                auto_move = false;
                if(auto_move_drop != -1) character_inventory.DropItem(auto_move_drop_inventory, auto_move_drop);
            }
        }

        private void UpdateEndActions()
        {
            //Stop attacking if target cant be attacked anymore (tool broke, or target died...)
            if (!character_combat.CanAttack(auto_move_attack)){
				if(auto_move_attack) {
					LLMInterface.Instance().StopCommand();
				}
                auto_move_attack = null;
			}

			/*
            //Stop sleep
            if (is_busy || IsMoving() || sleep_target == null)
                StopSleep();*/
        }

        private void UpdateAutoMoveTarget()
        {
            if (!IsMovementEnabled())
                return;

            //Update auto move for moving targets
            GameObject auto_move_obj = GetAutoTarget();
            if (auto_move && auto_move_obj != null)
            {
                Vector3 diff = auto_move_obj.transform.position - auto_move_pos;
                if (diff.magnitude > 1f)
                {
                    auto_move_pos = auto_move_obj.transform.position;
                    auto_move_pos_next = auto_move_obj.transform.position;
                    CalculateNavmesh(); //Recalculate navmesh because target moved
                }
            }

            //Navmesh calculate next path
            if (auto_move && use_navmesh && path_found && path_index < nav_paths.Length)
            {
                auto_move_pos_next = nav_paths[path_index];
                Vector3 move_dir_total = auto_move_pos_next - transform.position;
                move_dir_total.y = 0f;
                if (move_dir_total.magnitude < 0.2f)
                    path_index++;
            }
        }

        private Vector3 FindMovementDirection()
        {
            Vector3 tmove = Vector3.zero;

            if (!IsMovementEnabled())
                return tmove;

            //AUTO Moving (after mouse click)
            auto_move_timer += Time.fixedDeltaTime;
            if (auto_move && auto_move_timer > 0.02f) //auto_move_timer to let the navmesh time to calculate a path
            {
                Vector3 move_dir_total = auto_move_pos - transform.position;
                Vector3 move_dir_next = auto_move_pos_next - transform.position;
                Vector3 move_dir = move_dir_next.normalized * Mathf.Min(move_dir_total.magnitude, 1f);
                move_dir.y = 0f;

                tmove = move_dir * GetMoveSpeed();
            }

            //Keyboard/gamepad moving
            if (!auto_move && IsControlsEnabled())
            {
                tmove = controls_move * GetMoveSpeed();
            }

            //Stop moving if doing action
            if (is_busy)
                tmove = Vector3.zero;

            if (!IsJumping() && !is_grounded)
                fall_vect = Vector3.MoveTowards(fall_vect, Vector3.down * fall_speed, fall_gravity * Time.fixedDeltaTime);

            if (!is_grounded || IsJumping())
            {
                tmove += fall_vect;
            }
            //Add slope angle
            else if (is_grounded)
            {
                tmove = Vector3.ProjectOnPlane(tmove.normalized, ground_normal).normalized * tmove.magnitude;
            }

            return tmove;
        }

        private Vector3 FindFacingDirection()
        {
            Vector3 tfacing = Vector3.zero;

            if (!IsMovementEnabled())
                return tfacing;
			
			if (IsBusy())
                return tfacing;

            //Calculate Facing
            if (IsMoving())
            {
                tfacing = new Vector3(move.x, 0f, move.z).normalized;
            }

            //Rotate character with right joystick when not in free rotate mode
            bool freerotate = TheCamera.Get().IsFreelook();
            if (!freerotate)
            {
                Vector2 look = controls_freelook;
                if (look.magnitude > 0.5f)
                    tfacing = look.normalized;
            }

            return tfacing;
        }

        private void OnPause(bool paused)
        {
            if (paused)
            {
            }
        }

        //Detect if character is on the floor
        private void DetectGrounded()
        {
            float hradius = GetColliderHeightRadius();
            float radius = GetColliderRadius() * 0.9f;
            Vector3 center = GetColliderCenter();

            float gdist; Vector3 gnormal;
            is_grounded = PhysicsTool.DetectGround(transform, center, hradius, radius, ground_layer, out gdist, out gnormal);
            ground_normal = gnormal;

            float slope_angle = Vector3.Angle(ground_normal, Vector3.up);
            is_grounded = is_grounded && slope_angle <= slope_angle_max;
        }

        //Detect if there is an obstacle in front of the character
        private void DetectFronted()
        {
            Vector3 scale = transform.lossyScale;
            float hradius = collide.height * scale.y * 0.5f - 0.02f; //radius is half the height minus offset
            float radius = collide.radius * (scale.x + scale.y) * 0.5f + 0.5f;

            Vector3 center = GetColliderCenter();
            Vector3 p1 = center;
            Vector3 p2 = center + Vector3.up * hradius;
            Vector3 p3 = center + Vector3.down * hradius;

            RaycastHit h1, h2, h3;
            bool f1 = PhysicsTool.RaycastCollision(p1, facing * radius, out h1);
            bool f2 = PhysicsTool.RaycastCollision(p2, facing * radius, out h2);
            bool f3 = PhysicsTool.RaycastCollision(p3, facing * radius, out h3);

            is_fronted = f1 || f2 || f3;

            //Debug.DrawRay(p1, facing * radius);
            //Debug.DrawRay(p2, facing * radius);
            //Debug.DrawRay(p3, facing * radius);
        }

        //--- Generic Actions ----

        //Same as trigger action, but also show the progress circle
        public void TriggerProgressBusy(SAction action, float duration, UnityAction callback = null)
        {
            if (!is_busy)
            {
                if (action_ui && AssetData.Get().action_progress != null && duration > 0.1f)
                {
                    action_progress = Instantiate(AssetData.Get().action_progress, transform);
                    action_progress.GetComponent<ActionProgress>().duration = duration;
                }

                is_busy = true;
                busy_action = action;
                action_routine = StartCoroutine(RunBusyRoutine(duration, callback));
                can_cancel_action = true;
                StopMove();
            }
        }

        //Wait for X seconds for any generic action (player can't do other things during that time)
        public void TriggerBusy(SAction action, float duration, UnityAction callback = null)
        {
            if (!is_busy)
            {
                is_busy = true;
                busy_action = action;
                action_routine = StartCoroutine(RunBusyRoutine(duration, callback));
                can_cancel_action = false;
            }
        }

        public void TriggerProgressBusy(float duration, UnityAction callback = null)
        {
            TriggerProgressBusy(null, duration, callback);
        }

        public void TriggerBusy(float duration, UnityAction callback = null)
        {
            TriggerBusy(null, duration, callback);
        }

        private IEnumerator RunBusyRoutine(float action_duration, UnityAction callback=null)
        {
            yield return new WaitForSeconds(action_duration);

            is_busy = false;
            busy_action = null;
            if (callback != null)
                callback.Invoke();
        }

        public void CancelBusy()
        {
            if (can_cancel_action && is_busy)
            {
                if (action_routine != null)
                    StopCoroutine(action_routine);
                if (action_progress != null)
                    Destroy(action_progress);
                is_busy = false;
                busy_action = null;
            }
        }

        //Call animation directly
        public void TriggerAnim(string anim, Vector3 face_at, float duration = 0f)
        {
            FaceTorward(face_at);
            if (onTriggerAnim != null)
                onTriggerAnim.Invoke(anim, duration);
        }

        public void SetBusy(bool action)
        {
            is_busy = action;
            can_cancel_action = false;
        }

        //---- Special actions

        public void Sleep(ActionSleep sleep_target)
        {
            if (!is_sleep && IsMovementEnabled())
            {
                this.sleep_target = sleep_target;
                is_sleep = true;
                auto_move = false;
                auto_move_attack = null;
                TheGame.Get().SetGameSpeedMultiplier(sleep_target.sleep_speed_mult);
				StartCoroutine("SleepTimer");
            }
        }

        public void StopSleep()
        {
            if (is_sleep)
            {
                is_sleep = false;
                sleep_target = null;
                TheGame.Get().SetGameSpeedMultiplier(1f);
                LLMInterface.Instance().StopCommand();
            }
        }

		IEnumerator SleepTimer(){
			yield return new WaitForSeconds(5);

			StopSleep();
		}

        //----- Player Orders ----------

        public void MoveTo(Vector3 pos)
        {
            auto_move = true;
            auto_move_pos = pos;
            auto_move_pos_next = pos;
            auto_move_target = null;
            auto_move_attack = null;
            auto_move_drop = -1;
            auto_move_drop_inventory = null;
            auto_move_timer = 0f;
            path_found = false;
            calculating_path = false;

            CalculateNavmesh();
        }

        public void UpdateMoveTo(Vector3 pos)
        {
            //Meant to be called every frame, for this reason don't do navmesh
            auto_move = true;
            auto_move_pos = pos;
            auto_move_pos_next = pos;
            path_found = false;
            calculating_path = false;
            auto_move_target = null;
            auto_move_attack = null;
            auto_move_drop = -1;
            auto_move_drop_inventory = null;
			CalculateNavmesh();
        }

        public void FaceFront()
        {
            FaceTorward(transform.position + TheCamera.Get().GetFacingFront());
        }

        public void FaceTorward(Vector3 pos)
        {
            Vector3 face = (pos - transform.position);
            face.y = 0f;
            if (face.magnitude > 0.01f)
            {
                facing = face.normalized;
            }
        }

        public void FaceDir(Vector3 dir)
        {
            dir.y = 0f;
            facing = dir.normalized;
        }

        public void Interact(Selectable selectable)
        {
            Interact(selectable, selectable.GetClosestInteractPoint(GetInteractCenter()));
        }

        public void Interact(Selectable selectable, Vector3 pos)
        {
            if (interact_type == PlayerInteractBehavior.MoveAndInteract)
                InteractMove(selectable, pos);
            else if (interact_type == PlayerInteractBehavior.InteractOnly)
                InteractDirect(selectable, pos);
        }

        //Interact directly (dont move to)
        public void InteractDirect(Selectable selectable, Vector3 pos)
        {
            if (selectable.IsInUseRange(this))
                selectable.Use(this, pos);
        }

        //Move to target and interact
        public void InteractMove(Selectable selectable, Vector3 pos)
        {
            bool can_interact = selectable.CanBeInteracted();
            Vector3 tpos = pos;
            if(can_interact)
                tpos = selectable.GetClosestInteractPoint(GetInteractCenter(), pos);

            auto_move_target = can_interact ? selectable : null;
            auto_move_pos = tpos;
            auto_move_pos_next = tpos;

            auto_move = true;
            auto_move_drop = -1;
            auto_move_drop_inventory = null;
            auto_move_timer = 0f;
            path_found = false;
            calculating_path = false;
            auto_move_attack = null;
            CalculateNavmesh();
        }

        public void InteractWithNearest()
        {
            bool freelook = TheCamera.Get().IsFreelook();
            Selectable nearest;

            if (freelook)
            {
                nearest = Selectable.GetNearestRaycast();
            }
            else
            {
                nearest = Selectable.GetNearestAutoInteract(GetInteractCenter(), 5f);
            }

            if (nearest != null)
            {
                Interact(nearest);
            }
        }

        public void Attack()
        {
            if (Combat.attack_type == PlayerAttackBehavior.ClickToHit)
                AttackFront();
            else
                AttackNearest();
        }

        public void AttackFront()
        {
            if(TheCamera.Get().IsFreelook())
                FaceFront();
            Combat.Attack();
        }

        public void Attack(Destructible target)
        {
            if (interact_type == PlayerInteractBehavior.MoveAndInteract)
                AttackMove(target);
            else if (Combat.attack_type == PlayerAttackBehavior.AutoAttack)
                AttackTarget(target);
            else
                AttackDirect(target);
        }

        //Just one attack strike (dont move to)
        public void AttackDirect(Destructible target)
        {
            if(Combat.IsAttackTargetInRange(target))
                Combat.Attack(target);
        }

        //Move to target and attack
        public void AttackMove(Destructible target)
        {
            if (character_combat.CanAttack(target))
            {
				character_inventory.SetActiveGroup(target.required_item);

                auto_move = true;
                auto_move_target = null;
                auto_move_attack = target;
                auto_move_pos = target.transform.position - (target.transform.position - transform.position).normalized * Combat.GetAttackRange(); 
                auto_move_pos_next = target.transform.position;
                auto_move_drop = -1;
                auto_move_drop_inventory = null;
                auto_move_timer = 0f;
                path_found = false;
                calculating_path = false;
                CalculateNavmesh();
            }
        }

        //Target for multiple attack, but dont move to target
        public void AttackTarget(Destructible target)
        {
            if (character_combat.CanAttack(target))
            {
                auto_move = false;
                auto_move_target = null;
                auto_move_attack = target;
                auto_move_pos = transform.position;
                auto_move_pos_next = transform.position;
                auto_move_drop = -1;
                auto_move_drop_inventory = null;
                auto_move_timer = 0f;
                path_found = false;
                calculating_path = false;
				CalculateNavmesh();
            }
        }

        public void AttackNearest()
        {
            float range = Mathf.Max(Combat.GetAttackRange() + 2f, 5f);
            Destructible destruct = Destructible.GetNearestAutoAttack(this, GetInteractCenter(), range);
            Attack(destruct);
        }

        public void StopMove()
        {
            StopAutoMove();
            move = Vector3.zero;
        }

        public void StopAutoMove()
        {
            auto_move = false;
            auto_move_target = null;
            auto_move_attack = null;
            auto_move_drop_inventory = null;
        }

        //Temporary pause auto move to be resumed (but keep its target)
        public void PauseAutoMove()
        {
            auto_move = false;
        }

        public void ResumeAutoMove()
        {
            if (auto_move_target != null || auto_move_attack != null)
                auto_move = true;
        }

        public void SetFallVect(Vector3 fall)
        {
            fall_vect = fall;
        }

        public void Kill()
        {
            character_combat.Kill();
        }

        public void EnableControls()
        {
            controls_enabled = true;
        }

        public void DisableControls()
        {
            controls_enabled = false;
            StopAutoMove();
        }

        public void EnableMovement()
        {
            movement_enabled = true;
        }

        public void DisableMovement()
        {
            movement_enabled = false;
            StopAutoMove();
        }

        public void EnableCollider()
        {
            collide.enabled = true;
        }

        public void DisableCollider()
        {
            collide.enabled = false;
        }

        //------- Mouse Clicks --------

        private void OnClick(Vector3 pos)
        {
            if (!IsControlsEnabled())
                return;
            
            bool freerotate = TheCamera.Get().IsFreelook();
            if (freerotate)
                AttackFront();
        }

        private void OnRightClick(Vector3 pos)
        {
            if (!IsControlsEnabled())
                return;

        }

        private void OnMouseHold(Vector3 pos)
        {
            if (!IsControlsEnabled())
                return;

            if (TheGame.IsMobile())
                return; //On mobile, use joystick instead, no mouse hold

            //Stop auto target if holding
            PlayerControlsMouse mcontrols = PlayerControlsMouse.Get();
            if (auto_move && mcontrols.GetMouseHoldDuration() > 1f)
                StopAutoMove();

            //Only hold for normal movement, if interacting dont change while holding
            if (character_craft.GetCurrentBuildable() == null && auto_move_target == null && auto_move_attack == null)
            {
                UpdateMoveTo(pos);
            }
        }

        private void OnMouseRelease(Vector3 pos)
        {
            if (!IsControlsEnabled())
                return;

            bool in_range = interact_type == PlayerInteractBehavior.MoveAndInteract || character_craft.IsInBuildRange();
            if (TheGame.IsMobile() && in_range)
            {
                character_craft.TryBuildAt(pos);
            }
        }

        private void OnClickFloor(Vector3 pos)
        {
            if (!IsControlsEnabled())
                return;

            CancelBusy();

            //Build mode
            if (character_craft.IsBuildMode())
            {
                if (character_craft.ClickedBuild())
                    character_craft.CancelCrafting();

                if (!TheGame.IsMobile()) //On mobile, will build on mouse release
                    character_craft.TryBuildAt(pos);
            }
            //Move to clicked position
            else if (interact_type == PlayerInteractBehavior.MoveAndInteract)
            {
                MoveTo(pos);

                PlayerUI ui = PlayerUI.Get(player_id);
                auto_move_drop = ui != null ? ui.GetSelectedSlotIndex() : -1;
                auto_move_drop_inventory = ui != null ? ui.GetSelectedSlotInventory() : null;
            }
        }

        private void OnClickObject(Selectable selectable, Vector3 pos)
        {
            if (!IsControlsEnabled())
                return;

            if (selectable == null)
                return;

            if (character_craft.IsBuildMode())
            {
                OnClickFloor(pos);
                return;
            }

            CancelBusy();
            selectable.Select();

            //Attack target ?
            bool freerotate = TheCamera.Get().IsFreelook();
            Destructible target = selectable.Destructible;
            if (freerotate)
            {
                AttackFront();
            }
            else if (target != null && character_combat.CanAutoAttack(target))
            {
                Attack(target);
            }
            else
            {
                Interact(selectable, pos);
            }
        }

        //---- Navmesh ----

        public void CalculateNavmesh()
        {
            if (auto_move && use_navmesh && !calculating_path)
            {
                calculating_path = true;
                path_found = false;
                path_index = 0;
                auto_move_pos_next = auto_move_pos; //Default
                NavMeshTool.CalculatePath(transform.position, auto_move_pos, 1 << 0, FinishCalculateNavmesh);
				GetComponent<NavMeshAgent>().SetDestination(auto_move_pos);
            }
        }

        private void FinishCalculateNavmesh(NavMeshToolPath path)
        {
            calculating_path = false;
            path_found = path.success;
            nav_paths = path.path;
            path_index = 0;
        }

        //---- Getters ----

        //Check if character is near an object of that group
        public bool IsNearGroup(GroupData group)
        {
            Selectable group_select = Selectable.GetNearestGroup(group, transform.position);
            return group_select != null && group_select.IsInUseRange(this);
        }

        public ActionSleep GetSleepTarget()
        {
            return sleep_target;
        }

        public Destructible GetAutoAttackTarget()
        {
            return auto_move_attack;
        }

        public Selectable GetAutoSelectTarget()
        {
            return auto_move_target;
        }

        public GameObject GetAutoTarget()
        {
            GameObject auto_move_obj = null;
            if (auto_move_target != null && auto_move_target.type == SelectableType.Interact)
                auto_move_obj = auto_move_target.gameObject;
            if (auto_move_attack != null)
                auto_move_obj = auto_move_attack.gameObject;
            return auto_move_obj;
        }

        public InventoryData GetAutoDropInventory()
        {
            return auto_move_drop_inventory;
        }

        public Vector3 GetAutoMoveTarget()
        {
            return auto_move_pos;
        }

        public bool IsDead()
        {
            return character_combat.IsDead();
        }

        public bool IsSleeping()
        {
            return is_sleep;
        }

        public bool IsFishing()
        {
            return is_busy && busy_action is ActionFish;
        }

        public bool IsRiding()
        {
            return character_ride != null && character_ride.IsRiding();
        }

        public bool IsSwimming()
        {
            return character_swim != null && character_swim.IsSwimming();
        }

        public bool IsClimbing()
        {
            return character_climb != null && character_climb.IsClimbing();
        }

        public bool IsJumping()
        {
            return character_jump != null && character_jump.IsJumping();
        }

        public SAction GetBusyAction()
        {
            return busy_action;
        }

        public bool IsBusy()
        {
            return is_busy;
        }
        
        public bool IsAutoMove()
        {
            return auto_move;
        }

        public bool IsMoving()
        {
            if (IsRiding() && character_ride.GetAnimal() != null)
                return character_ride.GetAnimal().IsMoving();
            if (Climbing && Climbing.IsClimbing())
                return Climbing.IsMoving();

            Vector3 moveXZ = new Vector3(move.x, 0f, move.z);
            return moveXZ.magnitude > GetMoveSpeed() * moving_threshold;
        }

        public Vector3 GetMove()
        {
            return move;
        }

        public Vector3 GetFacing()
        {
            return facing;
        }

        public Vector3 GetMoveNormalized()
        {
            return move.normalized * Mathf.Clamp01(move.magnitude / GetMoveSpeed());
        }

        public float GetMoveSpeed()
        {
            float boost = 1f + character_attr.GetBonusEffectTotal(BonusType.SpeedBoost);
            float base_speed = IsSwimming() ? character_swim.swim_speed : move_speed;
            return base_speed * boost * character_attr.GetSpeedMult();
        }

        public Vector3 GetPosition()
        {
            if (IsRiding() && character_ride.GetAnimal() != null)
                return character_ride.GetAnimal().transform.position;
            return transform.position;
        }

        public Vector3 GetInteractCenter()
        {
            return GetPosition() + transform.forward * interact_offset;
        }

        public Vector3 GetColliderCenter()
        {
            Vector3 scale = transform.lossyScale;
            return collide.transform.position + Vector3.Scale(collide.center, scale);
        }

        public float GetColliderHeightRadius()
        {
            Vector3 scale = transform.lossyScale;
            return collide.height * scale.y * 0.5f + ground_detect_dist; //radius is half the height minus offset
        }

        public float GetColliderRadius()
        {
            Vector3 scale = transform.lossyScale;
            return collide.radius * (scale.x + scale.y) * 0.5f;
        }

        public bool IsFronted()
        {
            return is_fronted;
        }

        public bool IsGrounded()
        {
            return is_grounded;
        }

        //Can the player give any command to the character?
        public bool IsControlsEnabled()
        {
            return move_enabled && controls_enabled && !IsDead() && !TheUI.Get().IsFullPanelOpened();
        }

        //Can the character move? Or is it performing an action that prevents him from moving?
        public bool IsMovementEnabled()
        {
            return move_enabled && movement_enabled && !IsDead() && !IsRiding() && !IsClimbing();
        }

        public PlayerCharacterCombat Combat
        {
            get { return character_combat; }
        }

        public PlayerCharacterAttribute Attributes
        {
            get {return character_attr;}
        }

        public PlayerCharacterCraft Crafting
        {
            get { return character_craft; }
        }

        public PlayerCharacterInventory Inventory
        {
            get { return character_inventory; }
        }

        public PlayerCharacterJump Jumping
        {
            get { return character_jump; } //Can be null
        }

        public PlayerCharacterSwim Swimming
        {
            get { return character_swim; } //Can be null
        }

        public PlayerCharacterClimb Climbing
        {
            get { return character_climb; } //Can be null
        }

        public PlayerCharacterRide Riding
        {
            get { return character_ride; } //Can be null
        }

        public PlayerCharacterAnim Animation
        {
            get { return character_anim; } //Can be null
        }

        public PlayerCharacterData Data => SaveData; //Compatibility with other versions, same than SaveData 
        public PlayerCharacterData SData => SaveData; //Compatibility with other versions, same than SaveData 

        public PlayerCharacterData SaveData 
        {
            get { return PlayerCharacterData.Get(player_id); }
        }

        public InventoryData InventoryData
        {
            get { return character_inventory.InventoryData; }
        }

        public InventoryData EquipData
        {
            get { return character_inventory.EquipData; }
        }

        public static PlayerCharacter GetNearest(Vector3 pos, float range = 999f)
        {
            PlayerCharacter nearest = null;
            float min_dist = range;
            foreach (PlayerCharacter unit in players_list)
            {
                float dist = (unit.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = unit;
                }
            }
            return nearest;
        }

        public static PlayerCharacter GetFirst()
        {
            return player_first;
        }

        public static PlayerCharacter Get(int player_id=0)
        {
            foreach (PlayerCharacter player in players_list)
            {
                if (player.player_id == player_id)
                    return player;
            }
            return null;
        }

        public static List<PlayerCharacter> GetAll()
        {
            return players_list;
        }
    }

}