using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    [RequireComponent(typeof(Selectable))]
    public class Ladder : MonoBehaviour
    {
        public Vector3 top_jump_offset;

        private Selectable select;
        private Collider collide;

        private bool front_blocked = false;
        private bool back_blocked = false;

        private void Awake()
        {
            select = GetComponent<Selectable>();
            collide = GetComponentInChildren<Collider>();

            select.onUse += ClimbLadder;
        }

        private void Start()
        {
            front_blocked = PhysicsTool.RaycastCollision(transform.position + Vector3.up * 0.5f, transform.forward, out RaycastHit hit1);
            back_blocked = PhysicsTool.RaycastCollision(transform.position + Vector3.up * 0.5f, -transform.forward, out RaycastHit hit2);
        }

        public void ClimbLadder(PlayerCharacter player)
        {
            if(player.Climbing != null)
                player.Climbing.Climb(this);
        }

        public Bounds GetBounds()
        {
            return collide.bounds;
        }

        public Vector3 GetOffsetDir(Vector3 player_pos)
        {
            Vector3 dir = player_pos - transform.position;
            return Vector3.Project(dir, transform.forward);
        }

        public bool IsSideBlocked(Vector3 dir)
        {
            float dot = Vector3.Dot(dir, transform.forward);
            if(dot > 0f) return IsFrontBlocked();
            else return IsBackBlocked();
        }

        public bool IsFrontBlocked() { return front_blocked; }
        public bool IsBackBlocked() { return back_blocked; }
    }
}
