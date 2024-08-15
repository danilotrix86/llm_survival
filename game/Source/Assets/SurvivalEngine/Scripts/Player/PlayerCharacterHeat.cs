using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Add this script to the player character to add the heat/cold system
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterHeat : MonoBehaviour
    {
        [Header("Global Temperature")]
        public float global_heat = 25f; //Global heat without any source, change this value if changing season, use negative value to represent cold
        public float global_heat_weight = 1f; //Weight of the global heat

        [Header("Character Resistance")]
        public float heat_change_speed = 25f; //How fast does heat can change per hour (before applying cold resist)
        public float cold_resist = 0f; //Higher the value, will resist to cold better (this value can be increased by bonuses)

        [Header("Cold Threshold")]
        public float cold_threshold = 10f; //Below this heat, ice FX will start appearing
        public float damage_threshold = 5f; //Below this heat, character will receive damage
        public float damage_hp_loss = -20f; //Damage per game-hours when below damage threshold

        private PlayerCharacter character;

        private static List<PlayerCharacterHeat> character_list = new List<PlayerCharacterHeat>();

        void Awake()
        {
            character_list.Add(this);
            character = GetComponent<PlayerCharacter>();
        }

        private void OnDestroy()
        {
            character_list.Remove(this);
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();

            //Calculate heat
            float total_heat = global_heat * global_heat_weight;
            float total_heat_weight = global_heat_weight;

            foreach (HeatSource source in HeatSource.GetAll())
            {
                float dist = (source.transform.position - transform.position).magnitude;
                if (source.enabled && dist < source.heat_range)
                {
                    total_heat += source.heat * source.heat_weight;
                    total_heat_weight += source.heat_weight;
                }
            }

            //Character heat will move toward this value
            float average_heat = total_heat / total_heat_weight;
            float current_heat = character.Attributes.GetAttributeValue(AttributeType.Heat);
            float change_speed = heat_change_speed;
            float dir = average_heat - current_heat;

            //Cold resist
            if (dir < 0f)
            {
                float resist = cold_resist + character.Attributes.GetBonusEffectTotal(BonusType.ColdResist);
                change_speed = change_speed / (1f + resist);
            }

            //Update heat
            if (Mathf.Abs(dir) > 0.1f) {
                
                current_heat += Mathf.Sign(dir) * change_speed * game_speed * Time.deltaTime;
                character.Attributes.SetAttribute(AttributeType.Heat, current_heat);
            }

            //Deal damage
            if (current_heat < damage_threshold + 0.01f)
            {
                float update_value = damage_hp_loss * game_speed * Time.deltaTime;
                character.Attributes.AddAttribute(AttributeType.Health, update_value);
            }

            //Debug.Log(average_heat + " " + current_heat + " " + change_speed);
        }

        public bool IsCold()
        {
            float current_heat = character.Attributes.GetAttributeValue(AttributeType.Heat);
            return current_heat < cold_threshold + 0.01f;
        }

        public bool IsColdDamage()
        {
            float current_heat = character.Attributes.GetAttributeValue(AttributeType.Heat);
            return current_heat < damage_threshold + 0.01f;
        }

        //Use this function if changing season and the temperature is changing
        public static void SetGlobalHeat(float value)
        {
            foreach (PlayerCharacterHeat character in character_list)
                character.global_heat = value;
        }

        public static PlayerCharacterHeat Get(int player_id = 0)
        {
            foreach (PlayerCharacterHeat player in character_list)
            {
                if (player.character.player_id == player_id)
                    return player;
            }
            return null;
        }

        public static List<PlayerCharacterHeat> GetAll()
        {
            return character_list;
        }
    }

}