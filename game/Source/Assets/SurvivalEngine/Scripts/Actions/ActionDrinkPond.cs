using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Drink directly from a water source
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/DrinkPond", order = 50)]
    public class ActionDrinkPond : AAction
    {
        public float drink_hp;
        public float drink_hunger;
        public float drink_thirst;
        public float drink_happiness;

        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            string animation = character.Animation ? character.Animation.take_anim : "";
            character.TriggerAnim(animation, select.transform.position);
            character.TriggerBusy(0.5f, () =>
            {
                character.Attributes.AddAttribute(AttributeType.Health, drink_hp);
                character.Attributes.AddAttribute(AttributeType.Hunger, drink_hunger);
                character.Attributes.AddAttribute(AttributeType.Thirst, drink_thirst);
                character.Attributes.AddAttribute(AttributeType.Happiness, drink_happiness);
				LLMInterface.Instance().StopCommand();
            });
            
        }
    }

}
