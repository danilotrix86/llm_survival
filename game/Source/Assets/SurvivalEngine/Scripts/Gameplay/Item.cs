using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{

    /// <summary>
    /// Items are objects that can be picked, dropped and held into the player's inventory. Some item can also be crafted or used as crafting material.
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class Item : Craftable
    {
        [Header("Item")]
        public ItemData data;
        public int quantity = 1;

        [Header("FX")]
        public float auto_collect_range = 0f; //Will automatically be collected when in range
        public bool snap_to_ground = true; //If true, item will be automatically placed on the ground instead of floating if spawns in the air
        public AudioClip take_audio;
        public GameObject take_fx;

        [HideInInspector]
        public bool was_spawned = false; //If true, item was dropped by the player, or loaded from save file

        public UnityAction onTake;
        public UnityAction onDestroy;

        private Selectable selectable;
        private UniqueID unique_id;

        private static List<Item> item_list = new List<Item>();

        protected override void Awake()
        {
            base.Awake();
            item_list.Add(this);
            selectable = GetComponent<Selectable>();
            unique_id = GetComponent<UniqueID>();
            selectable.onUse += OnUse;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            item_list.Remove(this);
        }

        private void Start()
        {
            if (!was_spawned && PlayerData.Get().IsObjectRemoved(GetUID()))
            {
                Destroy(gameObject);
                return;
            }

            if (snap_to_ground)
            {
                float dist;
                bool grounded = DetectGrounded(out dist);
                if (!grounded)
                {
                    transform.position += Vector3.down * dist;
                }
            }
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (was_spawned && selectable.IsActive())
            {
                PlayerData pdata = PlayerData.Get();
                DroppedItemData dropped_item = pdata.GetDroppedItem(GetUID());
                if (dropped_item != null)
                {
                    if (data.HasDurability() && dropped_item.durability <= 0f)
                        DestroyItem(); //Destroy item from durability
                }
            }

            if (auto_collect_range > 0.1f)
            {
                PlayerCharacter player = PlayerCharacter.GetNearest(transform.position, auto_collect_range);
                if (player != null)
                    player.Inventory.AutoTakeItem(this);
            }
        }

        private void OnUse(PlayerCharacter character)
        {
            //Take
            character.Inventory.TakeItem(this);
        }

        public void TakeItem()
        {
            if (onTake != null)
                onTake.Invoke();

            DestroyItem();
			LLMInterface.Instance().StopCommand("pick");

            TheAudio.Get().PlaySFX("item", take_audio);
            if (take_fx != null)
                Instantiate(take_fx, transform.position, Quaternion.identity);
        }

        //Destroy content but keep container
        public void SpoilItem()
        {
            if (data.container_data)
                Item.Create(data.container_data, transform.position, quantity);
            DestroyItem();
        }

        //Remove quantity
        public void EatItem()
        {
            quantity--;
            DroppedItemData invdata = PlayerData.Get().GetDroppedItem(GetUID());
            if (invdata != null)
                invdata.quantity = quantity;

            if (quantity <= 0)
                DestroyItem();
        }

        public void DestroyItem()
        {
            PlayerData pdata = PlayerData.Get();
            if (was_spawned)
                pdata.RemoveDroppedItem(GetUID()); //Removed from dropped items
            else
                pdata.RemoveObject(GetUID()); //Taken from map

            item_list.Remove(this);

            if (onDestroy != null)
                onDestroy.Invoke();

            Destroy(gameObject);
        }

        private bool DetectGrounded(out float dist)
        {
            float radius = 20f;
            float radius_up = 5f;
            float offset = 0.5f;
            Vector3 center = transform.position + Vector3.up * offset;
            Vector3 centerup = transform.position + Vector3.up * radius_up;

            RaycastHit hd1, hu1, hf1;
            LayerMask everything = ~0;
            bool f1 = Physics.Raycast(center, Vector3.down, out hf1, offset + 0.1f, everything.value, QueryTriggerInteraction.Ignore);
            bool d1 = Physics.Raycast(center, Vector3.down, out hd1, radius + offset, everything.value, QueryTriggerInteraction.Ignore);
            bool u1 = Physics.Raycast(centerup, Vector3.down, out hu1, radius_up + 0.1f, everything.value, QueryTriggerInteraction.Ignore);
            dist = d1 ? hd1.distance - offset : (u1 ? hu1.distance - radius_up : 0f);
            return f1;
        }

        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id.unique_id);
        }

        public string GetUID()
        {
            return unique_id.unique_id;
        }

        public bool HasGroup(GroupData group)
        {
            return data.HasGroup(group) || selectable.HasGroup(group);
        }

        public Selectable GetSelectable()
        {
            return selectable;
        }

        public DroppedItemData SaveData
        {
            get { return PlayerData.Get().GetDroppedItem(GetUID()); }  //Can be null if not dropped or spawned
        }

        public static new Item GetNearest(Vector3 pos, float range = 999f)
        {
            Item nearest = null;
            float min_dist = range;
            foreach (Item item in item_list)
            {
                float dist = (item.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = item;
                }
            }
            return nearest;
        }

        public static new Item GetNearestKnownData(ItemData data, Vector3 pos){
            Item nearest = null;
            float min_dist = 99999999999;
            foreach (Item item in item_list)
            {
				if(item.GetData() != data) continue;
                if(!item.unique_id.parentCell.discovered) continue;

                float dist = (item.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = item;
                }
            }
            return nearest;
        }

        public static new Item GetNearestData(ItemData data, Vector3 pos, float range = 999f)
        {
            Item nearest = null;
            float min_dist = range;
            foreach (Item item in item_list)
            {
				if(item.GetData() != data) continue;

                float dist = (item.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = item;
                }
            }
            return nearest;
        }

        public static int CountInRange(Vector3 pos, float range)
        {
            int count = 0;
            foreach (Item item in GetAll())
            {
                float dist = (item.transform.position - pos).magnitude;
                if (dist < range)
                    count++;
            }
            return count;
        }

        public static int CountInRange(ItemData data, Vector3 pos, float range)
        {
            int count = 0;
            foreach (Item item in GetAll())
            {
                if (item.data == data)
                {
                    float dist = (item.transform.position - pos).magnitude;
                    if (dist < range)
                        count++;
                }
            }
            return count;
        }

        public static Item GetByUID(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (Item item in item_list)
                {
                    if (item.GetUID() == uid)
                        return item;
                }
            }
            return null;
        }

        public static List<Item> GetAllOf(ItemData data)
        {
            List<Item> valid_list = new List<Item>();
            foreach (Item item in item_list)
            {
                if (item.data == data)
                    valid_list.Add(item);
            }
            return valid_list;
        }

        public static new List<Item> GetAll()
        {
            return item_list;
        }

        //Spawn an existing one in the save file (such as after loading)
        public static Item Spawn(string uid, Transform parent = null)
        {
            DroppedItemData ddata = PlayerData.Get().GetDroppedItem(uid);
            if (ddata != null && ddata.scene == SceneNav.GetCurrentScene())
            {
                ItemData idata = ItemData.Get(ddata.item_id);
                if (idata != null)
                {
                    GameObject build = Instantiate(idata.item_prefab, ddata.pos, idata.item_prefab.transform.rotation);
                    build.transform.parent = parent;

                    Item item = build.GetComponent<Item>();
                    item.data = idata;
                    item.was_spawned = true;
                    item.unique_id.unique_id = uid;
                    item.quantity = ddata.quantity;
                    return item;
                }
            }
            return null;
        }

        //Create a totally new one that will be added to save file
        public static Item Create(ItemData data, Vector3 pos, int quantity, bool instantCollect=false)
        {
            DroppedItemData ditem = PlayerData.Get().AddDroppedItem(data.id, SceneNav.GetCurrentScene(), pos, quantity, data.durability);
            GameObject obj = Instantiate(data.item_prefab, pos, data.item_prefab.transform.rotation);
            Item item = obj.GetComponent<Item>();
            item.data = data;
            item.was_spawned = true;
            item.unique_id.unique_id = ditem.uid;
            item.quantity = quantity;
			if(instantCollect) {
				GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerCharacter>().Inventory.AutoTakeItem(item, true);
			}
            return item;
        }

        //Create a new item that existed in inventory (such as when dropping it)
        public static Item Create(ItemData data, Vector3 pos, int quantity, float durability, string uid)
        {
            DroppedItemData ditem = PlayerData.Get().AddDroppedItem(data.id, SceneNav.GetCurrentScene(), pos, quantity, durability, uid);
            GameObject obj = Instantiate(data.item_prefab, pos, data.item_prefab.transform.rotation);
            Item item = obj.GetComponent<Item>();
            item.data = data;
            item.was_spawned = true;
            item.unique_id.unique_id = ditem.uid;
            item.quantity = quantity;
            return item;
        }
    }

}