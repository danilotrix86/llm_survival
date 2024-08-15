using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    public enum AnimalState
    {
        Wander = 0,
        Alerted = 2,
        Escape = 4,
        Attack = 6,
        MoveTo = 10,
        Dead = 20,
    }

    public enum AnimalBehavior
    {
        None = 0,   //Custom behavior from another script
        Escape = 5,  //Escape on sight
        PassiveEscape = 10,  //Escape if attacked 
        PassiveDefense = 15, //Attack if attacked
        Aggressive = 20, //Attack on sight, goes back after a while
        VeryAggressive = 25, //Attack on sight, will not stop following
    }

    public enum WanderBehavior
    {
        None = 0, //Dont wander
        WanderNear = 10, //Will wander near starting position
        WanderFar = 20, //Will wander beyond starting position
    }

    /// <summary>
    /// Animal behavior script for wandering, escaping, or chasing the player
    /// </summary>

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Destructible))]
    [RequireComponent(typeof(Character))]
    public class AnimalWild : MonoBehaviour
    {
        [Header("Behavior")]
        public WanderBehavior wander = WanderBehavior.WanderNear;
        public AnimalBehavior behavior = AnimalBehavior.PassiveEscape;

        [Header("Move")]
        public float wander_speed = 2f;
        public float run_speed = 5f;
        public float wander_range = 10f;
        public float wander_interval = 10f;

        [Header("Vision")]
        public float detect_range = 5f;
        public float detect_angle = 360f;
        public float detect_360_range = 1f;
        public float reaction_time = 0.5f; //How fast it detects threats

        [Header("Actions")]
        public float action_duration = 10f; //How long will it attack/escape targets

        public UnityAction onAttack;
        public UnityAction onDamaged;
        public UnityAction onDeath;

        private AnimalState state;
        private Character character;
        private Selectable selectable;
        private Destructible destruct;
        private Animator animator;

        private Vector3 start_pos;

        private PlayerCharacter player_target = null;
        private Destructible attack_target = null;
        private Vector3 wander_target;

        private bool is_running = false;
        private float state_timer = 0f;
        private bool is_active = false;

        private float lure_interest = 8f;
        private bool force_action = false;
        private float update_timer = 0f;

        void Awake()
        {
            character = GetComponent<Character>();
            destruct = GetComponent<Destructible>();
            selectable = GetComponent<Selectable>();
            animator = GetComponentInChildren<Animator>();
            start_pos = transform.position;
            state_timer = 99f; //Find wander right away
            update_timer = Random.Range(-1f, 1f);

            if (wander != WanderBehavior.None)
                transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        void Start()
        {
            character.onAttack += OnAttack;
            destruct.onDamaged += OnDamaged;
            destruct.onDamagedBy += OnDamagedBy;
            destruct.onDamagedByPlayer += OnDamagedPlayer;
            destruct.onDeath += OnDeath;
        }

        void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())
                return;

            //Optimization, dont run if too far
            float dist = (TheCamera.Get().GetTargetPos() - transform.position).magnitude;
            float active_range = Mathf.Max(detect_range * 2f, selectable.active_range * 0.8f);
            is_active = (state != AnimalState.Wander && state != AnimalState.Dead) || character.IsMoving() || dist < active_range;
        }

        private void Update()
        {
            //Animations
            bool paused = TheGame.Get().IsPaused();
            if (animator != null)
                animator.enabled = !paused;

            if (TheGame.Get().IsPaused())
                return;

            if (state == AnimalState.Dead || behavior == AnimalBehavior.None || !is_active)
                return;

            state_timer += Time.deltaTime;

            if (state != AnimalState.MoveTo)
                is_running = (state == AnimalState.Escape || state == AnimalState.Attack);

            character.move_speed = is_running ? run_speed : wander_speed;

            //States
            if (state == AnimalState.Wander)
            {
                if (state_timer > wander_interval && wander != WanderBehavior.None)
                {
                    state_timer = Random.Range(-1f, 1f);
                    FindWanderTarget();
                    character.MoveTo(wander_target);
                }

                //Character stuck
                if (character.IsStuck())
                    character.Stop();
            }

            if (state == AnimalState.Alerted)
            {
                GameObject target = GetTarget();
                if (target == null)
                {
                    character.Stop();
                    ChangeState(AnimalState.Wander);
                    return;
                }

                character.FaceTorward(target.transform.position);

                if (state_timer > reaction_time)
                {
                    ReactToThreat();
                }
            }

            if (state == AnimalState.Escape)
            {
                GameObject target = GetTarget();
                if (target == null)
                {
                    StopAction();
                    return;
                }

                if (!force_action && state_timer > action_duration)
                {
                    Vector3 targ_dir = (target.transform.position - transform.position);
                    targ_dir.y = 0f;

                    if (targ_dir.magnitude > detect_range)
                    {
                        StopAction();
                    }
                }

            }

            if (state == AnimalState.Attack)
            {
                GameObject target = GetTarget();
                if (target == null)
                {
                    StopAction();
                    return;
                }

                //Very aggressive wont stop following 
                if (!force_action && behavior != AnimalBehavior.VeryAggressive && state_timer > action_duration)
                {
                    Vector3 targ_dir = target.transform.position - transform.position;
                    Vector3 start_dir = start_pos - transform.position;

                    float follow_range = detect_range * 2f;
                    bool cant_see = targ_dir.magnitude > follow_range;
                    bool too_far = wander == WanderBehavior.WanderNear && start_dir.magnitude > Mathf.Max(wander_range, follow_range);
                    if (cant_see || too_far)
                    {
                        StopAction();
                        MoveToTarget(wander_target, false);
                    }
                }
            }

            if (state == AnimalState.MoveTo)
            {
                if (character.HasReachedMoveTarget())
                    StopAction();
            }

            update_timer += Time.deltaTime;
            if (update_timer > 0.5f)
            {
                update_timer = Random.Range(-0.1f, 0.1f);
                SlowUpdate(); //Optimization
            }

            //Animations
            if (animator != null && animator.enabled)
            {
                animator.SetBool("Move", IsMoving() && IsActive());
                animator.SetBool("Run", IsRunning() && IsActive());
            }
        }

        private void SlowUpdate()
        {
            if (state == AnimalState.Wander)
            {
                //These behavior trigger a reaction on sight, while the "Defense" behavior only trigger a reaction when attacked
                if (behavior == AnimalBehavior.Aggressive || behavior == AnimalBehavior.VeryAggressive || behavior == AnimalBehavior.Escape)
                {
                    DetectThreat(detect_range);

                    if (GetTarget() != null)
                    {
                        character.Stop();
                        ChangeState(AnimalState.Alerted);
                    }
                }
            }

            if (state == AnimalState.Attack)
            {
                if (character.IsStuck() && !character.IsAttacking() && state_timer > 1f)
                {
                    DetectThreat(detect_range);
                    ReactToThreat();
                }
            }
        }

        //Detect if the player is in vision
        private void DetectThreat(float range)
        {
            Vector3 pos = transform.position;

            //React to player
            float min_dist = range;
            foreach (PlayerCharacter player in PlayerCharacter.GetAll())
            {
                Vector3 char_dir = (player.transform.position - pos);
                float dist = char_dir.magnitude;
                if (dist < min_dist && !player.IsDead())
                {
                    float dangle = detect_angle / 2f; // /2 for each side
                    float angle = Vector3.Angle(transform.forward, char_dir.normalized);
                    if (angle < dangle || char_dir.magnitude < detect_360_range)
                    {
                        player_target = player;
                        attack_target = null;
                        min_dist = dist;
                    }
                }
            }

            //React to other characters/destructibles
            foreach (Selectable selectable in Selectable.GetAllActive())
            {
                if (selectable.gameObject != gameObject)
                {
                    Vector3 dir = (selectable.transform.position - pos);
                    if (dir.magnitude < min_dist)
                    {
                        float dangle = detect_angle / 2f; // /2 for each side
                        float angle = Vector3.Angle(transform.forward, dir.normalized);
                        if (angle < dangle || dir.magnitude < detect_360_range)
                        {
                            //Find destructible to attack
                            if (HasAttackBehavior())
                            {
                                Destructible destruct = selectable.Destructible;
                                if (destruct && !destruct.IsDead() && (destruct.target_team == AttackTeam.Ally || destruct.target_team == AttackTeam.Enemy)) //Attack by default (not neutral)
                                {
                                    if (destruct.target_team != this.destruct.target_team || destruct.target_group != this.destruct.target_group) //Is not in same team
                                    {
                                        attack_target = destruct;
                                        player_target = null;
                                        min_dist = dir.magnitude;
                                    }
                                }
                            }

                            //Find character to escape from
                            if (HasEscapeBehavior())
                            {
                                Character character = selectable.Character;
                                if (character && character.attack_enabled) //Only afraid if the character can attack
                                {
                                    if (character.GetDestructible().target_group != this.destruct.target_group) //Not afraid if in same team
                                    {
                                        attack_target = destruct;
                                        player_target = null;
                                        min_dist = dir.magnitude;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        //React to player if seen by animal
        private void ReactToThreat()
        {
            GameObject target = GetTarget();

            if (target == null || IsDead())
                return;

            if (HasEscapeBehavior())
            {
                ChangeState(AnimalState.Escape);
                character.Escape(target);
            }
            else if (HasAttackBehavior())
            {
                ChangeState(AnimalState.Attack);
                if (player_target)
                    character.Attack(player_target);
                else if (attack_target)
                    character.Attack(attack_target);
            }
        }

        private GameObject GetTarget()
        {
            if (player_target != null && !player_target.IsDead())
                return player_target.gameObject;
            else if (attack_target != null && !attack_target.IsDead())
                return attack_target.gameObject;
            return null;
        }

        private void FindWanderTarget()
        {
            float range = Random.Range(0f, wander_range);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 spos = wander == WanderBehavior.WanderFar ? transform.position : start_pos;
            Vector3 pos = spos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * range;
            wander_target = pos;

            Lure lure = Lure.GetNearestInRange(transform.position);
            if (lure != null)
            {
                Vector3 dir = lure.transform.position - transform.position;
                dir.y = 0f;

                Vector3 center = transform.position + dir.normalized * dir.magnitude * 0.5f;
                if (lure_interest < 4f)
                    center = lure.transform.position;

                float range2 = Mathf.Clamp(lure_interest, 1f, wander_range);
                Vector3 pos2 = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * range2;
                wander_target = pos2;

                lure_interest = lure_interest * 0.5f;
                if (lure_interest <= 0.2f)
                    lure_interest = 8f;
            }
        }

        public void AttackTarget(PlayerCharacter target)
        {
            if (target != null)
            {
                ChangeState(AnimalState.Attack);
                this.player_target = target;
                this.attack_target = null;
                force_action = true;
                character.Attack(target);
            }
        }

        public void EscapeTarget(PlayerCharacter target)
        {
            if (target != null)
            {
                ChangeState(AnimalState.Escape);
                this.player_target = target;
                this.attack_target = null;
                force_action = true;
                character.Escape(target.gameObject);
            }
        }

        public void AttackTarget(Destructible target)
        {
            if (target != null)
            {
                ChangeState(AnimalState.Attack);
                this.attack_target = target;
                this.player_target = null;
                force_action = true;
                character.Attack(target);
            }
        }

        public void EscapeTarget(Destructible target)
        {
            if (target != null)
            {
                ChangeState(AnimalState.Escape);
                this.attack_target = target;
                this.player_target = null;
                force_action = true;
                character.Escape(target.gameObject);
            }
        }

        public void MoveToTarget(Vector3 pos, bool run)
        {
            is_running = run;
            force_action = true;
            ChangeState(AnimalState.MoveTo);
            character.MoveTo(pos);
        }

        public void StopAction()
        {
            character.Stop();
            is_running = false;
            force_action = false;
            player_target = null;
            attack_target = null;
            ChangeState(AnimalState.Wander);
        }

        public void ChangeState(AnimalState state)
        {
            this.state = state;
            state_timer = 0f;
            lure_interest = 8f;
        }

        public void Reset()
        {
            StopAction();
            character.Reset();
            animator.Rebind();
        }

        private void OnDamaged()
        {
            if (IsDead())
                return;

            if (onDamaged != null)
                onDamaged.Invoke();

            if (animator != null)
                animator.SetTrigger("Damaged");
        }

        private void OnDamagedPlayer(PlayerCharacter player)
        {
            if (IsDead() || state_timer < 2f)
                return;

            player_target = player;
            attack_target = null;
            ReactToThreat();
        }

        private void OnDamagedBy(Destructible attacker)
        {
            if (IsDead() || state_timer < 2f)
                return;

            player_target = null;
            attack_target = attacker;
            ReactToThreat();
        }

        private void OnDeath()
        {
            state = AnimalState.Dead;

            if (onDeath != null)
                onDeath.Invoke();

            if (animator != null)
                animator.SetTrigger("Death");
        }

        void OnAttack()
        {
            if (animator != null)
                animator.SetTrigger("Attack");
        }

        public bool HasAttackBehavior()
        {
            return behavior == AnimalBehavior.Aggressive || behavior == AnimalBehavior.VeryAggressive || behavior == AnimalBehavior.PassiveDefense;
        }

        public bool HasEscapeBehavior()
        {
            return behavior == AnimalBehavior.Escape || behavior == AnimalBehavior.PassiveEscape;
        }

        public bool IsDead()
        {
            return character.IsDead();
        }

        public bool IsActive()
        {
            return is_active;
        }

        public bool IsMoving()
        {
            return character.IsMoving();
        }

        public bool IsRunning()
        {
            return character.IsMoving() && is_running;
        }

        public string GetUID()
        {
            return character.GetUID();
        }
    }

}
