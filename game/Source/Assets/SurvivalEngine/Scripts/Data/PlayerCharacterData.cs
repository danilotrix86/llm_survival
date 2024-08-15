using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    [System.Serializable]
    public class TimedBonusData
    {
        public BonusType bonus;
        public float time;
        public float value;
    }

    [System.Serializable]
    public class PlayerLevelData
    {
        public string id;
        public int level;
        public int xp;
    }

    [System.Serializable]
    public class PlayerPetData
    {
        public string pet_id;
        public string uid;
    }

    [System.Serializable]
    public class PlayerCharacterData
    {
        public int player_id;

        public Vector3Data position;
        public int gold = 0;

        public Dictionary<string, PlayerLevelData> levels = new Dictionary<string, PlayerLevelData>();
        public Dictionary<AttributeType, float> attributes = new Dictionary<AttributeType, float>();
        public Dictionary<BonusType, TimedBonusData> timed_bonus_effects = new Dictionary<BonusType, TimedBonusData>();
        public Dictionary<string, int> crafted_count = new Dictionary<string, int>();
        public Dictionary<string, int> kill_count = new Dictionary<string, int>();
        public Dictionary<string, bool> unlocked_ids = new Dictionary<string, bool>();
        public Dictionary<string, PlayerPetData> pets = new Dictionary<string, PlayerPetData>();

        public PlayerCharacterData(int id) { player_id = id; }

        public void FixData()
        {
            //Fix data to make sure old save files compatible with new game version
            if (levels == null)
                levels = new Dictionary<string, PlayerLevelData>();
            if (attributes == null)
                attributes = new Dictionary<AttributeType, float>();
            if (unlocked_ids == null)
                unlocked_ids = new Dictionary<string, bool>();
            if (timed_bonus_effects == null)
                timed_bonus_effects = new Dictionary<BonusType, TimedBonusData>();

            if (crafted_count == null)
                crafted_count = new Dictionary<string, int>();
            if (kill_count == null)
                kill_count = new Dictionary<string, int>();

            if (pets == null)
                pets = new Dictionary<string, PlayerPetData>();
        }

        //--- Attributes ----

        public bool HasAttribute(AttributeType type)
        {
            return attributes.ContainsKey(type);
        }

        public float GetAttributeValue(AttributeType type)
        {
            if (attributes.ContainsKey(type))
                return attributes[type];
            return 0f;
        }

        public void SetAttributeValue(AttributeType type, float value, float max)
        {
            attributes[type] = Mathf.Clamp(value, 0f, max);
        }

        public void AddAttributeValue(AttributeType type, float value, float max)
        {
            if (!attributes.ContainsKey(type))
                attributes[type] = value;
            else
                attributes[type] += value;

            attributes[type] = Mathf.Clamp(attributes[type], 0f, max);
        }

        public void AddTimedBonus(BonusType type, float value, float duration)
        {
            TimedBonusData new_bonus = new TimedBonusData();
            new_bonus.bonus = type;
            new_bonus.value = value;
            new_bonus.time = duration;

            if (!timed_bonus_effects.ContainsKey(type) || timed_bonus_effects[type].time < duration)
                timed_bonus_effects[type] = new_bonus;
        }

        public void RemoveTimedBonus(BonusType type)
        {
            if (timed_bonus_effects.ContainsKey(type))
                timed_bonus_effects.Remove(type);
        }

        public float GetTotalTimedBonus(BonusType type)
        {
            if (timed_bonus_effects.ContainsKey(type) && timed_bonus_effects[type].time > 0f)
                return timed_bonus_effects[type].value;
            return 0f;
        }

        // ---- Levels ------

        public void GainXP(string id, int xp)
        {
            PlayerLevelData ldata = GetLevelData(id);
            ldata.xp += xp;
        }

        public void SetXP(string id, int xp)
        {
            PlayerLevelData ldata = GetLevelData(id);
            ldata.xp = xp;
        }

        public void GainLevel(string id)
        {
            PlayerLevelData ldata = GetLevelData(id);
            LevelData current = LevelData.GetLevel(id, ldata.level);
            LevelData next = LevelData.GetLevel(id, ldata.level + 1);
            if (next != null && current != next)
            {
                ldata.level = next.level;
                ldata.xp = Mathf.Max(ldata.xp, next.xp_required);
            }
        }

        public void SetLevel(string id, int level)
        {
            PlayerLevelData ldata = GetLevelData(id);
            LevelData current = LevelData.GetLevel(id, ldata.level);
            LevelData next = LevelData.GetLevel(id, level);
            if (next != null && current != next)
            {
                ldata.level = next.level;
                ldata.xp = Mathf.Max(ldata.xp, next.xp_required);
            }
        }

        public int GetLevel(string id)
        {
            PlayerLevelData ldata = GetLevelData(id);
            return ldata.level;
        }

        public int GetXP(string id)
        {
            PlayerLevelData ldata = GetLevelData(id);
            return ldata.xp;
        }

        public PlayerLevelData GetLevelData(string id)
        {
            if (levels.ContainsKey(id))
                return levels[id];
            PlayerLevelData data = new PlayerLevelData();
            data.id = id;
            data.level = 1;
            levels[id] = data;
            return data;
        }

        public float GetLevelBonusValue(BonusType type, GroupData target = null)
        {
            float val = 0f;
            foreach (KeyValuePair<string, PlayerLevelData> pair in levels)
            {
                PlayerLevelData ldata = pair.Value;
                LevelData level = LevelData.GetLevel(ldata.id, ldata.level);
                if (level != null)
                {
                    foreach (LevelUnlockBonus bonus in level.unlock_bonuses)
                    {
                        if (bonus.bonus == type && target == bonus.target_group)
                            val += bonus.bonus_value;
                    }
                }
            }
            return val;
        }

        // ---- Unlock groups -----

        public void UnlockID(string id)
        {
            if (!string.IsNullOrEmpty(id))
                unlocked_ids[id] = true;
        }

        public void RemoveUnlockedID(string id)
        {
            if (unlocked_ids.ContainsKey(id))
                unlocked_ids.Remove(id);
        }

        public bool IsIDUnlocked(string id)
        {
            if (unlocked_ids.ContainsKey(id))
                return unlocked_ids[id];
            return false;
        }

        // --- Craftable crafted
        public void AddCraftCount(string craft_id, int value = 1)
        {
            if (!string.IsNullOrEmpty(craft_id))
            {
                if (crafted_count.ContainsKey(craft_id))
                    crafted_count[craft_id] += value;
                else
                    crafted_count[craft_id] = value;

                if(crafted_count[craft_id] <= 0)
                    crafted_count.Remove(craft_id);
            }
        }

        public int GetCraftCount(string craft_id)
        {
            if (crafted_count.ContainsKey(craft_id))
                return crafted_count[craft_id];
            return 0;
        }

        public void ResetCraftCount(string craft_id)
        {
            if (crafted_count.ContainsKey(craft_id))
                crafted_count.Remove(craft_id);
        }

        public void ResetCraftCount()
        {
            crafted_count.Clear();
        }

        // --- Killed things
        public void AddKillCount(string craft_id, int value = 1)
        {
            if (!string.IsNullOrEmpty(craft_id))
            {
                if (kill_count.ContainsKey(craft_id))
                    kill_count[craft_id] += value;
                else
                    kill_count[craft_id] = value;

                if (kill_count[craft_id] <= 0)
                    kill_count.Remove(craft_id);
            }
        }

        public int GetKillCount(string craft_id)
        {
            if (kill_count.ContainsKey(craft_id))
                return kill_count[craft_id];
            return 0;
        }

        public void ResetKillCount(string craft_id)
        {
            if (kill_count.ContainsKey(craft_id))
                kill_count.Remove(craft_id);
        }

        public void ResetKillCount()
        {
            kill_count.Clear();
        }

        //----- Pet owned by player ----
        public void AddPet(string uid, string pet_id)
        {
            PlayerPetData pet = new PlayerPetData();
            pet.pet_id = pet_id;
            pet.uid = uid;
            pets[uid] = pet;
        }

        public void RemovePet(string uid)
        {
            if (pets.ContainsKey(uid))
                pets.Remove(uid);
        }

        public static PlayerCharacterData Get(int player_id)
        {
            return PlayerData.Get().GetPlayerCharacter(player_id);
        }
    }
}