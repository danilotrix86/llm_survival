using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if MAP_MINIMAP
using MapMinimap;
#endif

namespace SurvivalEngine
{
    /// <summary>
    /// Wrapper class for Map Minimap
    /// </summary>

    public class MapMinimapWrap : MonoBehaviour
    {

#if MAP_MINIMAP

        static MapMinimapWrap()
        {
            TheGame.afterLoad += ReloadMM;
            TheGame.afterNewGame += NewMM;
        }

        void Awake()
        {
            PlayerData.LoadLast(); //Make sure the game is loaded

            TheGame the_game = FindObjectOfType<TheGame>();
            MapManager map_manager = FindObjectOfType<MapManager>();

            if (map_manager != null)
            {
                map_manager.onOpenMap += OnOpen;
                map_manager.onCloseMap += OnClose;
            }
            else
            {
                Debug.LogError("Map Minimap: Integration failed - Make sure to add the MapManager to the scene");
            }

            if (the_game != null)
            {
                the_game.beforeSave += SaveDQ;
                LoadMM();
            }
        }

        private void Start()
        {
            
        }

        private void Update()
        {
            
        }

        private static void ReloadMM()
        {
            MapData.Unload();
            LoadMM();
        }

        private static void NewMM()
        {
            PlayerData pdata = PlayerData.Get();
            if (pdata != null)
            {
                MapData.Unload();
                MapData.NewGame(pdata.filename);
            }
        }

        private static void LoadMM()
        {
            PlayerData pdata = PlayerData.Get();
            if (pdata != null)
            {
                MapData.AutoLoad(pdata.filename);
            }
        }

        private void SaveDQ(string filename)
        {
            if (MapData.Get() != null && !string.IsNullOrEmpty(filename))
            {
                MapData.Save(filename, MapData.Get());
            }
        }

        private void OnOpen()
        {
            TheGame.Get().PauseScripts();
        }

        private void OnClose()
        {
            TheGame.Get().UnpauseScripts();
        }

#endif

    }
}

