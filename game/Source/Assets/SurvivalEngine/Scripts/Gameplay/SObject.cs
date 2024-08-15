using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Base class for either Craftables or Spawnables, has generic static useful functions
    /// </summary>
    public abstract class SObject : MonoBehaviour
    {
        public IdData GetData()
        {
            if (this is Spawnable)
                return ((Spawnable)this).data;
            if (this is Craftable)
                return ((Craftable)this).GetData();
            return null;
        }

        public static int CountSceneObjects(IdData data)
        {
            return CountSceneObjects(data, Vector3.zero, float.MaxValue); //All objects in scene
        }

        //Count all objects
        public static int CountSceneObjects(IdData data, Vector3 pos, float range)
        {
            int count = 0;
            if (data is CraftData)
            {
                count += Craftable.CountSceneObjects((CraftData)data, pos, range);
            }
            if (data is SpawnData)
            {
                count += Spawnable.CountInRange((SpawnData)data, pos, range);
            }
            return count;
        }

        //Create a new spawn object in save file and spawn it, also determing its type automatically (Item, Plants, or just Spawn...)
        public static GameObject Create(SData data, Vector3 pos)
        {
            if (data == null)
                return null;

            if (data is CraftData)
            {
                CraftData cdata = (CraftData)data;
                return Craftable.Create(cdata, pos);
            }
            if (data is SpawnData)
            {
                SpawnData spawn_data = (SpawnData)data;
                return Spawnable.Create(spawn_data, pos);
            }
            if (data is LootData)
            {
                LootData loot = (LootData)data;
                if (Random.value <= loot.probability)
                {
                    Item item = Item.Create(loot.item, pos, loot.quantity);
                    return item.gameObject;
                }
            }
            return null;
        }
    }
}

