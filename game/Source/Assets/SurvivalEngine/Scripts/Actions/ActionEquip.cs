using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Use to equip/unequip equipment items
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Equip", order = 50)]
    public class ActionEquip : SAction
    {

        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem();
            InventoryData inventory = slot.GetInventory();

            if (item != null && item.type == ItemType.Equipment)
            {
                if (inventory.type == InventoryType.Equipment && slot is EquipSlotUI)
                {
                    EquipSlotUI eslot = (EquipSlotUI) slot;
                    character.Inventory.UnequipItem(eslot.equip_slot);
                }
                else
                {
                    character.Inventory.EquipItem(inventory, slot.index);
                }
            }
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem();
            return item != null && item.type == ItemType.Equipment;
        }
    }

}