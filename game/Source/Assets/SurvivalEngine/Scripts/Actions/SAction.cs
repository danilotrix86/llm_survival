using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Selectable Action parent class: Any action selected manually through the Action Selector (Items or Selectables)
    /// </summary>

    public abstract class SAction : ScriptableObject
    {
        public string title;

        //When using an action on a Selectable in the scene
        public virtual void DoAction(PlayerCharacter character, Selectable select)
        {

        }

        //When using an action on a ItemData in inventory (equip/eat/etc)
        public virtual void DoAction(PlayerCharacter character, ItemSlot slot)
        {

        }

        //Condition to check if the action is possible, override to add a condition
        public virtual bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return true; //No condition
        }

        //Condition to check if the action is possible, override to add a condition
        public virtual bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return true; //No condition
        }

        public bool IsAuto() { return (this is AAction); }
        public bool IsMerge() { return (this is MAction); }
    }

}