using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Harvest the fruit of a plant
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Harvest", order = 50)]
    public class ActionHarvest : AAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            Plant plant = select.GetComponent<Plant>();
            if (plant != null)
            {
                string animation = character.Animation ? character.Animation.take_anim : "";
                character.TriggerAnim(animation, plant.transform.position);
                character.TriggerBusy(0.5f, () =>
                {
                    plant.Harvest(character);
					LLMInterface.Instance().StopCommand();
                });
            }
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            Plant plant = select.GetComponent<Plant>();
            if (plant != null)
            {
                return plant.HasFruit();
            }
            return false;
        }
    }

}