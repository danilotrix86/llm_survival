using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    [System.Serializable]
    public enum BonusType
    {
        None = 0,

        SpeedBoost = 5, //Value in percentage
        AttackBoost = 7, //Value in percentage
        ArmorBoost = 8, //Value in percentage

        HealthUp = 10, //Value in amount per game-hour
        HungerUp = 11, //Value in amount per game-hour
        ThirstUp = 12, //Value in amount per game-hour
        HappyUp = 13, //Value in amount per game-hour
        EnergyUp = 14, //Value in amount per game-hour

        HealthMax = 20, //Value in amount per game-hour
        HungerMax = 21, //Value in amount per game-hour
        ThirstMax = 22, //Value in amount per game-hour
        HappyMax = 23, //Value in amount per game-hour
        EnergyMax = 24, //Value in amount per game-hour

        ColdResist=30, //Add cold resistance

        Invulnerable = 40, //In percentage, so 0.5 is half damage, 1 is no damage
    }

    /// <summary>
    /// Data file bonus effects (ongoing effect applied to the character when equipping an item or near a construction)
    /// </summary>

    [CreateAssetMenu(fileName = "BonusEffect", menuName = "SurvivalEngine/BonusEffect", order = 7)]
    public class BonusEffectData : ScriptableObject
    {
        public string effect_id;
        public BonusType type;  //Type of bonus
        public GroupData target; //Target needed for boost to apply (ex: tree, rock)
        public float value;


        public static BonusType GetAttributeBonusType(AttributeType type)
        {
            if (type == AttributeType.Health)
                return BonusType.HealthUp;
            if (type == AttributeType.Hunger)
                return BonusType.HungerUp;
            if (type == AttributeType.Thirst)
                return BonusType.ThirstUp;
            if (type == AttributeType.Happiness)
                return BonusType.HappyUp;
            if (type == AttributeType.Stress)
                return BonusType.EnergyUp;
            if (type == AttributeType.Heat)
                return BonusType.ColdResist;
            return BonusType.None;
        }

        public static BonusType GetAttributeMaxBonusType(AttributeType type)
        {
            if (type == AttributeType.Health)
                return BonusType.HealthMax;
            if (type == AttributeType.Hunger)
                return BonusType.HungerMax;
            if (type == AttributeType.Thirst)
                return BonusType.ThirstMax;
            if (type == AttributeType.Happiness)
                return BonusType.HappyMax;
            if (type == AttributeType.Stress)
                return BonusType.EnergyMax;
            return BonusType.None;
        }
    }

}