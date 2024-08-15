using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Dig using the shovel, put this on a Selectable to auto dig on left click
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/DigAuto", order = 50)]
    public class ActionDigAuto : AAction
    {
        public GroupData required_item;

        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            DigSpot spot = select.GetComponent<DigSpot>();
            if (spot != null)
            {
                string animation = character.Animation ? character.Animation.dig_anim : "";
                character.TriggerAnim(animation, spot.transform.position);
                character.TriggerProgressBusy(1.5f, () =>
                {
                    spot.Dig();

                    InventoryItemData ivdata = character.EquipData.GetFirstItemInGroup(required_item);
                    if (ivdata != null)
                        ivdata.durability -= 1;
                });
            }
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return character.EquipData.HasItemInGroup(required_item);
        }
    }

}