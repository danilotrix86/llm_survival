using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Type of level that can be increased (hunting, mining, etc).
    /// </summary>

    [CreateAssetMenu(fileName = "LevelData", menuName = "SurvivalEngine/LevelData", order = 11)]
    public class LevelData : ScriptableObject
    {
        public string id;
        public int level;
        public int xp_required;

        [Space(5)]
        public LevelUnlockBonus[] unlock_bonuses;
        public CraftData[] unlock_craft;

        private static List<LevelData> level_data = new List<LevelData>();

        public static void Load(string folder = "")
        {
            level_data.Clear();
            level_data.AddRange(Resources.LoadAll<LevelData>(folder));
        }

        public static LevelData GetLevel(string id, int level)
        {
            foreach (LevelData data in level_data)
            {
                if (data.id == id && data.level == level)
                {
                    return data;
                }
            }
            return GetMaxLevel(id);
        }

        public static LevelData GetMaxLevel(string id)
        {
            LevelData max = null;
            foreach (LevelData level in level_data)
            {
                if (level.id == id)
                {
                    if (max == null || level.level > max.level)
                        max = level;
                }
            }
            return max;
        }

        public static LevelData GetLevelByXP(string id, int xp)
        {
            foreach (LevelData current in level_data)
            {
                if (current.id == id)
                {
                    LevelData next = GetLevel(id, current.level + 1);
                    if (next != null && xp >= current.xp_required && xp < next.xp_required)
                    {
                        return current;
                    }
                }
            }
            return GetMaxLevel(id);
        }
    }

    [System.Serializable]
    public class LevelUnlockBonus
    {
        public BonusType bonus;
        public float bonus_value;
        public GroupData target_group;
    }

}