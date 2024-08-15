using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Generates items over time, that can be picked by the player. Examples include bird nest (create eggs), or a fishing spot (create fishes).
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class ItemProvider : MonoBehaviour
    {
        [Header("Item Spawn")]
        public float item_spawn_time = 2f; //In game hours
        public int item_max = 3;
        public ItemData[] items;

        [Header("Item Take")]
        public bool auto_take = true; //Character can take by clicking, otherwise will require a special action

        [Header("FX")]
        public GameObject[] item_models;
        public AudioClip take_sound;

        private UniqueID unique_id;

        private int nb_item = 1;
        private float item_progress = 0f;

        void Awake()
        {
            unique_id = GetComponent<UniqueID>();
        }

        private void Start()
        {
            if (PlayerData.Get().HasCustomInt(GetAmountUID()))
                nb_item = PlayerData.Get().GetCustomInt(GetAmountUID());

            if (auto_take)
                GetComponent<Selectable>().onUse += OnUse;
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();

            item_progress += game_speed * Time.deltaTime;
            if (item_progress > item_spawn_time)
            {
                item_progress = 0f;
                nb_item += 1;
                nb_item = Mathf.Min(nb_item, item_max);

                PlayerData.Get().SetCustomInt(GetAmountUID(), nb_item);
            }

            for (int i = 0; i < item_models.Length; i++)
            {
                bool visible = (i < nb_item);
                if (item_models[i].activeSelf != visible)
                    item_models[i].SetActive(visible);
            }
        }

        public void RemoveItem()
        {
            if (nb_item > 0)
                nb_item--;

            PlayerData.Get().SetCustomInt(GetAmountUID(), nb_item);
        }

        public void GainItem(PlayerCharacter player, int quantity = 1)
        {
            if (items.Length > 0)
            {
                ItemData item = items[Random.Range(0, items.Length)];
                player.Inventory.GainItem(item, quantity); //Gain auto item
            }
        }

        public void PlayTakeSound()
        {
            TheAudio.Get().PlaySFX("item", take_sound);
        }

        private void OnUse(PlayerCharacter player)
        {
            if (HasItem())
            {
                string animation = player != null && player.Animation ? player.Animation.take_anim : "";
                player.TriggerAnim(animation, transform.position);
                player.TriggerBusy(0.5f, () =>
                {
                    RemoveItem();
                    GainItem(player);
                    PlayTakeSound();
                });
            }
        }

        public bool HasItem()
        {
            return nb_item > 0;
        }

        public int GetNbItem()
        {
            return nb_item;
        }

        public string GetAmountUID()
        {
            if (!string.IsNullOrEmpty(unique_id.unique_id))
                return unique_id.unique_id + "_amount";
            return "";
        }
    }

}