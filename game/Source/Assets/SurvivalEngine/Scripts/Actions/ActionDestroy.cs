using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Just destroy the destructible
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Destroy", order = 50)]
    public class ActionDestroy : AAction
    {
        public string animation;

        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            select.Destructible.KillIn(0.5f);
            character.TriggerAnim(animation, select.transform.position);
            character.TriggerBusy(0.5f);
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return select.Destructible != null && !select.Destructible.IsDead();
        }
    }

}