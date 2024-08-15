using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Action to attack a destructible (if the destructible cant be attack automatically)
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Attack", order = 50)]
    public class ActionAttack : SAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            if (select.Destructible)
            {
                character.Attack(select.Destructible);
            }
        }
    }

}