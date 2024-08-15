using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Drop an item 
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Drop", order = 50)]
    public class ActionDrop : SAction
    {

        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            InventoryData inventory = slot.GetInventory();
            character.Inventory.DropItem(inventory, slot.index);
        }
    }

}