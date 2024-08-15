using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Fill a jug with water (or other)
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Fill", order = 50)]
    public class ActionFill : MAction
    {
        public ItemData filled_item;

        //Merge action
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            if (select.HasGroup(merge_target))
            {
                InventoryData inventory = slot.GetInventory();
                inventory.RemoveItemAt(slot.index, 1);
                character.Inventory.GainItem(inventory, filled_item, 1);
            }
        }

    }

}