using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// A group doesn't do anything by itself, but it serves to reference objects as a group instead of referencing them one by one.
    /// Ex: all tools that can cut trees could be in the group 'CutTree', and trees would have a requirement that need the player to hold a 'CutTree' item.
    /// This avoid having to reference the new item to each tree everytime you create a new type of axe. You could just add the new axe to the group that is already attached to every exisitng tree.
    /// </summary>

    [CreateAssetMenu(fileName = "GroupData", menuName = "SurvivalEngine/GroupData", order = 1)]
    public class GroupData : ScriptableObject
    {
        public string group_id;
        public string title;
        public Sprite icon;
    }

}