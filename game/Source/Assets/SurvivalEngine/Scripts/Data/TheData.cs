using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Manager script that will load all scriptable objects for use at runtime
    /// </summary>

    public class TheData : MonoBehaviour
    {
        public GameData data;
        public AssetData assets;

        [Header("Resources Sub Folder")]
        public string load_folder = "";

        private static TheData _instance;

        void Awake()
        {
            _instance = this;

            CraftData.Load(load_folder);
            ItemData.Load(load_folder);
            ConstructionData.Load(load_folder);
            PlantData.Load(load_folder);
            CharacterData.Load(load_folder);
            SpawnData.Load(load_folder);
            LevelData.Load(load_folder);
            
            //Load managers
            if (!FindObjectOfType<TheUI>())
                Instantiate(TheGame.IsMobile() ? assets.ui_canvas_mobile : assets.ui_canvas);
            if (!FindObjectOfType<TheAudio>())
                Instantiate(assets.audio_manager);
            if (!FindObjectOfType<ActionSelector>())
                Instantiate(assets.action_selector);
        }

        public static TheData Get()
        {
            if (_instance == null)
                _instance = FindObjectOfType<TheData>();
            return _instance;
        }
    }

}