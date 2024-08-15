using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Use to take a construction that has an item variant (lure/trap)
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Take", order = 50)]
    public class ActionTake : SAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            PlayerData pdata = PlayerData.Get();
            Construction construction = select.GetComponent<Construction>();
            if (construction != null && construction.data != null)
            {
                ItemData take_item = construction.data.take_item_data;
                InventoryData inv_data = character.Inventory.GetValidInventory(take_item, 1);
                if (take_item != null && inv_data != null)
                {
                    BuiltConstructionData bdata = pdata.GetConstructed(construction.GetUID());
                    float durability = bdata != null && bdata.durability > 0.01f ? bdata.durability : take_item.durability;
                    
                    inv_data.AddItem(take_item.id, 1, durability, select.GetUID());
                    select.Destroy();
                }
            }

            Character acharacter = select.GetComponent<Character>();
            if (acharacter != null)
            {
                ItemData take_item = acharacter.data.take_item_data;
                InventoryData inv_data = character.Inventory.GetValidInventory(take_item, 1);
                if (take_item != null && inv_data != null)
                {
                    TrainedCharacterData cdata = pdata.GetCharacter(acharacter.GetUID());
                    inv_data.AddItem(take_item.id, 1, take_item.durability, select.GetUID());
                    select.Destroy();
                }
            }
        }
    }

}
