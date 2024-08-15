using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Action to repair a building/items with a repair kit
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Repair", order = 50)]
    public class ActionRepair : MAction
    {
        public float duration = 0.5f;

        //Repair Items
        public override void DoAction(PlayerCharacter character, ItemSlot slot, ItemSlot slot_other)
        {
            string anim = character.Animation ? character.Animation.use_anim : "";
            character.TriggerAnim(anim, character.transform.position);
            character.TriggerProgressBusy(duration, () =>
            {
                InventoryItemData repair = slot.GetInventoryItem();
                InventoryItemData titem = slot_other.GetInventoryItem();
                if (repair != null && titem != null)
                {
                    ItemData iiteam = ItemData.Get(titem.item_id);
                    titem.durability = iiteam.durability;
                    repair.durability -= 1f;
                }
            });
        }

        //Repair Buildings
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            string anim = character.Animation ? character.Animation.use_anim : "";
            character.TriggerAnim(anim, select.transform.position);
            character.TriggerProgressBusy(duration, () =>
            {
                InventoryItemData repair = slot.GetInventoryItem();
                Destructible target = select.Destructible;
                if (repair != null && target != null)
                {
                    target.hp = target.GetMaxHP();
                    repair.durability -= 1f;
                }
            });
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot, ItemSlot slot_other)
        {
            ItemData item = slot_other.GetItem();
            if (item == null) return false;
            bool target_valid = merge_target == null || item.HasGroup(merge_target);
            bool durability_valid = item.durability_type == DurabilityType.UsageCount || item.durability_type == DurabilityType.UsageTime;
            return durability_valid && target_valid;
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            if (select == null) return false;
            bool target_valid = merge_target == null || select.HasGroup(merge_target);
            bool destruct_valid = select.Destructible != null && select.Destructible.target_team == AttackTeam.Ally;
            return target_valid && destruct_valid;
        }
    }

}
