using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Will damage animals when they step on it
    /// </summary>

    public class Trap : MonoBehaviour
    {
        public int damage = 50;
        public GroupData target_group; //If set, will only trap that group, if not set, will trap all characters

        public GameObject active_model;
        public GameObject triggered_model;

        private Construction construct;
        private Buildable buildable;
        private bool triggered = false;
        private float trigger_timer = 0f;

        void Start()
        {
            active_model.SetActive(true);
            triggered_model.SetActive(false);
            construct = GetComponent<Construction>();
            buildable = GetComponent<Buildable>();
        }

        void Update()
        {
            trigger_timer += Time.deltaTime;

        }

        //Trigger will 'close' the trap and damage the animal triggering it
        public void Trigger(Character triggerer)
        {
            if (buildable != null && buildable.IsBuilding())
                return;

            if (!triggered && trigger_timer > 2f)
            {
                triggered = true;
                active_model.SetActive(false);
                triggered_model.SetActive(true);

                //Durability
                if (construct != null) {
                    BuiltConstructionData bdata = PlayerData.Get().GetConstructed(construct.GetUID());
                    if (bdata != null && construct.data != null && construct.data.durability_type == DurabilityType.UsageCount)
                        bdata.durability -= 1f;
                }

                //Deal damage
                if (triggerer)
                    triggerer.GetDestructible().TakeDamage(damage);
            }
        }

        //Activate 'opens' the trap, it will be ready to be triggered
        public void Activate()
        {
            if (triggered)
            {
                triggered = false;
                active_model.SetActive(true);
                triggered_model.SetActive(false);
                trigger_timer = 0f;
            }
        }

        public bool IsActive()
        {
            return !triggered;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!triggered)
            {
                Character character = other.GetComponent<Character>();
                if (character != null)
                {
                    if (target_group == null || character.HasGroup(target_group))
                        Trigger(character);
                }
            }
        }
    }

}