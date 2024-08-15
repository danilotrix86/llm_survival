using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    public enum PetState
    {
        Idle=0,
        Follow = 2,
        Attack = 5,
        Dig = 8,
        Pet = 10,
        MoveTo=15,
        Dead = 20,
    }

    /// <summary>
    /// Pet behavior script for following player, attacking enemies, and digging
    /// </summary>

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Destructible))]
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(UniqueID))]
    public class Pet : MonoBehaviour
    {
        [Header("Actions")]
        public float follow_range = 3f;
        public float detect_range = 5f;
        public float wander_range = 4f;
        public float action_duration = 10f;
        public bool can_attack = false;
        public bool can_dig = false;

        public UnityAction onAttack;
        public UnityAction onDamaged;
        public UnityAction onDeath;
        public UnityAction onPet;

        private Character character;
        private Destructible destruct;
        private UniqueID unique_id;

        private PetState state;
        private Vector3 start_pos;
        private Animator animator;

        private Destructible attack_target = null;
        private GameObject action_target = null;
        private Vector3 wander_target;

        private int master_player = -1;
        private bool follow = false;
        private float state_timer = 0f;
        private bool force_action = false;

        void Awake()
        {
            character = GetComponent<Character>();
            destruct = GetComponent<Destructible>();
            unique_id = GetComponent<UniqueID>();
            animator = GetComponentInChildren<Animator>();
            start_pos = transform.position;
            wander_target = transform.position;

            character.onAttack += OnAttack;
            destruct.onDamaged += OnTakeDamage;
            destruct.onDeath += OnKill;

            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        private void Start()
        {
            if (PlayerData.Get().IsObjectRemoved(GetUID()))
            {
                Destroy(gameObject);
                return;
            }

            if (PlayerData.Get().HasCustomInt(unique_id.GetSubUID("master")))
            {
                master_player = PlayerData.Get().GetCustomInt(unique_id.GetSubUID("master"));
                Follow();
            }
        }

        private void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (state == PetState.Dead)
                return;

            state_timer += Time.deltaTime;

            //States
            if (state == PetState.Idle)
            {
                if (follow && state_timer > 2f && HasMaster())
                    ChangeState(PetState.Follow);

                if (state_timer > 5f && wander_range > 0.1f)
                {
                    state_timer = Random.Range(-1f, 1f);
                    FindWanderTarget();
                    character.MoveTo(wander_target);
                }

                //Character stuck
                if (character.IsStuck())
                    character.Stop();
            }

            if (state == PetState.Follow)
            {
                if(HasMaster() && !IsMoving() && PlayerIsFar(follow_range))
                    character.Follow(GetMaster().gameObject);

                if (!follow)
                    ChangeState(PetState.Idle);

                DetectAction();
            }

            if (state == PetState.Dig)
            {
                if (action_target == null)
                {
                    StopAction();
                    return;
                }

                Vector3 dir = action_target.transform.position - transform.position;
                if (dir.magnitude < 1f)
                {
                    character.Stop();
                    character.FaceTorward(action_target.transform.position);

                    if (animator != null)
                        animator.SetTrigger("Dig");
                    StartCoroutine(DigRoutine());
                }

                if (state_timer > 10f)
                {
                    StopAction();
                }
            }

            if (state == PetState.Attack)
            {
                if (attack_target == null || attack_target.IsDead())
                {
                    StopAction();
                    return;
                }

                Vector3 targ_dir = attack_target.transform.position - transform.position;
                if (!force_action && state_timer > action_duration)
                {
                    if (targ_dir.magnitude > detect_range || PlayerIsFar(detect_range * 2f))
                    {
                        StopAction();
                    }
                }

                if (targ_dir.y > 10f)
                    StopAction(); //Bird too high
            }

            if (state == PetState.Pet)
            {
                if (state_timer > 2f)
                {
                    if (HasMaster())
                        ChangeState(PetState.Follow);
                    else
                        ChangeState(PetState.Idle);
                }
            }

            if (state == PetState.MoveTo)
            {
                if (character.HasReachedMoveTarget())
                    StopAction();
            }

            if (animator != null)
            {
                animator.SetBool("Move", IsMoving());
            }
        }

        private IEnumerator DigRoutine()
        {
            yield return new WaitForSeconds(1f);

            if (action_target != null)
            {
                DigSpot dig = action_target.GetComponent<DigSpot>();
                if (dig != null)
                    dig.Dig();
            }

            StopAction();
        }

        private void DetectAction()
        {
            if (PlayerIsFar(detect_range))
                return;

            foreach (Selectable selectable in Selectable.GetAllActive())
            {
                if (selectable.gameObject != gameObject)
                {
                    Vector3 dir = (selectable.transform.position - transform.position);
                    if (dir.magnitude < detect_range)
                    {
                        DigSpot dig = selectable.GetComponent<DigSpot>();
                        Destructible destruct = selectable.GetComponent<Destructible>();

                        if (can_attack && destruct && destruct.target_team == AttackTeam.Enemy && destruct.required_item == null)
                        {
                            attack_target = destruct;
                            action_target = null;
                            character.Attack(destruct);
                            ChangeState(PetState.Attack);
                            return;
                        }

                        else if (can_dig && dig != null)
                        {
                            attack_target = null;
                            action_target = dig.gameObject;
                            ChangeState(PetState.Dig);
                            character.MoveTo(dig.transform.position);
                            return;
                        }
                    }
                }
            }
        }

        public void PetPet()
        {
            StopAction();
            ChangeState(PetState.Pet);
            if (animator != null)
                animator.SetTrigger("Pet");
        }

        public void TamePet(PlayerCharacter player)
        {
            if (player != null && character.data != null && !HasMaster() && unique_id.HasUID())
            {
                PetPet();
                follow = true;

                //Create a new character so that the pet can change scene
                string prev_uid = unique_id.unique_id;
                TrainedCharacterData prev_cdata = PlayerData.Get().GetCharacter(prev_uid);
                if (prev_cdata == null)
                {
                    TrainedCharacterData cdata = PlayerData.Get().AddCharacter(character.data.id, SceneNav.GetCurrentScene(), transform.position, transform.rotation);
                    unique_id.SetUID(cdata.uid);
                    PlayerData.Get().RemoveObject(prev_uid);
                }

                //Set master
                master_player = player.player_id;
                player.SaveData.AddPet(unique_id.unique_id, character.data.id);
                PlayerData.Get().SetCustomInt(unique_id.GetSubUID("master"), player.player_id);
            }
        }

        public void UntamePet()
        {
            if (HasMaster() && unique_id.HasUID())
            {
                StopAction();

                //Remove master
                PlayerCharacter master = GetMaster();
                master.SaveData.RemovePet(unique_id.unique_id);
                master_player = -1;
                PlayerData.Get().SetCustomInt(unique_id.GetSubUID("master"), -1);
            }
        }

        public void Follow()
        {
            if (HasMaster())
            {
                StopAction();
                follow = true;
                ChangeState(PetState.Follow);
            }
        }

        public void StopFollow()
        {
            follow = false;
            StopAction();
            ChangeState(PetState.Idle);
        }

        public void AttackTarget(Destructible target)
        {
            if (target != null)
            {
                attack_target = target;
                action_target = null;
                force_action = true;
                character.Attack(target);
                ChangeState(PetState.Attack);
            }
        }

        public void MoveToTarget(Vector3 pos)
        {
            force_action = true;
            attack_target = null;
            action_target = null;
            ChangeState(PetState.MoveTo);
            character.MoveTo(pos);
        }

        public void StopAction()
        {
            character.Stop();
            attack_target = null;
            action_target = null;
            force_action = false;
            ChangeState(PetState.Idle);
        }

        private void FindWanderTarget()
        {
            float range = Random.Range(0f, wander_range);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * range;
            wander_target = pos;
        }

        public void ChangeState(PetState state)
        {
            this.state = state;
            state_timer = 0f;
        }

        private void OnAttack()
        {
            if (animator != null)
                animator.SetTrigger("Attack");

            if (onAttack != null)
                onAttack.Invoke();
        }

        private void OnTakeDamage()
        {
            if (IsDead())
                return;

            if (onDamaged != null)
                onDamaged.Invoke();
        }

        private void OnKill()
        {
            state = PetState.Dead;

            if (animator != null)
                animator.SetTrigger("Death");

            if (onDeath != null)
                onDeath.Invoke();
        }


        public bool PlayerIsFar(float distance)
        {
            if (HasMaster())
            {
                PlayerCharacter master = GetMaster();
                Vector3 dir = master.transform.position - transform.position;
                return dir.magnitude > distance;
            }
            return false;
        }

        public PlayerCharacter GetMaster()
        {
            return PlayerCharacter.Get(master_player);
        }

        public bool HasMaster()
        {
            return master_player >= 0;
        }

        public bool IsFollow()
        {
            return follow;
        }

        public bool IsDead()
        {
            return character.IsDead();
        }

        public bool IsMoving()
        {
            return character.IsMoving();
        }

        public string GetUID()
        {
            return character.GetUID();
        }
    }

}
