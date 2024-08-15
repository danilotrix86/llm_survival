using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurvivalEngine
{

    /// <summary>
    /// Top level UI script that manages the UI
    /// </summary>

    public class TheUI : MonoBehaviour
    {
        [Header("Panels")]
        public CanvasGroup gameplay_ui;
        public PausePanel pause_panel;
        public GameOverPanel game_over_panel;

        [Header("Material")]
        public Material ui_material;
        public Material text_material;
        
        public AudioClip ui_sound;

        public Color filter_red;
        public Color filter_yellow;

        private Canvas canvas;
        private RectTransform rect;

        private static TheUI _instance;

        void Awake()
        {
            _instance = this;
            canvas = GetComponent<Canvas>();
            rect = GetComponent<RectTransform>();

            if (ui_material != null)
            {
                foreach (Image image in GetComponentsInChildren<Image>())
                    image.material = ui_material;
            }
            if(text_material != null)
            {
                foreach (Text txt in GetComponentsInChildren<Text>())
                    txt.material = text_material;
            }
        }

        private void Start()
        {
            canvas.worldCamera = TheCamera.GetCamera();

            if (!TheGame.IsMobile() && ItemSelectedFX.Get() == null && AssetData.Get().item_select_fx != null)
            {
                Instantiate(AssetData.Get().item_select_fx, transform.position, Quaternion.identity);
            }

            if (ItemDragFX.Get() == null && AssetData.Get().item_drag_fx != null)
            {
                Instantiate(AssetData.Get().item_drag_fx, transform.position, Quaternion.identity);
            }

            PlayerUI gameplay_ui = GetComponentInChildren<PlayerUI>();
            if (gameplay_ui == null)
                Debug.LogError("Warning: Missing PlayerUI script on the Gameplay tab in the UI prefab");
        }

        void Update()
        {
            pause_panel.SetVisible(TheGame.Get().IsPausedByPlayer());

            foreach (PlayerControls controls in PlayerControls.GetAll())
            {
                if (controls.IsPressPause() && !TheGame.Get().IsPausedByPlayer())
                    TheGame.Get().Pause();
                else if (controls.IsPressPause() && TheGame.Get().IsPausedByPlayer())
                    TheGame.Get().Unpause();
            }

            //Gamepad auto focus
            UISlotPanel focus_panel = UISlotPanel.GetFocusedPanel();
            if (focus_panel != pause_panel && TheGame.Get().IsPausedByPlayer() && PlayerControls.IsAnyGamePad())
            {
                pause_panel.Focus();
            }
            if (focus_panel == pause_panel && !TheGame.Get().IsPausedByPlayer())
            {
               UISlotPanel.UnfocusAll();
            }
        }

        public void ShowGameOver()
        {
            foreach(PlayerUI ui in PlayerUI.GetAll())
                ui.CancelSelection();
            game_over_panel.Show();
        }

        public void OnClickPause()
        {
            if (TheGame.Get().IsPaused())
                TheGame.Get().Unpause();
            else
                TheGame.Get().Pause();

            TheAudio.Get().PlaySFX("UI", ui_sound);
        }

        public bool IsBlockingPanelOpened()
        {
            return StoragePanel.IsAnyVisible() || ReadPanel.IsAnyVisible() || pause_panel.IsVisible() || game_over_panel.IsVisible();
        }

        public bool IsFullPanelOpened()
        {
            return pause_panel.IsVisible() || game_over_panel.IsVisible();
        }
        
        //Menu are panels that block gamepad controls
        public bool IsMenuOpened()
        {
            return pause_panel.IsVisible() || game_over_panel.IsVisible();
        }

        //Convert a screen position (like mouse) to a anchored position in the canvas
        public Vector2 ScreenPointToCanvasPos(Vector2 pos)
        {
            Vector2 localpoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pos, canvas.worldCamera, out localpoint);
            return localpoint;
        }

        public Vector2 ScreenPointToCanvasPos(Vector2 pos, RectTransform localRect)
        {
            Vector2 localpoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(localRect, pos, canvas.worldCamera, out localpoint);
            return localpoint;
        }

        public Vector2 WorldToScreenPos(Vector3 world)
        {
            return TheCamera.GetCamera().WorldToScreenPoint(world);
        }

        public Vector2 WorldToCanvasPos(Vector3 world)
        {
            Vector2 screen_pos = WorldToScreenPos(world);
            return ScreenPointToCanvasPos(screen_pos);
        }

        public Vector2 GetCanvasSize()
        {
            return rect.sizeDelta;
        }

        public static GameObject RaycastUI(Vector2 mouse_pos)
        {
            List<RaycastResult> results = RaycastAllUI(mouse_pos);
            float mdist = 999f;
            GameObject ui = null;
            foreach (RaycastResult result in results)
            {
                if (result.distance < mdist)
                {
                    ui = result.gameObject;
                    mdist = result.distance;
                }
            }
            return ui;
        }

        public static List<RaycastResult> RaycastAllUI(Vector2 mouse_pos)
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = mouse_pos;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results;
        }

        public static bool IsMouseOverUI(Vector2 mouse_pos)
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = mouse_pos;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }

        public static TheUI Get()
        {
            return _instance;
        }
    }

}