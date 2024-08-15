using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Merge Action parent class: Any action that happens when mixing two items (ex: coconut and axe), or one item with a selectable (ex: raw food on top of fire)
    /// </summary>

    public abstract class MAction : SAction
    {
        public GroupData merge_target;

        //When using an ItemData action on another ItemData (ex: cut coconut), slot is the one with the action, slot_other is the one without the action
        public virtual void DoAction(PlayerCharacter character, ItemSlot slot, ItemSlot slot_other)
        {

        }

        //When using an ItemData action on a Selectable (ex: cook meat)
        public virtual void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {

        }

        //Condition when merging item-item, override if you need to add a new condition
        public virtual bool CanDoAction(PlayerCharacter character, ItemSlot slot, ItemSlot slot_target) //slot_target is the one without the action
        {
            ItemData item = slot_target.GetItem();
            if (item == null) return false;
            return merge_target == null || item.HasGroup(merge_target);
        }

        //Condition when mergin item-select, override if you need to add a new condition
        public virtual bool CanDoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            if (select == null) return false;
            return merge_target == null || select.HasGroup(merge_target);
        }


        //---- Override basic action, to be able to use Merge actions are regular ones

        //Do the action using the nearest selectable
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            Selectable select = Selectable.GetNearestGroup(merge_target, character.transform.position);
            if (select != null)
            {
                DoAction(character, slot, select);
            }
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            Selectable select = Selectable.GetNearestGroup(merge_target, character.transform.position);
            if (select != null && select.IsInUseRange(character))
            {
                return CanDoAction(character, slot, select);
            }
            return false;
        }

    }

}