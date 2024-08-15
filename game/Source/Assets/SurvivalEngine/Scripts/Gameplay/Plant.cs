using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Plants can be sowed (from a seed) and their fruit can be harvested. They can also have multiple growth stages.
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Buildable))]
    [RequireComponent(typeof(UniqueID))]
    [RequireComponent(typeof(Destructible))]
    public class Plant : Craftable
    {
        [Header("Plant")]
        public PlantData data;
        public int growth_stage = 0;

        [Header("Growth")]
        public float grow_time = 8f;
        public float water_grow_boost = 1f; //In percentage (0f = no difference, 0.5f = 50% boost)
        public float water_duration = 4f; //In game hours
        public bool regrow_on_death; //If true, will go back to stage 1 instead of being destroyed

        [Header("Harvest")]
        public ItemData fruit;
        public float fruit_grow_time = 0f; //In game hours
        public Transform fruit_model;
        public bool death_on_harvest;

        [Header("FX")]
        public GameObject gather_fx;
        public AudioClip gather_audio;

        [HideInInspector]
        public bool was_spawned = false; //If true, means it was crafted or loaded from save file

        private Selectable selectable;
        private Buildable buildable;
        private Destructible destruct;
        private UniqueID unique_id;

        private int nb_stages = 1;

        private float fruit_progress = 0f;
        private float growth_progress = 0f;
        private bool has_fruit = false;

        private float boost_mult = 1f;
        private float boost_timer = 0f;
        private float update_timer = 0f;

        private static List<Plant> plant_list = new List<Plant>();
		private GroupData plantGroup;

        protected override void Awake()
        {
            base.Awake();
            plant_list.Add(this);
            selectable = GetComponent<Selectable>();
            buildable = GetComponent<Buildable>();
            destruct = GetComponent<Destructible>();
            unique_id = GetComponent<UniqueID>();
            selectable.onDestroy += OnDeath;
            buildable.onBuild += OnBuild;

            if(data != null)
                nb_stages = Mathf.Max(data.growth_stage_prefabs.Length, 1);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            plant_list.Remove(this);
        }

        void Start()
        {
            if (!was_spawned && PlayerData.Get().IsObjectRemoved(GetUID()))
            {
                Destroy(gameObject);
                return;
            }

            //Fruit
            if (PlayerData.Get().HasCustomInt(GetSubUID("fruit")))
                has_fruit = PlayerData.Get().GetCustomInt(GetSubUID("fruit")) > 0;

            //Progress
            if (PlayerData.Get().HasCustomFloat(GetSubUID("progress")))
            {
                growth_progress = PlayerData.Get().GetCustomFloat(GetSubUID("progress"));
                fruit_progress = PlayerData.Get().GetCustomFloat(GetSubUID("progress"));
            }


			if(selectable.groups[0]){
				plantGroup = selectable.groups[0];
				if(!HasFruit()){

					selectable.RemoveGroup(plantGroup);
				}
			}

            RefreshFruitModel();
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (buildable.IsBuilding())
                return;

            float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();
            growth_progress += game_speed * boost_mult * Time.deltaTime;
            fruit_progress += game_speed * boost_mult * Time.deltaTime;

            //Boost stop
            if (boost_timer > 0f)
            {
                boost_timer -= game_speed * Time.deltaTime;
                if (boost_timer <= 0.01f)
                    boost_mult = 1f;
            }

            update_timer += Time.deltaTime;
            if (update_timer > 0.5f)
            {
                update_timer = Random.Range(-0.1f, 0.1f);
                SlowUpdate();
            }
        }

        private void SlowUpdate()
        {
            if (!IsFullyGrown() && grow_time > 0.001f)
            {
                PlayerData.Get().SetCustomFloat(GetSubUID("progress"), growth_progress);
                if (growth_progress > grow_time)
                {
                    GrowPlant();
                    return;
                }
            }

            if (!has_fruit && fruit != null)
            {
                PlayerData.Get().SetCustomFloat(GetSubUID("progress"), fruit_progress);
                if (fruit_progress > fruit_grow_time)
                {
                    GrowFruit();
                    return;
                }
            }

            //Water 
            if (!HasWater() && TheGame.Get().IsWeather(WeatherEffect.Rain))
                Water();
        }

        public void GrowPlant()
        {
            if (!IsFullyGrown())
            {
                GrowPlant(growth_stage + 1);
            }
        }

        public void GrowPlant(int grow_stage)
        {
            if (data != null && grow_stage >= 0 && grow_stage < nb_stages)
            {
                SowedPlantData sdata = PlayerData.Get().GetSowedPlant(GetUID());
                if (sdata == null)
                {
                    //Remove this plant and create a new one (this one probably was already in the scene)
                    if (!was_spawned)
                        PlayerData.Get().RemoveObject(GetUID()); //Remove Unique id
                    sdata = PlayerData.Get().AddPlant(data.id, SceneNav.GetCurrentScene(), transform.position, transform.rotation, grow_stage);
                }
                else
                {
                    //Grow current plant from data
                    PlayerData.Get().GrowPlant(GetUID(), grow_stage);
                }

                growth_progress = 0f;
                PlayerData.Get().SetCustomFloat(GetSubUID("progress"), 0f);
                plant_list.Remove(this); //Remove from list so spawn works!

                Spawn(sdata.uid);
                Destroy(gameObject);
            }
        }

        public void GrowFruit()
        {
            if (fruit != null && !has_fruit)
            {
                has_fruit = true;
				if(plantGroup) selectable.AddGroup(plantGroup);
                fruit_progress = 0f;
                PlayerData.Get().SetCustomInt(GetSubUID("fruit"), 1);
                PlayerData.Get().SetCustomFloat(GetSubUID("progress"), 0f);
                RefreshFruitModel();
            }
        }

        public void Water()
        {
            boost_mult = (1f + water_grow_boost);
            boost_timer = water_duration;
        }

        public void Harvest(PlayerCharacter character)
        {
            if (fruit != null && has_fruit && character.Inventory.CanTakeItem(fruit, 1))
            {
                GameObject source = fruit_model != null ? fruit_model.gameObject : gameObject;
                character.Inventory.GainItem(fruit, 1, source.transform.position);

                RemoveFruit();

                if (death_on_harvest && destruct != null)
                    destruct.Kill();

                TheAudio.Get().PlaySFX("plant", gather_audio);

                if (gather_fx != null)
                    Instantiate(gather_fx, transform.position, Quaternion.identity);
            }
        }

        public void RemoveFruit()
        {
            if (has_fruit)
            {
                has_fruit = false;
                fruit_progress = 0f;
				if(plantGroup) selectable.RemoveGroup(plantGroup);
                PlayerData.Get().SetCustomFloat(GetSubUID("progress"), 0f);
                PlayerData.Get().SetCustomInt(GetSubUID("fruit"), 0);
                RefreshFruitModel();
            }
        }

        private void RefreshFruitModel()
        {
            if (fruit_model != null && has_fruit != fruit_model.gameObject.activeSelf)
                fruit_model.gameObject.SetActive(has_fruit);
        }

        public void Kill()
        {
            destruct.Kill();
        }

        public void KillNoLoot()
        {
            destruct.KillNoLoot(); //Such as when being eaten, dont spawn loot
        }

        private void OnBuild()
        {
            if (data != null)
            {
                SowedPlantData splant = PlayerData.Get().AddPlant(data.id, SceneNav.GetCurrentScene(), transform.position, transform.rotation, growth_stage);
                unique_id.unique_id = splant.uid;
            }
        }

        private void OnDeath()
        {
            if (data != null)
            {
                foreach (PlayerCharacter character in PlayerCharacter.GetAll())
                    character.SaveData.AddKillCount(data.id); //Add kill count
            }

            PlayerData.Get().RemovePlant(GetUID());
            if (!was_spawned)
                PlayerData.Get().RemoveObject(GetUID());

            if (HasFruit())
                Item.Create(fruit, transform.position, 1);

            if (data != null && regrow_on_death)
            {
                SowedPlantData sdata = PlayerData.Get().GetSowedPlant(GetUID());
                Create(data, transform.position, transform.rotation, 0);
            }
        }

        public bool HasFruit()
        {
            return has_fruit;
        }

        public bool HasWater()
        {
            return boost_timer > 0f;
        }

        public bool IsFullyGrown()
        {
            return (growth_stage + 1) >= nb_stages;
        }

        public bool IsBuilt()
        {
            return !IsDead() && !buildable.IsBuilding();
        }

        public bool IsDead()
        {
            return destruct.IsDead();
        }
        
        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id.unique_id);
        }

        public string GetUID()
        {
            return unique_id.unique_id;
        }

        public string GetSubUID(string tag)
        {
            return unique_id.GetSubUID(tag);
        }

        public bool HasGroup(GroupData group)
        {
            if (data != null)
                return data.HasGroup(group) || selectable.HasGroup(group);
            return selectable.HasGroup(group);
        }

        public Selectable GetSelectable()
        {
            return selectable;
        }

        public Destructible GetDestructible()
        {
            return destruct;
        }

        public Buildable GetBuildable()
        {
            return buildable;
        }

        public SowedPlantData SaveData
        {
            get { return PlayerData.Get().GetSowedPlant(GetUID()); }  //Can be null if not sowed or spawned
        }

        public static new Plant GetNearest(Vector3 pos, float range = 999f)
        {
            Plant nearest = null;
            float min_dist = range;
            foreach (Plant plant in plant_list)
            {
                float dist = (plant.transform.position - pos).magnitude;
                if (dist < min_dist && plant.IsBuilt())
                {
                    min_dist = dist;
                    nearest = plant;
                }
            }
            return nearest;
        }

        public static int CountInRange(Vector3 pos, float range)
        {
            int count = 0;
            foreach (Plant plant in GetAll())
            {
                float dist = (plant.transform.position - pos).magnitude;
                if (dist < range && plant.IsBuilt())
                    count++;
            }
            return count;
        }

        public static int CountInRange(PlantData data, Vector3 pos, float range)
        {
            int count = 0;
            foreach (Plant plant in GetAll())
            {
                if (plant.data == data && plant.IsBuilt())
                {
                    float dist = (plant.transform.position - pos).magnitude;
                    if (dist < range)
                        count++;
                }
            }
            return count;
        }

        public static Plant GetByUID(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (Plant plant in plant_list)
                {
                    if (plant.GetUID() == uid)
                        return plant;
                }
            }
            return null;
        }

        public static List<Plant> GetAllOf(PlantData data)
        {
            List<Plant> valid_list = new List<Plant>();
            foreach (Plant plant in plant_list)
            {
                if (plant.data == data)
                    valid_list.Add(plant);
            }
            return valid_list;
        }

        public static new List<Plant> GetAll()
        {
            return plant_list;
        }

        //Spawn an existing one in the save file (such as after loading)
        public static Plant Spawn(string uid, Transform parent = null)
        {
            SowedPlantData sdata = PlayerData.Get().GetSowedPlant(uid);
            if (sdata != null && sdata.scene == SceneNav.GetCurrentScene())
            {
                PlantData pdata = PlantData.Get(sdata.plant_id);
                if (pdata != null)
                {
                    GameObject prefab = pdata.GetStagePrefab(sdata.growth_stage);
                    GameObject build = Instantiate(prefab, sdata.pos, sdata.rot);
                    build.transform.parent = parent;

                    Plant plant = build.GetComponent<Plant>();
                    plant.data = pdata;
                    plant.growth_stage = sdata.growth_stage;
                    plant.was_spawned = true;
                    plant.unique_id.unique_id = uid;
                    return plant;
                }
            }
            return null;
        }

        //Create a totally new one, in build mode for player to place, will be saved after FinishBuild() is called, -1 = max stage
        public static Plant CreateBuildMode(PlantData data, Vector3 pos, int stage)
        {
            GameObject prefab = data.GetStagePrefab(stage);
            GameObject build = Instantiate(prefab, pos, prefab.transform.rotation);
            Plant plant = build.GetComponent<Plant>();
            plant.data = data;
            plant.was_spawned = true;

            if(stage >= 0 && stage < data.growth_stage_prefabs.Length)
                plant.growth_stage = stage;
            
            return plant;
        }

        //Create a totally new one that will be added to save file, already placed
        public static Plant Create(PlantData data, Vector3 pos, int stage)
        {
            Plant plant = CreateBuildMode(data, pos, stage);
            plant.buildable.FinishBuild();
            return plant;
        }

        public static Plant Create(PlantData data, Vector3 pos, Quaternion rot, int stage)
        {
            Plant plant = CreateBuildMode(data, pos, stage);
            plant.transform.rotation = rot;
            plant.buildable.FinishBuild();
            return plant;
        }
    }

}