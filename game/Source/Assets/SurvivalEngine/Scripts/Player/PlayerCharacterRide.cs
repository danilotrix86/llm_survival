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
    public class PlayerCharacterRide : MonoBehaviour
    {
        private PlayerCharacter character;
        private bool is_riding = false;
        private AnimalRide riding_animal = null;

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (character.IsDead())
                return;

            if (is_riding)
            {
                if (riding_animal == null || riding_animal.IsDead())
                {
                    StopRide();
                    return;
                }

                transform.position = riding_animal.GetRideRoot();
                transform.rotation = Quaternion.LookRotation(riding_animal.transform.forward, Vector3.up);

                //Stop riding
                PlayerControls controls = PlayerControls.Get(character.player_id);
                if (character.IsControlsEnabled())
                {
                    if (controls.IsPressJump() || controls.IsPressAction() || controls.IsPressUICancel())
                        StopRide();
                }
            }
        }

        public void RideNearest()
        {
            AnimalRide animal = AnimalRide.GetNearest(transform.position, 2f);
            RideAnimal(animal);
        }

        public void RideAnimal(AnimalRide animal)
        {
            if (!is_riding && character.IsMovementEnabled() && animal != null)
            {
                is_riding = true;
                character.SetBusy(true);
                character.DisableMovement();
                character.DisableCollider();
                riding_animal = animal;
                transform.position = animal.GetRideRoot();
                animal.SetRider(character);
            }
        }

        public void StopRide()
        {
            if (is_riding)
            {
                if (riding_animal != null)
                    riding_animal.StopRide();
                is_riding = false;
                character.SetBusy(false);
                character.EnableMovement();
                character.EnableCollider();
                character.FaceDir(transform.forward);
                character.StopMove();
                riding_animal = null;
            }
        }

        public bool IsRiding()
        {
            return is_riding;
        }

        public AnimalRide GetAnimal()
        {
            return riding_animal;
        }

        public PlayerCharacter GetCharacter()
        {
            return character;
        }
    }

}
