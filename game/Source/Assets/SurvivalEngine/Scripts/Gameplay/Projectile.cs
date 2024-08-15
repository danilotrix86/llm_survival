using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// A projectile shot with a ranged weapon
    /// </summary>

    public class Projectile : MonoBehaviour
    {
        public float speed = 10f;
        public float duration = 10f;
        public float gravity = 0.2f;

        public AudioClip shoot_sound;
		
		[HideInInspector]
        public int damage = 0; //Will be replaced by weapon damage

        [HideInInspector]
        public Vector3 dir;

        [HideInInspector]
        public PlayerCharacter player_shooter;

        [HideInInspector]
        public Destructible shooter;

        private Vector3 curve_dir = Vector3.zero;
        private float curve_dist = 0f;
        private float timer = 0f;

        void Start()
        {
            TheAudio.Get().PlaySFX("projectile", shoot_sound);
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (curve_dist > 0.01f && (timer * speed) < curve_dist)
            {
                //Initial curved dir (only in freelook mode)
                float value = Mathf.Clamp01(timer * speed / curve_dist);
                Vector3 cdir = (1f - value) * curve_dir + value * dir;
                transform.position += cdir * speed * Time.deltaTime;
                transform.rotation = Quaternion.LookRotation(cdir.normalized, Vector2.up);
            }
            else
            {
                //Regular dir
                transform.position += dir * speed * Time.deltaTime;
                dir += gravity * Vector3.down * Time.deltaTime;
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector2.up);
            }

            timer += Time.deltaTime;
            if (timer > duration)
                Destroy(gameObject);
        }

        public void SetInitialCurve(Vector3 dir, float dist = 10f)
        {
            curve_dir = dir;
            curve_dist = dist * 1.25f; //Add offset for more accuracy
        }

        private void OnTriggerEnter(Collider collision)
        {
            Destructible destruct = collision.GetComponent<Destructible>();
            if (destruct != null && !destruct.attack_melee_only)
            {
                if(player_shooter != null)
                    destruct.TakeDamage(player_shooter, damage);
                else if (shooter != null)
                    destruct.TakeDamage(shooter, damage);
                else
                    destruct.TakeDamage(damage);
                Destroy(gameObject);
            }

            PlayerCharacterCombat player = collision.GetComponent<PlayerCharacterCombat>();
            if (player != null && (player_shooter == null || player_shooter.Combat != player))
            {
                player.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }

}