using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Sleeeep!
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Sleep", order = 50)]
    public class ActionSleep : AAction
    {
        public float sleep_hp_hour; //In game hours
        public float sleep_hunger_hour;
        public float sleep_thirst_hour;
        public float sleep_hapiness_hour;
        public float sleep_speed_mult = 8f;

        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            Construction construct = select.GetComponent<Construction>();
            if (construct != null)
                character.Sleep(this);

        }
    }

}
