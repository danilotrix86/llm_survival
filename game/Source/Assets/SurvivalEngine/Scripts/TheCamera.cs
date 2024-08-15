using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    public enum FreelookMode
    {
        Hold = 0,
        Toggle = 10,
        Always = 20,
        Never = 30,
    }

    /// <summary>
    /// Main camera script
    /// </summary>

    public class TheCamera : MonoBehaviour
    {
        public bool move_enabled = true; //Uncheck if you want to use your own camera system

        [Header("Rotate/Zoom")]
        public float rotate_speed = 120f; //Set a negative value to inverse rotation side
        public float zoom_speed = 0.5f;
        public float zoom_in_max = 0.5f;
        public float zoom_out_max = 1f;

        [Header("Smoothing")]
        public bool smooth_camera = false; //Camera will be more smooth but less accurate
        public float smooth_speed = 10f;
        public float smooth_rotate_speed = 90f;

        [Header("Mobile Only")]
        public float rotate_speed_touch = 10f; //Mobile touch
        public float zoom_speed_touch = 1f; //Mobile touch

        [Header("Third Person Only")]
        public FreelookMode freelook_mode;
        public float freelook_speed_x = 150f;
        public float freelook_speed_y = 120f;
        public float freelook_max_up = 0.4f;
        public float freelook_max_down = 0.8f;

        [Header("Target")]
        public GameObject follow_target;
        public Vector3 follow_offset; //Offset for position
        public Vector3 pivot_offset; //Offset for rotation pivot

        protected Vector3 custom_offset;
        protected Vector3 current_vel;
        protected float current_zoom = -0.7f;
        protected float add_rotate = 0f;
        protected bool is_locked = false;

        protected Transform target_transform;
        protected Transform cam_target_transform;

        protected Camera cam;

        protected Vector3 shake_vector = Vector3.zero;
        protected float shake_timer = 0f;
        protected float shake_intensity = 1f;

        protected static TheCamera _instance;

        protected virtual void Awake()
        {
            _instance = this;
            cam = GetComponent<Camera>();

            GameObject cam_target = new GameObject("CameraTarget");
            target_transform = cam_target.transform;
            target_transform.position = transform.position - follow_offset;

            GameObject cam_target_cam = new GameObject("CameraTargetCam");
            cam_target_transform = cam_target_cam.transform;
            cam_target_transform.SetParent(target_transform);
            cam_target_transform.localPosition = follow_offset;
            cam_target_transform.localRotation = transform.localRotation;
        }

        protected virtual void Start()
        {
            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            mouse.onRightClick += (Vector3 vect) => { ToggleLock(); };
        }

        protected virtual void LateUpdate()
        {
            if (follow_target == null)
            {
                //Auto assign follow target
                PlayerCharacter first = PlayerCharacter.GetFirst();
                if (first != null)
                    follow_target = first.gameObject;
                return;
            }

            if (!move_enabled)
                return;

            UpdateControls();

            bool free_rotation = IsFreelook();
            if (free_rotation)
                UpdateFreeCamera();
            else
                UpdateCamera();

            //Untoggle if on top of UI
            if (is_locked && TheUI.Get() && TheUI.Get().IsBlockingPanelOpened())
                ToggleLock();

            //Shake FX
            if (shake_timer > 0f)
            {
                shake_timer -= Time.deltaTime;
                shake_vector = new Vector3(Mathf.Cos(shake_timer * Mathf.PI * 8f) * 0.02f, Mathf.Sin(shake_timer * Mathf.PI * 7f) * 0.02f, 0f);
                transform.position += shake_vector * shake_intensity;
            }
        }

        protected virtual void UpdateControls()
        {
            if (TheUI.Get().IsBlockingPanelOpened())
                return;

            PlayerControls controls = PlayerControls.Get();
            PlayerControlsMouse mouse = PlayerControlsMouse.Get();

            //Rotate
            add_rotate = 0f;
            add_rotate += controls.GetRotateCam() * rotate_speed;
            add_rotate += mouse.GetTouchRotate() * rotate_speed_touch;

            //Zoom 
            current_zoom += mouse.GetTouchZoom() * zoom_speed_touch; //Mobile 2 finger zoom
            current_zoom += mouse.GetMouseScroll() * zoom_speed; //Mouse scroll zoom
            current_zoom = Mathf.Clamp(current_zoom, -zoom_out_max, zoom_in_max);

            if (freelook_mode == FreelookMode.Hold)
                SetLockMode(mouse.IsMouseHoldRight());
            if (freelook_mode == FreelookMode.Always)
                SetLockMode(true);
            if (freelook_mode == FreelookMode.Never)
                SetLockMode(false);
            if (controls.IsGamePad())
                Cursor.visible = !is_locked && mouse.IsUsingMouse();
        }

        protected virtual void UpdateCamera()
        {
            //Rotate and Move
            float rot = target_transform.rotation.eulerAngles.y + add_rotate * Time.deltaTime;
            Quaternion targ_rot = Quaternion.Euler(target_transform.rotation.eulerAngles.x, rot, 0f);

            if (smooth_camera)
            {
                target_transform.position = Vector3.SmoothDamp(target_transform.position, follow_target.transform.position + pivot_offset, ref current_vel, 1f / smooth_speed);
                target_transform.rotation = Quaternion.Slerp(target_transform.rotation, targ_rot, smooth_rotate_speed * Time.deltaTime);
            }
            else
            {
                target_transform.position = follow_target.transform.position + pivot_offset;
                target_transform.rotation = targ_rot;
            }

            //Zoom
            Vector3 targ_zoom = (follow_offset + custom_offset) * (1f - current_zoom);
            cam_target_transform.localPosition = Vector3.Lerp(cam_target_transform.localPosition, targ_zoom, 10f * Time.deltaTime);

            //Move to target position
            transform.rotation = cam_target_transform.rotation;
            transform.position = cam_target_transform.position;
        }

        protected virtual void UpdateFreeCamera()
        {
            //Controls
            PlayerControls controls = PlayerControls.Get();
            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            Vector2 mouse_delta = Vector2.zero;
            if (is_locked)
                mouse_delta += mouse.GetMouseDelta();
            if (controls.IsGamePad())
                mouse_delta += controls.GetFreelook();

            //Rotate and move
            Quaternion rot_backup = target_transform.rotation;
            Quaternion targ_rot = target_transform.rotation;
            targ_rot = Quaternion.AngleAxis(freelook_speed_y * -mouse_delta.y * 0.5f * Time.deltaTime, target_transform.right) * targ_rot;
            targ_rot = Quaternion.Euler(0f, freelook_speed_x * mouse_delta.x * Time.deltaTime, 0) * targ_rot;
            targ_rot.eulerAngles = new Vector3(targ_rot.eulerAngles.x, targ_rot.eulerAngles.y, 0f);

            if (smooth_camera)
            {
                target_transform.position = Vector3.SmoothDamp(target_transform.position, follow_target.transform.position + pivot_offset, ref current_vel, 1f / smooth_speed);
                target_transform.rotation = Quaternion.Slerp(target_transform.rotation, targ_rot, smooth_rotate_speed * Time.deltaTime);
            }
            else
            {
                target_transform.position = follow_target.transform.position + pivot_offset;
                target_transform.rotation = targ_rot;
            }

            //Zoom
            Vector3 targ_zoom = (follow_offset + custom_offset) * (1f - current_zoom);
            cam_target_transform.localPosition = Vector3.Lerp(cam_target_transform.localPosition, targ_zoom, 10f * Time.deltaTime);

            //Lock to not rotate too much
            if (cam_target_transform.forward.y > freelook_max_up || cam_target_transform.forward.y < -freelook_max_down)
            {
                target_transform.rotation = rot_backup;
            }

            //Move to target position
            transform.rotation = cam_target_transform.rotation;
            transform.position = cam_target_transform.position;
        }

        public virtual void SetLockMode(bool locked)
        {
            if (is_locked != locked)
            {
                is_locked = locked;
                Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !locked;
            }
        }

        public virtual void ToggleLock()
        {
            if (freelook_mode == FreelookMode.Toggle)
            {
                SetLockMode(!is_locked);
            }
        }

        public virtual void MoveToTarget(Vector3 target)
        {
            Vector3 diff = target - target_transform.position;
            target_transform.position = target;
            transform.position += diff;
        }

        public virtual void Shake(float intensity = 2f, float duration = 0.5f)
        {
            shake_intensity = intensity;
            shake_timer = duration;
        }

        public virtual void SetOffset(Vector3 offset)
        {
            custom_offset = offset;
        }

        public virtual bool IsFreelook()
        {
            PlayerControls controls = PlayerControls.Get();
            return freelook_mode != FreelookMode.Never && (is_locked || controls.IsGamePad());
        }

        public bool IsFreeRotation() => IsFreelook(); //Previous version function name

        public virtual bool IsInside(Vector2 screen_pos)
        {
            return cam.pixelRect.Contains(screen_pos);
        }

        public virtual Quaternion GetFacingRotation()
        {
            Vector3 facing = GetFacingFront();
            return Quaternion.LookRotation(facing.normalized, Vector3.up);
        }

        public Quaternion GetRotation() => GetFacingRotation(); //Previous version function name

        public Vector3 GetTargetPos()
        {
            return target_transform.position;
        }

        public Quaternion GetTargetRotation()
        {
            return target_transform.rotation;
        }

        public Vector3 GetTargetPosOffsetFace(float dist)
        {
            return target_transform.position + GetFacingFront() * dist;
        }

        public Vector3 GetFacingFront()
        {
            Vector3 dir = transform.forward;
            dir.y = 0f;
            return dir.normalized;
        }

        public Vector3 GetFacingRight()
        {
            Vector3 dir = transform.right;
            dir.y = 0f;
            return dir.normalized;
        }

        public Vector3 GetFacingDir()
        {
            return transform.forward;
        }

        public bool IsLocked()
        {
            return is_locked;
        }

        public Camera GetCam()
        {
            return cam;
        }

        public static Camera GetCamera()
        {
            Camera camera = _instance != null ? _instance.GetCam() : Camera.main;
            return camera;
        }

        public static TheCamera Get()
        {
            return _instance;
        }
    }

}