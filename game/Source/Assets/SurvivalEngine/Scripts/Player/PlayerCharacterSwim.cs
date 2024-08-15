using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Script to allow player swimming
    /// Make sure the player character has a unique layer set to it (like Player layer)
    /// </summary>
    
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterSwim : MonoBehaviour
    {
        public float swim_speed = 1f;
        public LayerMask water_layer = (1 << 4); //The floor layer that will trigger the swimming state
        public LayerMask water_obstacle_layer = (1 << 14); //Invisible wall that is ignored by this character to be able to swim
        public Transform swim_mesh_offset;
        public float swim_offset_y = -1f;
        public bool swim_offset_camera = false;
        public GameObject swim_start_fx;
        public GameObject swim_ongoing_fx;
        public AudioClip swim_start_audio;

        private PlayerCharacter character;
        private bool is_swimming = false;
        private Vector3 swim_mesh_tpos;
        private int[] cground_layers = new int[0];
        private GameObject swimming_fx;

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            if (swim_mesh_offset != null)
                swim_mesh_tpos = swim_mesh_offset.transform.localPosition;

            if (swim_ongoing_fx != null)
            {
                swimming_fx = Instantiate(swim_ongoing_fx, transform);
                swimming_fx.SetActive(false);
            }

            foreach (int layer in PhysicsTool.LayerMaskToLayers(water_obstacle_layer))
                Physics.IgnoreLayerCollision(gameObject.layer, layer);
        }

        private void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (character.IsDead())
                return;

            Vector3 center = character.GetColliderCenter();
            float hradius = character.GetColliderHeightRadius();
            cground_layers = PhysicsTool.DetectGroundLayers(center, hradius);

        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (character.IsDead())
                return;

            //Swim
            if (!is_swimming && PhysicsTool.IsAnyLayerInLayerMask(cground_layers, water_layer))
                StartSwim();
            else if (is_swimming && !PhysicsTool.IsAnyLayerInLayerMask(cground_layers, water_layer))
                StopSwimming();

            //Swim adjust offset
            if (swim_mesh_offset != null)
                swim_mesh_offset.transform.localPosition = Vector3.Lerp(swim_mesh_offset.transform.localPosition, swim_mesh_tpos, 20f * Time.deltaTime);
        }

        private void StartSwim()
        {
            if (!is_swimming)
            {
                is_swimming = true;
                swim_mesh_tpos += Vector3.up * swim_offset_y;
                if (swim_start_fx != null)
                    Instantiate(swim_start_fx, transform.position, swim_start_fx.transform.rotation);
                if (swimming_fx != null)
                    swimming_fx.SetActive(true);
                character.TriggerBusy(0.25f);
                if (swim_offset_camera)
                    TheCamera.Get().SetOffset(Vector3.up * swim_offset_y);
                TheAudio.Get().PlaySFX("character", swim_start_audio);
            }
        }

        private void StopSwimming()
        {
            if (is_swimming)
            {
                is_swimming = false;
                swim_mesh_tpos -= Vector3.up * swim_offset_y;
                if (swimming_fx != null)
                    swimming_fx.SetActive(false);
                character.TriggerBusy(0.25f);
                if (swim_offset_camera)
                    TheCamera.Get().SetOffset(Vector3.zero);
            }
        }

        public bool IsSwimming()
        {
            return is_swimming;
        }
    }

}
