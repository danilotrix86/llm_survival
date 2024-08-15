using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Display the mobile joystick
    /// </summary>

    public class JoystickMobile : MonoBehaviour
    {
        public int joystick_id;

        public float sensitivity = 0.08f; //In percentage before reaching full speed
        public float threshold = 0.02f; //In percentage relative to Screen.height
        public float range = 0.1f; //Range it can be used when fixed_position is true
        public bool fixed_position = false;

        public RectTransform pin;

        private CanvasGroup canvas;
        private RectTransform rect;

        private bool joystick_active = false;
        private bool joystick_down = false;
        private Vector2 joystick_pos;
        private Vector2 joystick_dir;

        private static JoystickMobile instance;
        private static List<JoystickMobile> joysticks = new List<JoystickMobile>();

        void Awake()
        {
            instance = this;
            joysticks.Add(this);
            canvas = GetComponent<CanvasGroup>();
            rect = GetComponent<RectTransform>();
            canvas.alpha = 0f;

            if (!TheGame.IsMobile())
                enabled = false;
        }

        private void Start()
        {
            joystick_pos = TheUI.Get().WorldToScreenPos(rect.transform.position);
        }

        private void OnDestroy()
        {
            joysticks.Remove(this);
        }

        void Update()
        {
            PlayerControlsMouse controls = PlayerControlsMouse.Get();

            if (Input.GetMouseButtonDown(0) && controls.IsInGameplay())
            {
                if (!fixed_position)
                    joystick_pos = Input.mousePosition;
                joystick_dir = Vector2.zero;
                joystick_active = false;
                float nrange = range * Screen.height;
                float diff = Vector3.Distance(joystick_pos, Input.mousePosition);
                joystick_down = diff < nrange;
            }

            if (!Input.GetMouseButton(0))
            {
                joystick_active = false;
                joystick_down = false;
                joystick_dir = Vector2.zero;
            }

            if (Input.GetMouseButton(0) && joystick_down)
            {
                Vector2 mpos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                Vector2 distance = mpos - joystick_pos;
                distance = distance / (float)Screen.height; //Scaled dist (square)
                if (distance.magnitude > threshold)
                    joystick_active = true;

                joystick_dir = distance / sensitivity;
                joystick_dir = joystick_dir.normalized * Mathf.Min(joystick_dir.magnitude, 1f);
                if (distance.magnitude < threshold)
                    joystick_dir = Vector2.zero;
            }

            if (Input.touchCount >= 2)
                joystick_active = false;

            bool build_mode = PlayerUI.GetFirst() != null && PlayerUI.GetFirst().IsBuildMode();
            float target_alpha = IsVisible() && !build_mode ? 1f : 0f;
            canvas.alpha = Mathf.MoveTowards(canvas.alpha, target_alpha, 4f * Time.deltaTime);

            Vector2 screenPos = joystick_pos;
            rect.anchoredPosition = TheUI.Get().ScreenPointToCanvasPos(screenPos);
            pin.anchoredPosition = joystick_dir * 50f;

        }

        public bool IsActive()
        {
            return joystick_active;
        }

        public bool IsVisible()
        {
            return joystick_active || fixed_position;
        }

        public Vector2 GetPosition()
        {
            return joystick_pos;
        }

        public Vector2 GetDir()
        {
            return joystick_dir;
        }

        public static JoystickMobile Get()
        {
            return instance;
        }

        public static JoystickMobile Get(int id)
        {
            foreach (JoystickMobile stick in joysticks)
            {
                if (stick.joystick_id == id)
                    return stick;
            }
            return null;
        }
    }

}