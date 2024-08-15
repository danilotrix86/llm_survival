using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Cut an item with another item (ex: open coconut with axe)
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Cut", order = 50)]
    public class ActionCut : MAction
    {
        public ItemData cut_item;

        public override void DoAction(PlayerCharacter character, ItemSlot slot1, ItemSlot slot2)
        {
            InventoryData inventory = slot1.GetInventory();
            inventory.RemoveItemAt(slot1.index, 1);
            character.Inventory.GainItem(cut_item, 1);
        }
    }

}
