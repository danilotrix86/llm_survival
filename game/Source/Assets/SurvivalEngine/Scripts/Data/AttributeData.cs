using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    [System.Serializable]
    public enum AttributeType
    {
        None = 0,
        Health = 2,
        Stress = 3,
        Happiness = 4,
        Hunger = 6,
        Thirst = 8,
        Heat=10,

        //Generic attributes, rename them to your own 
        Attribute5 = 50,
        Attribute6 = 60,
        Attribute7 = 70,
        Attribute8 = 80,
        Attribute9 = 90,
    }

    /// <summary>
    /// Attribute data (health, hunger, thirst, etc)
    /// </summary>

    [CreateAssetMenu(fileName = "AttributeData", menuName = "SurvivalEngine/AttributeData", order = 11)]
    public class AttributeData : ScriptableObject
    {
        public AttributeType type;
        public string title;

        [Space(5)]

        public float start_value = 100f; //Starting value
        public float max_value = 100f; //Maximum value

        public float value_per_hour = -100f; //How much is gained (or lost) per in-game hour
		public float value_per_hour_night = 0;

        [Header("When reaches zero")]
        public float deplete_hp_loss = -100f; //Per hour
        public float deplete_move_mult = 1f; //1f = normal speed
        public float deplete_attack_mult = 1f; //1f = normal speed

		public bool invert = false;


    }

}