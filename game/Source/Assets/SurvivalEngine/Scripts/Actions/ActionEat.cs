using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Eat an item
    /// </summary>
    

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Eat", order = 50)]
    public class ActionEat : SAction
    {

        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            InventoryData inventory = slot.GetInventory();
            character.Inventory.EatItem(inventory, slot.index);
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem();
            return item != null && item.type == ItemType.Consumable;
        }
    }

}