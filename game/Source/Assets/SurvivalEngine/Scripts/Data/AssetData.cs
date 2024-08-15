using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Generic asset data (only one file)
    /// </summary>

    [CreateAssetMenu(fileName = "AssetData", menuName = "SurvivalEngine/AssetData", order = 0)]
    public class AssetData : ScriptableObject
    {
        [Header("Systems Prefabs")]
        public GameObject ui_canvas;
        public GameObject ui_canvas_mobile;
        public GameObject audio_manager;
        
        [Header("UI")]
        public GameObject action_selector;
        public GameObject action_progress;

        [Header("FX")]
        public GameObject item_take_fx;
        public GameObject item_select_fx;
        public GameObject item_drag_fx;
        public GameObject item_merge_fx;

        [Header("Music")]
        public AudioClip[] music_playlist;

        public static AssetData Get()
        {
            return TheData.Get().assets;
        }
    }

}
