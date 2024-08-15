using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterAttribute : MonoBehaviour
    {
        [Header("Attributes")]
        public AttributeData[] attributes;

        public UnityAction onGainLevel;

        private PlayerCharacter character;

        private float move_speed_mult = 1f;
        private float attack_mult = 1f;
        private bool depleting = false;

        private void Awake()
        {
            character = GetComponent<PlayerCharacter>();
        }

        void Start()
        {
            //Init attributes
            foreach (AttributeData attr in attributes)
            {
                if (!CharacterData.HasAttribute(attr.type))
                    CharacterData.SetAttributeValue(attr.type, attr.start_value, attr.max_value);
            }
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (character.IsDead())
                return;

            //Update attributes
            float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();

            //Update Attributes
            foreach (AttributeData attr in attributes)
            {
                float update_value = attr.value_per_hour + GetBonusEffectTotal(BonusEffectData.GetAttributeBonusType(attr.type));
				if(TheGame.Get().IsNight()) update_value += attr.value_per_hour_night;
                update_value = update_value * game_speed * Time.deltaTime;
                CharacterData.AddAttributeValue(attr.type, update_value, GetAttributeMax(attr.type));
            }

            //Penalty for depleted attributes
            move_speed_mult = 1f;
            attack_mult = 1f;
            depleting = false;

            foreach (AttributeData attr in attributes)
            {
				float currentVal = GetAttributeValue(attr.type);
                if (( currentVal < 0.01f && !attr.invert) || (currentVal > attr.max_value - 0.01f && attr.invert))
                {
                    move_speed_mult = move_speed_mult * attr.deplete_move_mult;
                    attack_mult = attack_mult * attr.deplete_attack_mult;
                    float update_value = attr.deplete_hp_loss * game_speed * Time.deltaTime;
                    AddAttribute(AttributeType.Health, update_value);
                    if (attr.deplete_hp_loss < 0f)
                        depleting = true;
                }
            }

            //Dying
            float health = GetAttributeValue(AttributeType.Health);
            if (health < 0.01f)
                character.Kill();

            //Sleeps add attributes
            if (character.IsSleeping())
            {
                ActionSleep sleep_target = character.GetSleepTarget();
                AddAttribute(AttributeType.Health, sleep_target.sleep_hp_hour * game_speed * Time.deltaTime);
                AddAttribute(AttributeType.Hunger, sleep_target.sleep_hunger_hour * game_speed * Time.deltaTime);
                AddAttribute(AttributeType.Thirst, sleep_target.sleep_thirst_hour * game_speed * Time.deltaTime);
                AddAttribute(AttributeType.Stress, sleep_target.sleep_hapiness_hour * game_speed * Time.deltaTime);
            }
        }
		
        public void AddAttribute(AttributeType type, float value)
        {
            if(HasAttribute(type))
                CharacterData.AddAttributeValue(type, value, GetAttributeMax(type));
        }

        public void SetAttribute(AttributeType type, float value)
        {
            if (HasAttribute(type))
                CharacterData.SetAttributeValue(type, value, GetAttributeMax(type));
        }

        public void ResetAttribute(AttributeType type)
        {
            AttributeData adata = GetAttribute(type);
            if (adata != null)
                CharacterData.SetAttributeValue(type, adata.start_value, GetAttributeMax(type));
        }

        public float GetAttributeValue(AttributeType type)
        {
            return CharacterData.GetAttributeValue(type);
        }

        public float GetAttributeMax(AttributeType type)
        {
            AttributeData adata = GetAttribute(type);
            if (adata != null)
                return adata.max_value + GetBonusEffectTotal(BonusEffectData.GetAttributeMaxBonusType(type));
            return 100f;
        }

        public AttributeData GetAttribute(AttributeType type)
        {
            foreach (AttributeData attr in attributes)
            {
                if (attr.type == type)
                    return attr;
            }
            return null;
        }

        public bool HasAttribute(AttributeType type)
        {
            return GetAttribute(type) != null;
        }

        public void GainXP(string id, int xp)
        {
            if (xp > 0)
            {
                CharacterData.GainXP(id, xp);
                CheckLevel(id);
            }
        }

        private void CheckLevel(string id)
        {
            PlayerLevelData ldata = CharacterData.GetLevelData(id);
            LevelData current = LevelData.GetLevel(id, ldata.level);
            LevelData next = LevelData.GetLevel(id, ldata.level + 1);
            if (current != null && next != null && current != next && ldata.xp >= next.xp_required)
            {
                GainLevel(id);
                CheckLevel(id); //Check again if it increased by 2+ levels
            }
        }

        public void GainLevel(string id)
        {
            CharacterData.GainLevel(id);

            int alevel = CharacterData.GetLevel(id);
            LevelData level = LevelData.GetLevel(id, alevel);
            if (level != null)
            {
                foreach (CraftData unlock in level.unlock_craft)
                    character.Crafting.LearnCraft(unlock.id);
            }

            onGainLevel?.Invoke();
        }

        public int GetLevel(string id)
        {
            return CharacterData.GetLevel(id);
        }

        public int GetXP(string id)
        {
            return CharacterData.GetXP(id);
        }

        public float GetBonusEffectTotal(BonusType type, GroupData[] targets = null)
        {
            float value = GetBonusEffectTotalSingle(type, null);
            if (targets != null)
            {
                foreach (GroupData target in targets)
                    value += GetBonusEffectTotalSingle(type, target);
            }
            return value;
        }

        public float GetBonusEffectTotalSingle(BonusType type, GroupData target)
        {
            float value = 0f;

            //Level bonus
            value += CharacterData.GetLevelBonusValue(type, target);

            //Equip bonus
            foreach (KeyValuePair<int, InventoryItemData> pair in character.EquipData.items)
            {
                ItemData idata = ItemData.Get(pair.Value?.item_id);
                if (idata != null)
                {
                    foreach (BonusEffectData bonus in idata.equip_bonus)
                    {
                        if (bonus.type == type && bonus.target == target)
                            value += bonus.value;
                    }
                }
            }

            //Aura bonus
            foreach (BonusAura aura in BonusAura.GetAll())
            {
                float dist = (aura.transform.position - transform.position).magnitude;
                if (aura.effect.type == type && aura.effect.target == target && dist < aura.range)
                    value += aura.effect.value;
            }

            //Timed bonus
            if(target == null)
                value += CharacterData.GetTotalTimedBonus(type);

            return value;
        }

        public float GetSpeedMult()
        {
            return Mathf.Max(move_speed_mult, 0.01f);
        }

        public float GetAttackMult()
        {
            return Mathf.Max(attack_mult, 0.01f);
        }

        public bool IsDepletingHP()
        {
			return false;
            return depleting;
        }

        public PlayerCharacterData CharacterData
        {
            get { return character.SaveData; }
        }

        public PlayerCharacter GetCharacter()
        {
            return character;
        }
    }
}
