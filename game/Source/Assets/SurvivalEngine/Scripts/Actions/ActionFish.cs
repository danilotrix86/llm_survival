using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Use your fishing rod to fish a fish!
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/Fish", order = 50)]
    public class ActionFish : AAction
    {
        public GroupData fishing_rod;
        public float fish_time = 3f; //In seconds

        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            if (select != null)
            {
                ItemProvider pond = select.GetComponent<ItemProvider>();
                if (pond != null && pond.HasItem())
                {
					character.Inventory.SetActiveGroup(fishing_rod);
                    character.FaceTorward(pond.transform.position);
                    character.TriggerProgressBusy(this, fish_time, () =>
                    {
                        pond.RemoveItem();
                        pond.GainItem(character, 1);
                        character.Attributes.GainXP("fishing", 10); //Example of XP gain
						character.Inventory.SetActiveGroup(null);
                        LLMInterface.Instance().StopCommand();
                    });
                }
            }
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            ItemProvider pond = select.GetComponent<ItemProvider>();
            bool canDo = pond != null /*&& !character.IsSwimming()*/;
            bool pondHasItem = pond.HasItem();
            bool characterHasTool = character.EquipData.HasItemInGroup(fishing_rod);
            
            if(!characterHasTool){
                LLMInterface.Instance().AddArgument("Fishing Rod");
                LLMInterface.Instance().ErrorCommand("ErrorTool");
            }

            if(!pondHasItem){
                LLMInterface.Instance().ErrorCommand("ErrorNoObject");
            }

            return canDo && pondHasItem && characterHasTool;
        }
    }

}