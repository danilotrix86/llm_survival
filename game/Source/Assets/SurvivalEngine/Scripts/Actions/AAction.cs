using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Automatic Action parent class: Any action performed automatically when the object is clicked on
    /// If you put more than 1, first AAction in the list that can be performed will be selected
    /// </summary>
    
    public abstract class AAction : SAction
    {
		public void InvokeAction(PlayerCharacter character, Selectable select){
			DoAction(character, select);
		}

        //When clicking on a Selectable in the scene
        public override void DoAction(PlayerCharacter character, Selectable select)
        {

        }

        //When right-clicking (or pressing Use) a ItemData in inventory
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {

        }

        //When left-clicking (or selecting) a ItemData in inventory
        public virtual void DoSelectAction(PlayerCharacter character, ItemSlot slot)
        {

        }

        //Condition to check if the selectable action is possible
        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return true; //No condition
        }

        //Condition to check if the item action is possible
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return true; //No condition
        }
    }

}