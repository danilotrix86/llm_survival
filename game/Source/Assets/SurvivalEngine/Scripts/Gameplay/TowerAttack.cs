using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SurvivalEngine
{
    public class TowerAttack : MonoBehaviour
    {
        public int attack_damage = 10;       //Basic damage 
        public float attack_range = 20f;   //How far can you attack
        public float attack_cooldown = 2f;  //Seconds of waiting in between each attack

        public Transform shoot_root;
        public GameObject projectile_prefab;

        private Buildable buildable;
        private Destructible destruct;
        private float timer = 0f;

        private void Awake()
        {
            buildable = GetComponent<Buildable>();
            destruct = GetComponent<Destructible>();
        }

        private void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (buildable != null && buildable.IsBuilding())
                return;

            timer += Time.deltaTime;
            if (timer > attack_cooldown)
            {
                timer = 0f;
                ShootNearestEnemy();
            }
        }

        public void ShootNearestEnemy()
        {
            Destructible nearest = Destructible.GetNearestAttack(AttackTeam.Enemy, transform.position, attack_range);
            Shoot(nearest);
        }

        public void Shoot(Destructible target)
        {
            if (target != null && projectile_prefab != null)
            {
                int damage = attack_damage;
                Vector3 pos = GetShootPos();
                Vector3 dir = target.GetCenter() - pos;
                GameObject proj = Instantiate(projectile_prefab, pos, Quaternion.LookRotation(dir.normalized, Vector3.up));
                Projectile project = proj.GetComponent<Projectile>();
                project.shooter = destruct;
                project.dir = dir.normalized;
                project.damage = damage;
            }
        }

        public Vector3 GetShootPos()
        {
            if (shoot_root != null)
                return shoot_root.position;
            return transform.position + Vector3.up * 2f;
        }
    }
}
