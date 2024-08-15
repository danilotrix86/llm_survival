using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if IN_CONTROL
using InControl;
#endif

namespace SurvivalEngine
{
    /// <summary>
    /// Wrapper class for integrating InControl
    /// To make controls work make sure to turn on gamepad_controls on the PlayerControls script
    /// </summary>

    public class InControlWrap : MonoBehaviour
    {
#if IN_CONTROL

        public int player_id = 0;

        public InputControlType action = InputControlType.Action1;
        public InputControlType attack = InputControlType.Action3;
        public InputControlType attack2 = InputControlType.RightBumper;
        public InputControlType jump = InputControlType.Action4;
        public InputControlType use = InputControlType.Action3;
        public InputControlType craft = InputControlType.LeftBumper;

       
        public InputControlType menu_accept = InputControlType.Action1;
        public InputControlType menu_cancel = InputControlType.Action2;
        public InputControlType menu_pause = InputControlType.Command;
        public InputControlType camera_left = InputControlType.LeftTrigger;
        public InputControlType camera_right = InputControlType.RightTrigger;

        [Header("Load InControl Manager Prefab")]
        public GameObject in_control_manager;

        private InputDevice active_device;

        private void Awake()
        {
            //Add InControl Manager to scene
            if (!FindObjectOfType<InControlManager>())
            {
                if (in_control_manager != null)
                {
                    Instantiate(in_control_manager);
                }
                else
                {
                    GameObject incontrol = new GameObject("InControl");
                    incontrol.AddComponent<InControlManager>();
                }
            }
        }

        void Start()
        {
            active_device = InputManager.ActiveDevice;

            PlayerControls controls = PlayerControls.Get(player_id);
            controls.gamepad_linked = true;

            controls.gamepad_action = () => { return WasPressed(active_device, action); };
            controls.gamepad_attack = () => { return WasPressed(active_device, attack) || WasPressed(active_device, attack2); };
            controls.gamepad_jump = () => { return WasPressed(active_device, jump); };
            controls.gamepad_use = () => { return WasPressed(active_device, use); };
            controls.gamepad_craft = () => { return WasPressed(active_device, craft); };
            controls.gamepad_accept = () => { return WasPressed(active_device, menu_accept); };
            controls.gamepad_cancel = () => { return WasPressed(active_device, menu_cancel); };
            controls.gamepad_pause = () => { return WasPressed(active_device, menu_pause); };

            controls.gamepad_camera = () => { return new Vector2(-GetAxis(active_device, camera_left) + GetAxis(active_device, camera_right), 0f); };

            controls.gamepad_move = () => { return GetTwoAxis(active_device, InputControlType.LeftStickX, InputControlType.LeftStickY); };
            controls.gamepad_freelook = () => { return GetTwoAxis(active_device, InputControlType.RightStickX, InputControlType.RightStickY); };
            controls.gamepad_menu = () => { return GetTwoAxisThreshold(active_device, InputControlType.LeftStickX, InputControlType.LeftStickY) + GetTwoAxisPress(active_device, InputControlType.DPadX, InputControlType.DPadY); };
            controls.gamepad_dpad = () => { return GetTwoAxisPress(active_device, InputControlType.DPadX, InputControlType.DPadY); };
        }

        void Update()
        {
            active_device = InputManager.ActiveDevice;
        }

        private bool WasPressed(InputDevice device, InputControlType type)
        {
            if(device != null)
                return device.GetControl(type).WasPressed;
            return false;
        }

        private float GetAxis(InputDevice device, InputControlType type)
        {
            if (device != null)
                return device.GetControl(type).Value;
            return 0f;
        }

        private Vector2 GetTwoAxis(InputDevice device, InputControlType typeX, InputControlType typeY)
        {
            return new Vector2(GetAxis(device, typeX), GetAxis(device, typeY));
        }

        private float GetAxisPress(InputDevice device, InputControlType type)
        {
            if (device != null)
            {
                InputControl control = device.GetControl(type);
                return control.WasPressed ? control.Value : 0f;
            }
            return 0f;
        }

        private Vector2 GetTwoAxisPress(InputDevice device, InputControlType typeX, InputControlType typeY)
        {
            return new Vector2(GetAxisPress(device, typeX), GetAxisPress(device, typeY));
        }


        private float GetAxisThreshold(InputDevice device, InputControlType type)
        {
            if (device != null)
            {
                InputControl control = device.GetControl(type);
                return Mathf.Abs(control.LastValue) < 0.5f && Mathf.Abs(control.Value) >= 0.5f ? Mathf.Sign(control.Value) : 0f;
            }
            return 0f;
        }

        private Vector2 GetTwoAxisThreshold(InputDevice device, InputControlType typeX, InputControlType typeY)
        {
            return new Vector2(GetAxisThreshold(device, typeX), GetAxisThreshold(device, typeY));
        }
#endif
    }
}