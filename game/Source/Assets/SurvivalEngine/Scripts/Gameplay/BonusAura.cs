using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// When the character is near this, will gain bonus
    /// </summary>

    public class BonusAura : MonoBehaviour
    {
        public BonusEffectData effect;
        public float range = 5f;


        private static List<BonusAura> aura_list = new List<BonusAura>();

        void Awake()
        {
            aura_list.Add(this);
        }

        private void OnDestroy()
        {
            aura_list.Remove(this);
        }

        public static List<BonusAura> GetAll()
        {
            return aura_list;
        }
    }

}