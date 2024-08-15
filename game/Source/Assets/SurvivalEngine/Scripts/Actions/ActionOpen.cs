using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Open a package with more items inside (like a gift, box)
    /// </summary>
    

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Open", order = 50)]
    public class ActionOpen : SAction
    {
        public SData[] items;

        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            InventoryData inventory = slot.GetInventory();
            inventory.RemoveItemAt(slot.index, 1);
            foreach (SData item in items)
            {
                if (item != null)
                {
                    if (item is ItemData)
                    {
                        ItemData iitem = (ItemData)item;
                        character.Inventory.GainItem(iitem, 1);
                    }

                    if (item is LootData)
                    {
                        LootData loot = (LootData)item;
                        if (Random.value <= loot.probability)
                        {
                            character.Inventory.GainItem(loot.item, loot.quantity);
                        }
                    }
                }
            }

        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return true;
        }
    }

}