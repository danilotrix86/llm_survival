using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Birds are alternate version of the animal script but with flying!
    /// </summary>

    public enum BirdState
    {
        Sit = 0,
        Fly = 2,
        FlyDown = 4,
        Alerted = 5,
        Dead = 10,
    }

    [RequireComponent(typeof(Character))]
    public class Bird : MonoBehaviour
    {
        [Header("Fly")]
        public float wander_radius = 10f;
        public float fly_duration = 20f;
        public float sit_duration = 20f;

        [Header("Vision")]
        public float detect_range = 5f;
        public float detect_angle = 360f;
        public float detect_360_range = 1f;
        public float reaction_time = 0.2f;

        [Header("Models")]
        public Animator sit_model;
        public Animator fly_model;

        private Character character;
        private Destructible destruct;
        private Collider[] colliders;
        private BirdState state = BirdState.Sit;
        private float state_timer = 0f;
        private Vector3 start_pos;
        private Vector3 target_pos;
        private float update_timer = 0f;

        private void Awake()
        {
            character = GetComponent<Character>();
            destruct = GetComponent<Destructible>();
            colliders = GetComponentsInChildren<Collider>();
            start_pos = transform.position;
            target_pos = transform.position;
            destruct.onDeath += OnDeath;
            state_timer = 99f; //Fly right away
            update_timer = Random.Range(-1f, 1f);

            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        private void Start()
        {
            StopFly();
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            state_timer += Time.deltaTime;

            if (state == BirdState.Sit)
            {
                if (state_timer > sit_duration)
                {
                    FlyAway();
                }
            }

            if (state == BirdState.Alerted)
            {
                if (state_timer > reaction_time)
                {
                    FlyAway();
                }
            }

            if (state == BirdState.Fly)
            {
                if (fly_model.gameObject.activeSelf && character.HasReachedMoveTarget())
                    fly_model.gameObject.SetActive(false);

                if (state_timer > fly_duration)
                {
                    StopFly();
                }
            }

            if (state == BirdState.FlyDown)
            {
                if (character.HasReachedMoveTarget())
                {
                    Land();
                }
            }

            update_timer += Time.deltaTime;
            if (update_timer > 0.5f)
            {
                update_timer = Random.Range(-0.1f, 0.1f);
                SlowUpdate(); //Optimization
            }
        }

        private void SlowUpdate()
        {
            if (state == BirdState.Sit)
            {
                DetectThreat();
            }
        }

        public void FlyAway()
        {
            state_timer = 0f;
            FindFlyPosition(transform.position, wander_radius, out target_pos);
            state = BirdState.Fly;
            sit_model.gameObject.SetActive(false);
            fly_model.gameObject.SetActive(true);
            character.MoveTo(target_pos);

            foreach (Collider collide in colliders)
                collide.enabled = false;
        }

        public void StopFly()
        {
            state_timer = 0f;
            Vector3 npos;
            bool succes = FindGroundPosition(start_pos, wander_radius, out npos);
            if (succes)
            {
                state = BirdState.FlyDown;
                target_pos = npos;
                fly_model.gameObject.SetActive(true);
                sit_model.gameObject.SetActive(false);
                character.MoveTo(target_pos);

                foreach (Collider collide in colliders)
                    collide.enabled = false;
            }
        }

        private void Land()
        {
            state_timer = Random.Range(-1f, 1f);
            state = BirdState.Sit;
            sit_model.gameObject.SetActive(true);
            fly_model.gameObject.SetActive(false);

            foreach (Collider collide in colliders)
                collide.enabled = true;
        }

        private void OnDeath()
        {
            StopMoving();
            state = BirdState.Dead;
            state_timer = 0f;
            sit_model.gameObject.SetActive(true);
            fly_model.gameObject.SetActive(false);
            sit_model.SetTrigger("Death");
        }

        private bool FindFlyPosition(Vector3 pos, float radius, out Vector3 fly_pos)
        {
            Vector3 offest = new Vector3(Random.Range(-radius, radius), 0f, Random.Range(radius, radius));
            fly_pos = pos + offest;
            fly_pos.y = start_pos.y + 20f;
            return true;
        }

        //Find landing position to make sure it wont land on an obstacle
        private bool FindGroundPosition(Vector3 pos, float radius, out Vector3 ground_pos)
        {
            Vector3 offest = new Vector3(Random.Range(-radius, radius), 0f, Random.Range(radius, radius));
            Vector3 center = pos + offest;
            bool found = PhysicsTool.FindGroundPosition(center, 50f, character.ground_layer.value, out ground_pos);
            return found;
        }

        //Detect if the player is in vision
        private void DetectThreat()
        {
            Vector3 pos = transform.position;

            //React to player
            foreach (PlayerCharacter player in PlayerCharacter.GetAll())
            {
                Vector3 char_dir = (player.transform.position - pos);
                if (char_dir.magnitude < detect_range)
                {
                    float dangle = detect_angle / 2f; // /2 for each side
                    float angle = Vector3.Angle(transform.forward, char_dir.normalized);
                    if (angle < dangle || char_dir.magnitude < detect_360_range)
                    {
                        state = BirdState.Alerted;
                        state_timer = 0f;
                        StopMoving();
                        return;
                    }
                }
            }

            //React to other characters
            foreach (Selectable selectable in Selectable.GetAllActive())
            {
                if (selectable.gameObject != gameObject)
                {
                    Vector3 dir = (selectable.transform.position - pos);
                    if (dir.magnitude < detect_range)
                    {
                        Character character = selectable.Character;
                        if (character && character.attack_enabled) //Only afraid if the character can attack
                        {
                            if (character.GetDestructible().target_group != this.destruct.target_group)
                            {
                                state = BirdState.Alerted;
                                state_timer = 0f;
                                StopMoving();
                                return;
                            }
                        }
                    }
                }
            }
        }

        public void StopMoving()
        {
            target_pos = transform.position;
            state_timer = 0f;
            character.Stop();
        }
    }

}