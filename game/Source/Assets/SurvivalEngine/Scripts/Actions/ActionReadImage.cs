using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Read a note on an item
    /// </summary>
    

    [CreateAssetMenu(fileName = "Action", menuName = "SurvivalEngine/Actions/ReadImage", order = 50)]
    public class ActionReadImage : SAction
    {
        public Sprite image;

        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            ItemData item = slot.GetItem();
            if (item != null)
            {
                ReadPanel.Get(1).ShowPanel(item.title, image);
            }
        }

        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return true;
        }
    }

}