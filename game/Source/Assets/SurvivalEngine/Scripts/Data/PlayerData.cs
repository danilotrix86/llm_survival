using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SurvivalEngine
{
    /// <summary>
    /// PlayerData is the main save file data script. Everything contained in this script is what will be saved. 
    /// It also contains a lot of functions to easily access the saved data. Make sure to call TheGame.Get().Save() to write the data to a file on the disk.
    /// The latest save file will be loaded automatically when starting the game
    /// </summary>

    [System.Serializable]
    public class PlayerData
    {
        public string filename;
        public string version;
        public DateTime last_save;

        //-------------------

        public int world_seed = 0; //Randomly Generated world
        public string current_scene = ""; //Scene loaded
        public int current_entry_index = 0; //-1 means go to current_pos, 0 means default scene pos, >0 means at matching entry index
        
        public int day = 0;
        public float day_time = 0f; // 0 = midnight, 24 = end of day
        public float play_time = 0f; //total play time in actual seconds

        public float master_volume = 1f;
        public float music_volume = 1f;
        public float sfx_volume = 1f;

        public Dictionary<int, PlayerCharacterData> player_characters = new Dictionary<int, PlayerCharacterData>();
        public Dictionary<string, InventoryData> inventories = new Dictionary<string, InventoryData>();

        public Dictionary<string, int> unique_ids = new Dictionary<string, int>(); //Unique ints
        public Dictionary<string, float> unique_floats = new Dictionary<string, float>();
        public Dictionary<string, string> unique_strings = new Dictionary<string, string>();
        public Dictionary<string, int> removed_objects = new Dictionary<string, int>(); //1 = removed
        public Dictionary<string, int> hidden_objects = new Dictionary<string, int>(); //1 = hidden

        public Dictionary<string, DroppedItemData> dropped_items = new Dictionary<string, DroppedItemData>();
        public Dictionary<string, BuiltConstructionData> built_constructions = new Dictionary<string, BuiltConstructionData>();
        public Dictionary<string, SowedPlantData> sowed_plants = new Dictionary<string, SowedPlantData>();
        public Dictionary<string, TrainedCharacterData> trained_characters = new Dictionary<string, TrainedCharacterData>();
        public Dictionary<string, SpawnedData> spawned_objects = new Dictionary<string, SpawnedData>(); //Objects spawned
        public Dictionary<string, SceneObjectData> scene_objects = new Dictionary<string, SceneObjectData>(); //Objects already in scene but moved
        public Dictionary<string, RegrowthData> world_regrowth = new Dictionary<string, RegrowthData>();
        
        //-------------------

        private static string file_loaded = "";
        private static PlayerData player_data = null;

        public const string last_save_id = "last_save_survival";
        public const string extension = ".survival";

        public PlayerData(string name)
        {
            filename = name;
            version = Application.version;
            last_save = DateTime.Now;

            day = 1;
            day_time = 6f; // Start game at 6 in the morning

            master_volume = 1f;
            music_volume = 1f;
            sfx_volume = 1f;
        }

        public void FixData()
        {
            //Fix data to make sure old save files compatible with new game version
            if (unique_ids == null)
                unique_ids = new Dictionary<string, int>();
            if (unique_floats == null)
                unique_floats = new Dictionary<string, float>();
            if (unique_strings == null)
                unique_strings = new Dictionary<string, string>();

            if (player_characters == null)
                player_characters = new Dictionary<int, PlayerCharacterData>();
            if (inventories == null)
                inventories = new Dictionary<string, InventoryData>();

            if (dropped_items == null)
                dropped_items = new Dictionary<string, DroppedItemData>();
            if (removed_objects == null)
                removed_objects = new Dictionary<string, int>();
            if (hidden_objects == null)
                hidden_objects = new Dictionary<string, int>();
            if (built_constructions == null)
                built_constructions = new Dictionary<string, BuiltConstructionData>();
            if (sowed_plants == null)
                sowed_plants = new Dictionary<string, SowedPlantData>();
            if (trained_characters == null)
                trained_characters = new Dictionary<string, TrainedCharacterData>();

            if (spawned_objects == null)
                spawned_objects = new Dictionary<string, SpawnedData>();
            if (scene_objects == null)
                scene_objects = new Dictionary<string, SceneObjectData>();
            if (world_regrowth == null)
                world_regrowth = new Dictionary<string, RegrowthData>();

            foreach (KeyValuePair<int, PlayerCharacterData> character in player_characters)
                character.Value.FixData();

            foreach (KeyValuePair<string, InventoryData> inventory in inventories)
                inventory.Value.FixData();

        }

        //-------- Dropped items --------

        public DroppedItemData AddDroppedItem(string item_id, string scene, Vector3 pos, int quantity, float durability)
        {
            string uid = UniqueID.GenerateUniqueID();
            return AddDroppedItem(item_id, scene, pos, quantity, durability, uid);
        }

        public DroppedItemData AddDroppedItem(string item_id, string scene, Vector3 pos, int quantity, float durability, string uid)
        {
            RemoveDroppedItem(uid);

            DroppedItemData ditem = new DroppedItemData();
            ditem.uid = uid;
            ditem.item_id = item_id;
            ditem.scene = scene;
            ditem.pos = pos;
            ditem.quantity = quantity;
            ditem.durability = durability;
            dropped_items[ditem.uid] = ditem;
            return ditem;
        }

        public void RemoveDroppedItem(string uid)
        {
            if (dropped_items.ContainsKey(uid))
                dropped_items.Remove(uid);
        }

        public DroppedItemData GetDroppedItem(string uid)
        {
            if (dropped_items.ContainsKey(uid))
                return dropped_items[uid];
            return null;
        }

        //---- Constructions and Plants and Characters

        public BuiltConstructionData AddConstruction(string construct_id, string scene, Vector3 pos, Quaternion rot, float durability)
        {
            BuiltConstructionData citem = new BuiltConstructionData();
            citem.uid = UniqueID.GenerateUniqueID();
            citem.construction_id = construct_id;
            citem.scene = scene;
            citem.pos = pos;
            citem.rot = rot;
            citem.durability = durability;
            built_constructions[citem.uid] = citem;
            return citem;
        }

        public void RemoveConstruction(string uid)
        {
            if (built_constructions.ContainsKey(uid))
                built_constructions.Remove(uid);
        }

        public BuiltConstructionData GetConstructed(string uid)
        {
            if (built_constructions.ContainsKey(uid))
                return built_constructions[uid];
            return null;
        }

        public SowedPlantData AddPlant(string plant_id, string scene, Vector3 pos, Quaternion rot, int stage)
        {
            SowedPlantData citem = new SowedPlantData();
            citem.uid = UniqueID.GenerateUniqueID();
            citem.plant_id = plant_id;
            citem.scene = scene;
            citem.pos = pos;
            citem.rot = rot;
            citem.growth_stage = stage;
            sowed_plants[citem.uid] = citem;
            return citem;
        }

        public void GrowPlant(string plant_uid, int stage)
        {
            if (sowed_plants.ContainsKey(plant_uid))
                sowed_plants[plant_uid].growth_stage = stage;
        }

        public void RemovePlant(string uid)
        {
            if (sowed_plants.ContainsKey(uid))
                sowed_plants.Remove(uid);
        }

        public SowedPlantData GetSowedPlant(string uid)
        {
            if (sowed_plants.ContainsKey(uid))
                return sowed_plants[uid];
            return null;
        }

        public TrainedCharacterData AddCharacter(string character_id, string scene, Vector3 pos, Quaternion rot)
        {
            TrainedCharacterData citem = new TrainedCharacterData();
            citem.uid = UniqueID.GenerateUniqueID();
            citem.character_id = character_id;
            citem.scene = scene;
            citem.pos = pos;
            citem.rot = rot;
            trained_characters[citem.uid] = citem;
            return citem;
        }

        public void RemoveCharacter(string uid)
        {
            if (trained_characters.ContainsKey(uid))
                trained_characters.Remove(uid);
        }

        public TrainedCharacterData GetCharacter(string uid)
        {
            if (trained_characters.ContainsKey(uid))
                return trained_characters[uid];
            return null;
        }

        public void SetCharacterPosition(string uid, string scene, Vector3 pos, Quaternion rot)
        {
            TrainedCharacterData cdata = GetCharacter(uid);
            if (cdata != null)
            {
                cdata.scene = scene;
                cdata.pos = pos;
                cdata.rot = rot;
            }
            else
            {
                //Characters that were not spawned are saved as scene objects
                SceneObjectData sobj = GetOrCreateSceneObject(uid, scene);
                if (sobj != null)
                {
                    sobj.pos = pos;
                    sobj.rot = rot;
                }
            }
        }

        public SceneObjectData GetOrCreateSceneObject(string uid, string scene)
        {
            SceneObjectData sobj = GetSceneObject(uid);
            if (sobj != null && sobj.scene == scene)
                return sobj;
            
            if (!string.IsNullOrEmpty(uid))
            {
                SceneObjectData nobj = new SceneObjectData();
                nobj.uid = uid;
                nobj.scene = scene;
                scene_objects[uid] = nobj;
                return nobj;
            }
            return null;
        }

        public SceneObjectData GetSceneObject(string uid)
        {
            if (scene_objects.ContainsKey(uid))
                return scene_objects[uid];
            return null;
        }

        public SpawnedData AddSpawnedObject(string id, string scene, Vector3 pos, Quaternion rot, float scale)
        {
            SpawnedData sdata = new SpawnedData();
            sdata.id = id;
            sdata.uid = UniqueID.GenerateUniqueID();
            sdata.scene = scene;
            sdata.pos = pos;
            sdata.rot = rot;
            sdata.scale = scale;
            spawned_objects[sdata.uid] = sdata;
            return sdata;
        }

        public void RemoveSpawnedObject(string uid)
        {
            if (spawned_objects.ContainsKey(uid))
                spawned_objects.Remove(uid);
        }

        public SpawnedData GetSpawnedObject(string uid)
        {
            if (spawned_objects.ContainsKey(uid))
                return spawned_objects[uid];
            return null;
        }

        //---- World Regrowth -----

        public void AddWorldRegrowth(string uid, RegrowthData data)
        {
            world_regrowth[uid] = data;
        }

        public void RemoveWorldRegrowth(string uid)
        {
            if (world_regrowth.ContainsKey(uid))
                world_regrowth.Remove(uid);
        }

        public RegrowthData GetWorldRegrowth(string uid)
        {
            if (world_regrowth.ContainsKey(uid))
                return world_regrowth[uid];
            return null;
        }

        public bool HasWorldRegrowth(string uid)
        {
            return world_regrowth.ContainsKey(uid);
        }

        //---- Destructibles -----

        public void RemoveObject(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
                removed_objects[uid] = 1;
        }

        public void ClearRemovedObject(string uid) {
            if (removed_objects.ContainsKey(uid))
                removed_objects.Remove(uid);
        }

        public bool IsObjectRemoved(string uid)
        {
            if (removed_objects.ContainsKey(uid))
                return removed_objects[uid] > 0;
            return false;
        }

        //----- Hide/show generic objects ------

        public void HideObject(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
                hidden_objects[uid] = 1;
        }

        public void ShowObject(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
                hidden_objects[uid] = 0;
        }

        public bool IsObjectHidden(string uid)
        {
            if (hidden_objects.ContainsKey(uid))
                return hidden_objects[uid] > 0;
            return false;
        }

        public bool HasHiddenState(string uid)
        {
            return hidden_objects.ContainsKey(uid);
        }

        // ---- Unique Ids (Custom data) ----
        public void SetCustomInt(string unique_id, int val)
        {
            if (!string.IsNullOrEmpty(unique_id))
                unique_ids[unique_id] = val;
        }

        public void RemoveCustomInt(string unique_id)
        {
            if (unique_ids.ContainsKey(unique_id))
                unique_ids.Remove(unique_id);
        }

        public int GetCustomInt(string unique_id)
        {
            if (unique_ids.ContainsKey(unique_id))
                return unique_ids[unique_id];
            return 0;
        }

        public bool HasCustomInt(string unique_id)
        {
            return unique_ids.ContainsKey(unique_id);
        }

        public void SetCustomFloat(string unique_id, float val)
        {
            if (!string.IsNullOrEmpty(unique_id))
                unique_floats[unique_id] = val;
        }

        public void RemoveCustomFloat(string unique_id)
        {
            if (unique_floats.ContainsKey(unique_id))
                unique_floats.Remove(unique_id);
        }

        public float GetCustomFloat(string unique_id)
        {
            if (unique_floats.ContainsKey(unique_id))
                return unique_floats[unique_id];
            return 0;
        }

        public bool HasCustomFloat(string unique_id)
        {
            return unique_floats.ContainsKey(unique_id);
        }

        public void SetCustomString(string unique_id, string val)
        {
            if (!string.IsNullOrEmpty(unique_id))
                unique_strings[unique_id] = val;
        }

        public void RemoveCustomString(string unique_id)
        {
            if (unique_strings.ContainsKey(unique_id))
                unique_strings.Remove(unique_id);
        }

        public string GetCustomString(string unique_id)
        {
            if (unique_strings.ContainsKey(unique_id))
                return unique_strings[unique_id];
            return "";
        }

        public bool HasCustomString(string unique_id)
        {
            return unique_strings.ContainsKey(unique_id);
        }
		
		public void RemoveAllCustom(string unique_id)
        {
            RemoveCustomString(unique_id);
            RemoveCustomFloat(unique_id);
            RemoveCustomInt(unique_id);
        }

        // ---- Multi-inventory Items -----

        public void SwapInventoryItems(InventoryData inventory1, int slot1, InventoryData inventory2, int slot2) 
        {
            InventoryItemData invt_slot1 = inventory1.GetInventoryItem(slot1);
            InventoryItemData invt_slot2 = inventory2.GetInventoryItem(slot2);
            ItemData idata1 = ItemData.Get(invt_slot1?.item_id);
            ItemData idata2 = ItemData.Get(invt_slot2?.item_id);

            if (idata1 && idata1.IsBag() && inventory2.type == InventoryType.Bag)
                return; //Cant put bag into bag
            if (idata2 && idata2.IsBag() && inventory1.type == InventoryType.Bag)
                return; //Cant put bag into bag

            inventory1.items[slot1] = invt_slot2;
            inventory2.items[slot2] = invt_slot1;

            if (invt_slot2 == null)
                inventory1.items.Remove(slot1);
            if (invt_slot1 == null)
                inventory2.items.Remove(slot2);
        }

        public void CombineInventoryItems(InventoryData inventory1, int slot1, InventoryData inventory2, int slot2)
        {
            InventoryItemData invt_slot1 = inventory1.GetInventoryItem(slot1);
            InventoryItemData invt_slot2 = inventory2.GetInventoryItem(slot2);

            if (invt_slot1.item_id == invt_slot2.item_id) {
                inventory1.RemoveItemAt(slot1, invt_slot1.quantity);
                inventory2.AddItemAt(invt_slot1.item_id, slot2, invt_slot1.quantity, invt_slot1.durability, invt_slot1.uid);
            }
        }

        // ---- Generic ------

        public InventoryData GetInventory(InventoryType type, string inventory_uid)
        {
            InventoryData sdata = null;
            if (!string.IsNullOrEmpty(inventory_uid))
            {
                if (inventories.ContainsKey(inventory_uid))
                {
                    sdata = inventories[inventory_uid];
                }
                else
                {
                    //Create new if dont exist
                    sdata = new InventoryData(type, inventory_uid);
                    inventories[inventory_uid] = sdata;
                }
            }
            return sdata;
        }

        public InventoryData GetInventory(InventoryType type, int player_id)
        {
            string uid = GetPlayerUID(player_id);
            return GetInventory(type, uid);
        }

        public InventoryData GetEquipInventory(InventoryType type, int player_id)
        {
            string uid = GetPlayerEquipUID(player_id);
            return GetInventory(type, uid);
        }

        public bool HasInventory(int player_id)
        {
            return HasInventory(GetPlayerUID(player_id));
        }

        public bool HasEquipInventory(int player_id)
        {
            return HasInventory(GetPlayerEquipUID(player_id));
        }

        public bool HasInventory(string inventory_uid)
        {
            if (!string.IsNullOrEmpty(inventory_uid))
            {
                if (inventories.ContainsKey(inventory_uid))
                    return true;
            }
            return false;
        }

        public PlayerCharacterData GetPlayerCharacter(int player_id)
        {
            PlayerCharacterData cdata;
            if (player_characters.ContainsKey(player_id))
            {
                cdata = player_characters[player_id];
            }
            else
            {
                //Create new if dont exist
                cdata = new PlayerCharacterData(player_id);
                player_characters[player_id] = cdata;
            }
            return cdata;
        }

        public string GetPlayerUID(int player_id)
        {
            return "player_" + player_id;
        }

        public string GetPlayerEquipUID(int player_id)
        {
            return "player_equip_" + player_id;
        }

        public bool IsWorldGenerated()
        {
            return world_seed != 0;
        }

        public bool IsNewGame()
        {
            return play_time < 0.0001f;
        }

        public float GetTotalTime()
        {
            return (day - 1) * 24f + day_time;
        }

        //--- Save / load -----

        public bool IsVersionValid()
        {
            return version == Application.version;
        }

        public void Save()
        {
            Save(file_loaded, this);
        }

        public static void Save(string filename, PlayerData data)
        {
            if (!string.IsNullOrEmpty(filename) && data != null)
            {
                data.filename = filename;
                data.last_save = DateTime.Now;
                data.version = Application.version;
                player_data = data;
                file_loaded = filename;

                SaveTool.SaveFile<PlayerData>(filename + extension, data);
                SetLastSave(filename);
            }
        }

        public static void NewGame()
        {
            NewGame(GetLastSave()); //default name
        }

        //You should reload the scene right after NewGame
        public static PlayerData NewGame(string filename)
        {
            file_loaded = filename;
            player_data = new PlayerData(filename);
            player_data.FixData();
            return player_data;
        }

        public static PlayerData Load(string filename)
        {
            if (player_data == null || file_loaded != filename)
            {
                player_data = SaveTool.LoadFile<PlayerData>(filename + extension);
                if (player_data != null)
                {
                    file_loaded = filename;
                    player_data.FixData();
                }
            }
            return player_data;
        }

        public static PlayerData LoadLast()
        {
            return AutoLoad(GetLastSave());
        }

        //Load if found, otherwise new game
        public static PlayerData AutoLoad(string filename)
        {
            //if (player_data == null)
            //    player_data = Load(filename);
            if (player_data == null)
                player_data = NewGame(filename);
            return player_data;
        }

        public static void SetLastSave(string filename)
        {
            if (SaveTool.IsValidFilename(filename))
            {
                PlayerPrefs.SetString(last_save_id, filename);
            }
        }

        public static string GetLastSave()
        {
            string name = PlayerPrefs.GetString(last_save_id, "");
            if (string.IsNullOrEmpty(name))
                name = "player"; //Default name
            return name;
        }

        public static bool HasLastSave()
        {
            return HasSave(GetLastSave());
        }

        public static bool HasSave(string filename)
        {
            return SaveTool.DoesFileExist(filename + extension);
        }

        public static void Unload()
        {
            player_data = null;
            file_loaded = "";
        }

        public static void Delete(string filename)
        {
            if (file_loaded == filename)
            {
                player_data = new PlayerData(filename);
                player_data.FixData();
            }

            SaveTool.DeleteFile(filename + extension);
        }

        public static bool IsLoaded()
        {
            return player_data != null && !string.IsNullOrEmpty(file_loaded);
        }

        public static PlayerData Get()
        {
            return player_data;
        }
    }


    [System.Serializable]
    public class DroppedItemData
    {
        public string uid;
        public string item_id;
        public string scene;
        public Vector3Data pos;
        public int quantity;
        public float durability;
    }

    [System.Serializable]
    public class BuiltConstructionData
    {
        public string uid;
        public string construction_id;
        public string scene;
        public Vector3Data pos;
        public QuaternionData rot;
        public float durability;
    }

    [System.Serializable]
    public class SowedPlantData
    {
        public string uid;
        public string plant_id;
        public string scene;
        public Vector3Data pos;
        public QuaternionData rot;
        public int growth_stage;
    }

    [System.Serializable]
    public class TrainedCharacterData
    {
        public string uid;
        public string character_id;
        public string scene;
        public Vector3Data pos;
        public QuaternionData rot;
    }

    [System.Serializable]
    public class SpawnedData
    {
        public string id;
        public string uid;
        public string scene;
        public Vector3Data pos;
        public QuaternionData rot;
        public float scale;
    }

    [System.Serializable]
    public class SceneObjectData
    {
        public string uid;
        public string scene;
        public Vector3Data pos;
        public QuaternionData rot;
    }

    [System.Serializable]
    public class RegrowthData
    {
        public string data_id;
        public string uid; //uid of the original object
        public string scene;
        public Vector3Data pos;
        public QuaternionData rot;
        public int layer;
        public float scale;
        public float time; //Time left before regrowth
        public float probability; //Probability to spawn after time expire
    }

    public enum TimeType
    {
        GameHours = 0,
        GameDays = 10,
    }

}