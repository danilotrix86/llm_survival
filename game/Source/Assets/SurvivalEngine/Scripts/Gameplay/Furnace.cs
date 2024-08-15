using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    [RequireComponent(typeof(Selectable))]
    public class Furnace : MonoBehaviour
    {
        public GameObject spawn_point;
        public int quantity_max = 1;

        [Header("FX")]
        public GameObject active_fx;
        public AudioClip put_audio;
        public AudioClip finish_audio;
        public GameObject progress_prefab;

        private Selectable select;
        private UniqueID unique_id;
        private Animator animator;

        private ItemData prev_item = null;
        private ItemData current_item = null;
        private int current_quantity= 0;
        private float timer = 0f;
        private float duration = 0f; //In game hours
        private ActionProgress progress;

        private static List<Furnace> furnace_list = new List<Furnace>();

        void Awake()
        {
            furnace_list.Add(this);
            select = GetComponent<Selectable>();
            unique_id = GetComponent<UniqueID>();
            animator = GetComponentInChildren<Animator>();
        }

        private void OnDestroy()
        {
            furnace_list.Remove(this);
        }

        private void Start()
        {
            string item_id = PlayerData.Get().GetCustomString(GetItemUID());
            ItemData idata = ItemData.Get(item_id);
            if (HasUID() && idata != null)
            {
                timer = PlayerData.Get().GetCustomFloat(GetTimerUID());
                duration = PlayerData.Get().GetCustomFloat(GetDurationUID());
                current_quantity = PlayerData.Get().GetCustomInt(GetQuantityUID());
                current_item = idata;

                if (progress_prefab != null && duration > 0.1f)
                {
                    GameObject obj = Instantiate(progress_prefab, transform);
                    progress = obj.GetComponent<ActionProgress>();
                    progress.manual = true;
                }
            }
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (HasItem())
            {
                float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();
                timer += game_speed * Time.deltaTime;

                PlayerData.Get().SetCustomFloat(GetTimerUID(), timer);

                if (timer > duration)
                {
                    FinishItem();
                }

                if (progress != null)
                    progress.manual_value = timer / duration;

                if (active_fx != null && active_fx.activeSelf != HasItem())
                    active_fx.SetActive(HasItem());
                if (animator != null)
                    animator.SetBool("Active", HasItem());
            }
        }

        public int PutItem(ItemData item, ItemData create, float duration, int quantity)
        {
            if (current_item == null || create == current_item)
            {
                if (current_quantity < quantity_max && quantity > 0)
                {
                    int max = quantity_max - current_quantity; //Maximum space remaining
                    int quant = Mathf.Min(max, quantity + current_quantity); //Cant put more than maximum

                    prev_item = item;
                    current_item = create;
                    current_quantity += quant;
                    timer = 0f;
                    this.duration = duration;

                    PlayerData.Get().SetCustomFloat(GetTimerUID(), timer);
                    PlayerData.Get().SetCustomFloat(GetDurationUID(), duration);
                    PlayerData.Get().SetCustomInt(GetQuantityUID(), quant);
                    PlayerData.Get().SetCustomString(GetItemUID(), create.id);

                    if (progress_prefab != null && duration > 0.1f)
                    {
                        GameObject obj = Instantiate(progress_prefab, transform);
                        progress = obj.GetComponent<ActionProgress>();
                        progress.manual = true;
                    }

                    if (select.IsNearCamera(10f))
                        TheAudio.Get().PlaySFX("furnace", put_audio);

                    return quant;  //Return actual quantity that was used
                }
            }
            return 0;
        }

        public void FinishItem()
        {
            if (current_item != null) {

                Item.Create(current_item, spawn_point.transform.position, current_quantity);

                prev_item = null;
                current_item = null;
                current_quantity = 0;
                timer = 0f;

                PlayerData.Get().RemoveCustomFloat(GetTimerUID());
                PlayerData.Get().RemoveCustomFloat(GetDurationUID());
                PlayerData.Get().RemoveCustomInt(GetQuantityUID());
                PlayerData.Get().RemoveCustomString(GetItemUID());

                if (active_fx != null)
                    active_fx.SetActive(false);
                if (animator != null)
                    animator.SetBool("Active", false);

                if (progress != null)
                    Destroy(progress.gameObject);

                if (select.IsNearCamera(10f))
                    TheAudio.Get().PlaySFX("furnace", finish_audio);
            }
        }

        public bool HasItem()
        {
            return current_item != null;
        }

        public int CountItemSpace()
        {
            return quantity_max - current_quantity; //Number of items that can still be placed inside
        }

        public bool HasUID() 
        {
            return unique_id != null && unique_id.HasUID();
        }

        public string GetUID()
        {
            return unique_id != null ? unique_id.unique_id : "";
        }

        public string GetItemUID()
        {
            if (HasUID())
                return GetUID() + "_item";
            return "";
        }

        public string GetTimerUID()
        {
            if (HasUID())
                return GetUID() + "_timer";
            return "";
        }

        public string GetDurationUID()
        {
            if (HasUID())
                return GetUID() + "_duration";
            return "";
        }

        public string GetQuantityUID()
        {
            if (HasUID())
                return GetUID() + "_quantity";
            return "";
        }

        public static Furnace GetNearestInRange(Vector3 pos, float range=999f)
        {
            float min_dist = range;
            Furnace nearest = null;
            foreach (Furnace furnace in furnace_list)
            {
                float dist = (pos - furnace.transform.position).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = furnace;
                }
            }
            return nearest;
        }

        public static List<Furnace> GetAll()
        {
            return furnace_list;
        }
    }

}
