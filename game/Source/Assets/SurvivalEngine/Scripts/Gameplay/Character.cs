using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    /// <summary> 
    /// Characters are allies or npc that can be given orders to move or perform actions
    /// </summary> 

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Destructible))]
    [RequireComponent(typeof(UniqueID))]
    public class Character : Craftable
    {
        [Header("Character")]
        public CharacterData data;

        [Header("Move")]
        public bool move_enabled = true;
        public float move_speed = 2f;
        public float rotate_speed = 250f;
        public float moving_threshold = 0.15f; //Move threshold is how fast the character need to move before its considered movement (triggering animations, etc)
        public bool avoid_obstacles = true; //More performant alternative to navmesh, will raycast to see if there are obstacles in front, then move around them
        public bool use_navmesh = false; //Use the real unity navmesh

        [Header("Ground/Falling")]
        public float fall_speed = 20f;
        public float slope_angle_max = 45f; //Maximum angle, in degrees that the character can climb up
        public float ground_detect_dist = 0.1f; //Margin distance between the character and the ground, used to detect if character is grounded.
        public LayerMask ground_layer = ~0; //What is considered ground?
        public float ground_refresh_rate = 0.2f; //Refresh rate for ground detection, higher value for performance, lower for accuracy, 0 = every frame

        [Header("Attack")]
        public bool attack_enabled = true;
        public int attack_damage = 10;
        public float attack_range = 1f;
        public float attack_cooldown = 3f;
        public float attack_windup = 0.5f;
        public float attack_duration = 1f;
        public AudioClip attack_audio;

        [Header("Attack Ranged")]
        public GameObject projectile_prefab;
        public Transform projectile_spawn;

        [Header("Action")]
        public float follow_distance = 3f;

        public UnityAction onAttack;
        public UnityAction onDamaged;
        public UnityAction onDeath;

        [HideInInspector]
        public bool was_spawned = false; //If true, means it was created by the player

        private Rigidbody rigid;
        private Selectable selectable;
        private Destructible destruct;
        private Buildable buildable; //Can be null
        private UniqueID unique_id;
        private Collider[] colliders;
        private Vector3 bounds_extent;
        private Vector3 bounds_center_offset;
        private string current_scene;

        private Vector3 moving;
        private Vector3 facing;

        private GameObject target = null;
        private Destructible attack_target = null;
        private PlayerCharacter attack_player = null;
        private Vector3 move_target;
        private Vector3 move_target_avoid;
        private Vector3 move_average;
        private Vector3 prev_pos;
        private float stuck_timer;

        private float attack_timer = 0f;
        private bool is_moving = false;
        private bool is_escaping = false;
        private bool is_attacking = false;
        private bool is_stuck = false;
        private bool attack_hit = false;
        private bool direct_move = false;

        private bool is_grounded = false;
        private bool is_fronted = false;
        private bool is_fronted_center = false;
        private bool is_fronted_left = false;
        private bool is_fronted_right = false;
        private float front_dist = 0f;
        private Vector3 ground_normal = Vector3.up;
        private float grounded_dist = 0f;
        private float grounded_dist_average = 0f;
        private float avoid_angle = 0f;
        private float avoid_side = 1f;

        private Vector3[] nav_paths = new Vector3[0];
        private Vector3 path_destination;
        private int path_index = 0;
        private bool follow_path = false;
        private bool calculating_path = false;
        private float navmesh_timer = 0f;
        private float ground_refesh_timer = 0f;

        private static List<Character> character_list = new List<Character>();

        protected override void Awake()
        {
            base.Awake();
            character_list.Add(this);
            rigid = GetComponent<Rigidbody>();
            selectable = GetComponent<Selectable>();
            destruct = GetComponent<Destructible>();
            buildable = GetComponent<Buildable>();
            unique_id = GetComponent<UniqueID>();
            colliders = GetComponentsInChildren<Collider>();
            avoid_side = Random.value < 0.5f ? 1f : -1f;
            facing = transform.forward;
            use_navmesh = move_enabled && use_navmesh;
            current_scene = SceneNav.GetCurrentScene();

            move_target = transform.position;
            move_target_avoid = transform.position;

            destruct.onDamaged += OnDamaged;
            destruct.onDeath += OnDeath;
            selectable.onDestroy += OnRemove;

            if (buildable != null)
                buildable.onBuild += OnBuild;

            foreach (Collider collide in colliders)
            {
                float size = collide.bounds.extents.magnitude;
                if (size > bounds_extent.magnitude)
                {
                    bounds_extent = collide.bounds.extents;
                    bounds_center_offset = collide.bounds.center - transform.position;
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            character_list.Remove(this);
        }

        void Start()
        {
            if (!was_spawned && PlayerData.Get().IsObjectRemoved(GetUID())) {
                Destroy(gameObject);
                return;
            }

            //Set current position
            SceneObjectData sobj = PlayerData.Get().GetSceneObject(GetUID());
            if (sobj != null && sobj.scene == current_scene)
            {
                transform.position = sobj.pos;
                transform.rotation = sobj.rot;
            }

            DetectGrounded(); //Check grounded
        }

        private void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (!move_enabled || IsDead())
                return;

            if (buildable && buildable.IsBuilding())
                return;
			
            //Update move target based on navmesh and target
            UpdateMoveTarget();

            //Find move direction
            Vector3 tmove = FindMoveDirection();

            //Apply movement
            moving = Vector3.Lerp(moving, tmove, 10f * Time.fixedDeltaTime);
            rigid.velocity = moving;

            //Find Facing direction
            if (IsMoving() && !IsDead())
            {
                Vector3 tface = new Vector3(moving.x, 0f, moving.z);
                facing = tface.normalized;
            }

            //Apply Rotation
            Quaternion targ_rot = Quaternion.LookRotation(facing, Vector3.up);
            Quaternion nrot = Quaternion.RotateTowards(rigid.rotation, targ_rot, rotate_speed * Time.fixedDeltaTime);
            rigid.MoveRotation(nrot);

            //Ground distance average
            if (is_grounded)
                grounded_dist_average = Mathf.MoveTowards(grounded_dist_average, grounded_dist, 5f * Time.fixedDeltaTime);
            
            //Check the average traveled movement (allow to check if character is stuck)
			float threshold = move_speed * Time.fixedDeltaTime * 0.25f;
            Vector3 last_frame_travel = transform.position - prev_pos;
            move_average = Vector3.MoveTowards(move_average, last_frame_travel, 2f * Time.fixedDeltaTime);
            prev_pos = transform.position;
            stuck_timer += (is_moving && move_average.magnitude < threshold) ? Time.fixedDeltaTime : -Time.fixedDeltaTime;
            stuck_timer = Mathf.Max(stuck_timer, 0f);
            is_stuck = is_moving && stuck_timer > 0.5f;
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (IsDead())
                return;

            if (buildable && buildable.IsBuilding())
                return;

            attack_timer += Time.deltaTime;
            navmesh_timer += Time.deltaTime;
            ground_refesh_timer += Time.deltaTime;

            //Detect obstacles and ground
            if (ground_refesh_timer > ground_refresh_rate)
            {
                ground_refesh_timer = Random.Range(-0.02f, 0.02f);
                DetectGrounded();
                DetectFronted();
            }

            //Save position
            PlayerData.Get().SetCharacterPosition(GetUID(), current_scene, transform.position, transform.rotation);

            //Stop moving
            if (is_moving && !HasTarget() && HasReachedMoveTarget(moving_threshold * 2f))
                Stop();

            //Attacking/Follow/Escape behavior
            UpdateFollowEscape();
            UpdateAttacking();

            //Check for obsctable avoiding
            CalculateAvoidObstacle();
        }

        private void UpdateMoveTarget()
        {
            if (IsDead())
                return;

            //Destination default
            move_target_avoid = move_target;

            //Navmesh
            if (use_navmesh && follow_path && !direct_move && is_moving && path_index < nav_paths.Length)
            {
                move_target_avoid = nav_paths[path_index];
                Vector3 dir_total = move_target_avoid - transform.position;
                dir_total.y = 0f;
                if (dir_total.magnitude < moving_threshold * 2f)
                    path_index++;
            }

            //Navmesh
            if (use_navmesh && is_moving && !direct_move)
            {
                Vector3 path_dir = path_destination - transform.position;
                Vector3 nav_move_dir = move_target - transform.position;
                float dot = Vector3.Dot(path_dir.normalized, nav_move_dir.normalized);
                if (dot < 0.7f)
                    CalculateNavmesh();
            }

            //Avoiding
            if (is_moving && !direct_move && avoid_obstacles && !use_navmesh && !HasReachedMoveTarget(1f))
                move_target_avoid = FindAvoidMoveTarget(move_target);
        }

        private Vector3 FindMoveDirection()
        {
            Vector3 tmove = Vector3.zero;
            bool is_flying = fall_speed < 0.01f;

            if (!IsDead())
            {
                //Moving
                if (is_moving)
                {
                    Vector3 move_dir_total = move_target - transform.position;
                    Vector3 move_dir_next = move_target_avoid - transform.position;
                    Vector3 move_dir = move_dir_next.normalized * Mathf.Min(move_dir_total.magnitude, 1f);
                    tmove = move_dir.normalized * Mathf.Min(move_dir.magnitude, 1f) * move_speed;
                }

                //Slope climbing
                float slope_angle = Vector3.Angle(ground_normal, Vector3.up);
                bool up_hill = Vector3.Dot(transform.forward, ground_normal) < -0.1f; //Climbing up
                if (up_hill && !is_flying && slope_angle > slope_angle_max)
                    tmove = Vector3.zero; // Slope too high
            }

            //Falling
            if (!is_grounded && !is_flying)
                tmove += Vector3.down * fall_speed;

            //Cancel falling
            if (is_grounded && !is_flying && tmove.y < 0f)
                tmove.y = 0f;

            //Adjust to slope
            if (is_grounded && !is_flying)
                tmove = Vector3.ProjectOnPlane(tmove.normalized, ground_normal).normalized * tmove.magnitude;

            return tmove;
        }

        private void UpdateFollowEscape()
        {
            if (target == null || is_attacking)
                return;

            Vector3 targ_dir = (target.transform.position - transform.position);
            targ_dir.y = 0f;

            if (is_escaping)
            {
                Vector3 targ_pos = transform.position - targ_dir.normalized * 4f;
                move_target = targ_pos;
            }
            else if (is_moving)
            {
                move_target = target.transform.position;

                //Stop following
                if ((attack_target != null || attack_player != null) && targ_dir.magnitude < GetAttackTargetHitRange() * 0.8f)
                {
                    move_target = transform.position;
                    is_moving = false;
                }

                //Stop following
                if (attack_target == null && attack_player == null && HasReachedMoveTarget(follow_distance))
                {
                    move_target = transform.position;
                    is_moving = false;
                }
            }
        }

        private void UpdateAttacking()
        {
            //Stop attacking
            if (is_attacking && !HasAttackTarget())
                Stop();

            if (!HasAttackTarget())
                return;

            Vector3 targ_dir = (target.transform.position - transform.position);

            //Cooldown in between each attack
            if (!is_attacking)
            {
				if (!is_moving && targ_dir.magnitude > GetAttackTargetHitRange())
                {
                    Attack(attack_target); //Start moving again
                }
				
                if (attack_timer > attack_cooldown)
                {
                    if (targ_dir.magnitude < GetAttackTargetHitRange())
                    {
                        StartAttackStrike();
                    }
                }
            }

            //During the attack (strike)
            if (is_attacking)
            {
                move_target = transform.position;
                move_target_avoid = transform.position;
                FaceTorward(target.transform.position);

                //Before strike
                if (!attack_hit && attack_timer > attack_windup)
                {
                    DoAttackStrike();
                }

                //After strike, start followin again
                if (attack_timer > attack_duration)
                {
                    is_attacking = false;
                    attack_timer = 0f;
                    is_moving = true;

                    if (attack_target != null)
                        Attack(attack_target);
                    if (attack_player != null)
                        Attack(attack_player);
                }
            }

            if (attack_target != null && attack_target.IsDead())
                Stop();

            if (attack_player != null && attack_player.IsDead())
                Stop();

            if (targ_dir.magnitude < GetAttackTargetHitRange() * 0.8f)
                StopMove();
        }

        //Start the attack windup
        private void StartAttackStrike()
        {
            is_attacking = true;
            is_moving = false;
            attack_hit = false;
            attack_timer = 0f;

            if (onAttack != null)
                onAttack.Invoke();
        }

        //Hit the target and deal damage
        private void DoAttackStrike()
        {
            attack_hit = true;

            float range = (target.transform.position - transform.position).magnitude;
            if (range < GetAttackTargetHitRange())
            {
                if (projectile_prefab != null)
                {
                    //Ranged attack
                    Vector3 pos = GetProjectileSpawnPos();
                    Vector3 tpos = target.transform.position + Vector3.up * 1.5f;
                    Vector3 dir = tpos - pos;
                    GameObject proj = Instantiate(projectile_prefab, pos, Quaternion.LookRotation(dir.normalized, Vector3.up));
                    Projectile project = proj.GetComponent<Projectile>();
                    project.shooter = destruct;
                    project.dir = dir.normalized;
                    project.damage = attack_damage;
                }
                else
                {
                    if (attack_target != null)
                        attack_target.TakeDamage(destruct, attack_damage);
                    if (attack_player != null)
                        attack_player.Combat.TakeDamage(attack_damage);
                }
            }

            if (selectable.IsNearCamera(20f))
                TheAudio.Get().PlaySFX("character", attack_audio);
        }

        private Vector3 GetProjectileSpawnPos()
        {
            if (projectile_spawn != null)
                return projectile_spawn.position;
            return transform.position + Vector3.up * 2f;
        }

        public void MoveTo(Vector3 pos)
        {
            move_target = pos;
            move_target_avoid = pos;
            target = null;
            attack_target = null;
            attack_player = null;
            is_escaping = false;
            is_moving = true;
            stuck_timer = 0f;
            direct_move = false;
            CalculateNavmesh();
        }

        //Meant to be called every frame, for this reason don't do navmesh
        public void DirectMoveTo(Vector3 pos)
        {
            move_target = pos;
            move_target_avoid = pos;
            target = null;
            attack_target = null;
            attack_player = null;
            is_escaping = false;
            is_moving = true;
            direct_move = true;
            stuck_timer = 0f;
        }

        public void DirectMoveToward(Vector3 dir)
        {
            DirectMoveTo(transform.position + dir.normalized);
        }

        public void Follow(GameObject target)
        {
            if (target != null)
            {
                this.target = target.gameObject;
                this.attack_target = null;
                this.attack_player = null;
                move_target = target.transform.position;
                is_escaping = false;
                is_moving = true;
                stuck_timer = 0f;
                direct_move = false;
                CalculateNavmesh();
            }
        }

        public void Escape(GameObject target)
        {
            this.target = target;
            this.attack_target = null;
            this.attack_player = null;
            Vector3 dir = target.transform.position - transform.position;
            move_target = transform.position - dir;
            is_escaping = true;
            is_moving = true;
            direct_move = false;
            stuck_timer = 0f;
        }

        public void Attack(Destructible target)
        {
            if (attack_enabled && target != null && target != destruct && target.CanBeAttacked())
            {
                this.target = target.gameObject;
                this.attack_target = target;
                this.attack_player = null;
                move_target = target.transform.position;
                is_escaping = false;
                is_moving = true;
                stuck_timer = 0f;
                direct_move = false;
                CalculateNavmesh();
            }
        }

        public void Attack(PlayerCharacter target)
        {
            if (attack_enabled && target != null && !target.IsDead())
            {
                this.target = target.gameObject;
                this.attack_target = null;
                this.attack_player = target;
                move_target = target.transform.position;
                is_escaping = false;
                is_moving = true;
                stuck_timer = 0f;
                direct_move = false;
                CalculateNavmesh();
            }
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

        public void Stop()
        {
            target = null;
            attack_target = null;
            attack_player = null;
            rigid.velocity = Vector3.zero;
			moving = Vector3.zero;
            move_target = transform.position;
            is_moving = false;
            is_attacking = false;
            stuck_timer = 0f;
            direct_move = false;
        }

        public void StopMove()
        {
            move_target = transform.position;
            rigid.velocity = Vector3.zero;
            moving = Vector3.zero;
            is_moving = false;
        }

        public void Kill()
        {
            if (destruct != null)
                destruct.Kill();
            else
                selectable.Destroy();
        }

        public void Reset()
        {
            StopMove();
            rigid.isKinematic = false;
            destruct.Reset();
        }

        private void CalculateAvoidObstacle()
        {
            //Add an offset to escape path when fronted
            if (avoid_obstacles & !direct_move)
            {
                if (is_fronted_left && !is_fronted_right)
                    avoid_side = 1f;
                if (is_fronted_right && !is_fronted_left)
                    avoid_side = -1f;

                //When fronted on all sides, use target to influence which side to go
                if (is_fronted_center && is_fronted_left && is_fronted_right && target)
                {
                    Vector3 dir = target.transform.position - transform.position;
                    dir = dir * (is_escaping ? -1f : 1f);
                    float dot = Vector3.Dot(dir.normalized, transform.right);
                    if (Mathf.Abs(dot) > 0.5f)
                        avoid_side = Mathf.Sign(dot);
                }

                float angle = avoid_side * 90f;
                float far_val = is_fronted ? 1f - (front_dist / destruct.hit_range) : Mathf.Abs(angle) / 90f; //1f = close, 0f = far
                float angle_speed = far_val * 150f + 50f;
                avoid_angle = Mathf.MoveTowards(avoid_angle, is_fronted ? angle : 0f, angle_speed * Time.deltaTime);
            }
        }

        private void CalculateNavmesh()
        {
            if (use_navmesh && !calculating_path && navmesh_timer > 0.5f)
            {
                calculating_path = true;
                path_index = 0;
                NavMeshTool.CalculatePath(transform.position, move_target, 1 << 0, FinishCalculateNavmesh);
                path_destination = move_target;
                navmesh_timer = 0f;
            }
        }

        private void FinishCalculateNavmesh(NavMeshToolPath path)
        {
            calculating_path = false;
            follow_path = path.success;
            nav_paths = path.path;
            path_index = 0;
            navmesh_timer = 0f;
        }

        //Check if touching the ground
        private void DetectGrounded()
        {
            float radius = (bounds_extent.x + bounds_extent.z) * 0.5f;
            float center_offset = bounds_extent.y;
            float hradius = center_offset + ground_detect_dist;

            Vector3 center = transform.position + bounds_center_offset;
            center.y = transform.position.y + center_offset;

            float gdist; Vector3 gnormal;
            is_grounded = PhysicsTool.DetectGround(transform, center, hradius, radius, ground_layer, out gdist, out gnormal);
            ground_normal = gnormal;
            grounded_dist = gdist;

            float slope_angle = Vector3.Angle(ground_normal, Vector3.up);
            is_grounded = is_grounded && slope_angle <= slope_angle_max;
        }

        //Detect if there is an obstacle in front of the character
        private void DetectFronted()
        {
            float radius = destruct.hit_range * 2f;

            Vector3 center = destruct.GetCenter();
            Vector3 dir = move_target_avoid - transform.position;
            Vector3 dirl = Quaternion.AngleAxis(-45f, Vector3.up) * dir.normalized;
            Vector3 dirr = Quaternion.AngleAxis(45f, Vector3.up) * dir.normalized;

            RaycastHit h, hl, hr;
            bool fc = PhysicsTool.RaycastCollision(center, dir.normalized * radius, out h);
            bool fl = PhysicsTool.RaycastCollision(center, dirl.normalized * radius, out hl);
            bool fr = PhysicsTool.RaycastCollision(center, dirr.normalized * radius, out hr);
            is_fronted_center = fc && (target == null || h.collider.gameObject != target);
            is_fronted_left = fl && (target == null || hl.collider.gameObject != target);
            is_fronted_right = fr && (target == null || hr.collider.gameObject != target);

            int front_count = (fc ? 1 : 0) + (fl ? 1 : 0) + (fr ? 1 : 0);
            front_dist = (fc ? h.distance : 0f) + (fl ? hl.distance : 0f) + (fr ? hr.distance : 0f);
            if (front_count > 0) front_dist = front_dist / (float)front_count;

            is_fronted = is_fronted_center || is_fronted_left || is_fronted_right;
        }

        private void OnBuild()
        {
            if (data != null)
            {
                TrainedCharacterData cdata = PlayerData.Get().AddCharacter(data.id, current_scene, transform.position, transform.rotation);
                unique_id.unique_id = cdata.uid;
            }
        }

        private void OnDamaged()
        {
            if (onDamaged != null)
                onDamaged.Invoke();
        }

        private void OnDeath()
        {
            rigid.velocity = Vector3.zero;
            moving = Vector3.zero;
            rigid.isKinematic = true;
            target = null;
            attack_target = null;
            attack_player = null;
            move_target = transform.position;
            is_moving = false;

            foreach (Collider coll in colliders)
                coll.enabled = false;

            if (onDeath != null)
                onDeath.Invoke();

            if (data != null)
            {
                foreach (PlayerCharacter character in PlayerCharacter.GetAll())
                    character.SaveData.AddKillCount(data.id); //Add kill count
            }
        }

        private void OnRemove()
        {
            PlayerData.Get().RemoveCharacter(GetUID());
            if (!was_spawned)
                PlayerData.Get().RemoveObject(GetUID());
        }

        //Find new move target while trying to avoid obstacles
        private Vector3 FindAvoidMoveTarget(Vector3 target)
        {
            Vector3 targ_dir = (target - transform.position);
            targ_dir = Quaternion.AngleAxis(avoid_angle, Vector3.up) * targ_dir; //Rotate if obstacle in front
            return transform.position + targ_dir;
        }

        //Did it reach its target destination?
        public bool HasReachedMoveTarget()
        {
            return HasReachedMoveTarget(moving_threshold * 2f); //Double threshold to make sure it doesn't stop moving before reaching it
        }

        public bool HasReachedMoveTarget(float distance)
        {
            Vector3 diff = move_target - transform.position;
            return (diff.magnitude < distance);
        }

        public bool HasTarget()
        {
            return target != null;
        }

        public bool HasAttackTarget()
        {
            return attack_enabled && target != null && GetAttackTarget() != null;
        }

        public GameObject GetAttackTarget()
        {
            GameObject target = null;
            if (attack_player != null)
                target = attack_player.gameObject;
            else if (attack_target != null)
                target = attack_target.gameObject;
            return target;
        }

        public float GetAttackTargetHitRange()
        {
            if (attack_target != null)
                return attack_range + attack_target.hit_range;
            return attack_range;
        }

        public bool IsAttacking()
        {
            if (HasAttackTarget()) {
                Vector3 targ_dir = (target.transform.position - transform.position);
                return targ_dir.magnitude < GetAttackTargetHitRange();
            }
            return false;
        }

        public bool IsDead()
        {
            return destruct.IsDead();
        }

        //Is actually moving
        public bool IsMoving()
        {
            Vector3 moveXZ = new Vector3(moving.x, 0f, moving.z);
            return is_moving && moveXZ.magnitude > moving_threshold;
        }

        //Order to move given
        public bool IsTryMoving()
        {
            return is_moving;
        }

        public Vector3 GetMove()
        {
            return moving;
        }

        public Vector3 GetFacing()
        {
            return facing;
        }

        public bool IsGrounded()
        {
            return is_grounded;
        }

        public bool IsFronted()
        {
            return is_fronted;
        }

        public bool IsFrontedLeft()
        {
            return is_fronted_left;
        }

        public bool IsFrontedRight()
        {
            return is_fronted_right;
        }

        public bool IsStuck()
        {
            return is_stuck;
        }

        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id.unique_id);
        }

        public string GetUID()
        {
            return unique_id.unique_id;
        }

        public string GetSubUID(string tag)
        {
            return unique_id.GetSubUID(tag);
        }

        public bool HasGroup(GroupData group)
        {
            if (data != null)
                return data.HasGroup(group) || selectable.HasGroup(group);
            return selectable.HasGroup(group);
        }

        public Selectable GetSelectable()
        {
            return selectable;
        }

        public Destructible GetDestructible()
        {
            return destruct;
        }

        public Buildable GetBuildable()
        {
            return buildable; //Can be null
        }

        public TrainedCharacterData SaveData
        {
            get { return PlayerData.Get().GetCharacter(GetUID()); }
        }

        public static new Character GetNearest(Vector3 pos, float range = 999f)
        {
            Character nearest = null;
            float min_dist = range;
            foreach (Character unit in character_list)
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

        public static int CountInRange(Vector3 pos, float range)
        {
            int count = 0;
            foreach (Character character in GetAll())
            {
                float dist = (character.transform.position - pos).magnitude;
                if (dist < range && !character.IsDead())
                    count++;
            }
            return count;
        }

        public static int CountInRange(CharacterData data, Vector3 pos, float range)
        {
            int count = 0;
            foreach (Character character in GetAll())
            {
                if (character.data == data && !character.IsDead()) {
                    float dist = (character.transform.position - pos).magnitude;
                    if (dist < range)
                        count++;
                }
            }
            return count;
        }

        public static Character GetByUID(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (Character unit in character_list)
                {
                    if (unit.GetUID() == uid)
                        return unit;
                }
            }
            return null;
        }

        public static List<Character> GetAllOf(CharacterData data)
        {
            List<Character> valid_list = new List<Character>();
            foreach (Character character in character_list)
            {
                if (character.data == data)
                    valid_list.Add(character);
            }
            return valid_list;
        }

        public static new List<Character> GetAll()
        {
            return character_list;
        }

        //Spawn an existing one in the save file (such as after loading)
        public static Character Spawn(string uid, Transform parent = null)
        {
            TrainedCharacterData tcdata = PlayerData.Get().GetCharacter(uid);
            if (tcdata != null && tcdata.scene == SceneNav.GetCurrentScene())
            {
                CharacterData cdata = CharacterData.Get(tcdata.character_id);
                if (cdata != null)
                {
                    GameObject cobj = Instantiate(cdata.character_prefab, tcdata.pos, tcdata.rot);
                    cobj.transform.parent = parent;

                    Character character = cobj.GetComponent<Character>();
                    character.data = cdata;
                    character.was_spawned = true;
                    character.unique_id.unique_id = uid;
                    return character;
                }
            }
            return null;
        }

        //Create a totally new one that will be added to save file, but only after constructed by the player
        public static Character CreateBuildMode(CharacterData data, Vector3 pos)
        {
            GameObject build = Instantiate(data.character_prefab, pos, data.character_prefab.transform.rotation);
            Character character = build.GetComponent<Character>();
            character.data = data;
            character.was_spawned = true;
            return character;
        }

        //Create a totally new one that will be added to save file
        public static Character Create(CharacterData data, Vector3 pos)
        {
            Quaternion rot = Quaternion.Euler(0f, 180f, 0f);
            Character unit = Create(data, pos, rot);
            return unit;
        }

        public static Character Create(CharacterData data, Vector3 pos, Quaternion rot)
        {
            TrainedCharacterData ditem = PlayerData.Get().AddCharacter(data.id, SceneNav.GetCurrentScene(), pos, rot);
            GameObject build = Instantiate(data.character_prefab, pos, rot);
            Character unit = build.GetComponent<Character>();
            unit.data = data;
            unit.was_spawned = true;
            unit.unique_id.unique_id = ditem.uid;
            return unit;
        }
    }

}