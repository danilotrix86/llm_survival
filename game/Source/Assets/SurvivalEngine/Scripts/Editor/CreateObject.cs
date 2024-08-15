using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace SurvivalEngine.EditorTool
{
    public enum CreateObjectType
    {
        None,
        Item,
        Construction,
        Plant,
        Character,
        Destructible,
        Selectable
    }

    /// <summary>
    /// Use this tool to easily generate a new object.
    /// It will link the data file to the prefab and attach all necessary components for the type of object you want to create.
    /// </summary>

    public class CreateObject : ScriptableWizard
    {
        [Header("New Object")]
        public string object_title;
        public CreateObjectType type;
        public GameObject mesh;
        public Sprite icon;

        [Header("Items Only")]
        public ItemType item_type;
        public EquipSlot equip_slot;
        public EquipSide equip_side;

        [Header("Plant Only")]
        public GameObject[] stages_mesh;
        public GameObject soil_mesh;

        [Header("Default Settings")]
        public CreateObjectSettings settings;

        [MenuItem("Survival Engine/Create New Object", priority = 1)]
        static void ScriptableWizardMenu()
        {
            ScriptableWizard.DisplayWizard<CreateObject>("CreateObject", "CreateObject");
        }

        void DoCreateObject()
        {
            if (settings == null)
            {
                Debug.LogError("Settings must not be null");
                return;
            }

            //Find data folder
            string folder = "";
            if (type == CreateObjectType.Item)
                folder = settings.items_folder;
            if (type == CreateObjectType.Construction)
                folder = settings.constructions_folder;
            if (type == CreateObjectType.Plant)
                folder = settings.plants_folder;
            if (type == CreateObjectType.Character)
                folder = settings.characters_folder;

            if (type == CreateObjectType.None)
            {
                Debug.LogError("Type can't be none!");
                return;
            }

            if (mesh == null)
            {
                Debug.LogError("A mesh must be assigned!");
                return;
            }

            if (string.IsNullOrEmpty(object_title.Trim()))
            {
                Debug.LogError("Title can't be blank");
                return;
            }

            //Make sure folder is valid
            string full_folder = Application.dataPath + "/" + folder;
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(full_folder))
            {
                Debug.LogError("Error, folder can't be found: " + full_folder);
                return;
            }

            //Make sure folder is valid
            string full_folder_equip = Application.dataPath + "/" + settings.prefab_equip_folder;
            if (type == CreateObjectType.Item && item_type == ItemType.Equipment && !Directory.Exists(full_folder_equip))
            {
                Debug.LogError("Error, folder can't be found: " + full_folder_equip);
                return;
            }

            //Make sure file don't already exists
            string file_title = object_title.Replace(" ", "");
            string full_file = full_folder + "/" + file_title + ".asset";
            if (!string.IsNullOrEmpty(folder) && File.Exists(full_file))
            {
                Debug.LogError("Error, file already exists: " + full_file);
                return;
            }

            //Make sure prefab don't already exists
            string full_prefab = Application.dataPath + "/" + settings.prefab_folder + "/" + file_title + ".prefab";
            if (File.Exists(full_prefab))
            {
                Debug.LogError("Error, prefab file already exists: " + full_prefab);
                return;
            }

            //---------------

            string file_data = "Assets/" + folder + "/" + file_title + ".asset";
            string file_prefab = "Assets/" + settings.prefab_folder + "/" + file_title + ".prefab";
            string file_prefab_equip = "Assets/" + settings.prefab_equip_folder + "/" + file_title + "Equip.prefab";

            bool is_plant_growth = type == CreateObjectType.Plant && stages_mesh != null && stages_mesh.Length > 1;
            string obj_title = is_plant_growth ? object_title + "S1" : object_title;
            GameObject obj = CreateBasicObject(obj_title, is_plant_growth ? stages_mesh[0] : mesh);
            Selectable select = obj.GetComponent<Selectable>();
            UniqueID uid = obj.GetComponent<UniqueID>();
            GameObject prefab = null;

            //Create data
            if (type == CreateObjectType.Item)
            {
                Item item = obj.AddComponent<Item>();
                ItemData data = CreateAsset<ItemData>(file_data);
                item.data = data;
                item.take_audio = settings.take_audio;
                item.take_fx = settings.take_fx;
                uid.uid_prefix = "item_";
                select.use_range = 1f;

                prefab = CreatePrefab(obj, file_prefab);
                data.item_prefab = prefab;
                data.type = item_type;

                List<SAction> actions = new List<SAction>();

                if (item_type == ItemType.Consumable)
                {
                    actions.Add(settings.eat_action);
                    data.eat_hp = 1;
                    data.eat_hunger = 5;
                    data.durability_type = DurabilityType.Spoilage;
                }

                if (item_type == ItemType.Equipment)
                {
                    GameObject obj_equip = new GameObject(object_title + "Equip");
                    obj_equip.transform.position = FindPosition();

                    GameObject mesh_equip = Instantiate(mesh, obj_equip.transform.position, mesh.transform.rotation);
                    mesh_equip.name = object_title + "Mesh";
                    mesh_equip.transform.SetParent(obj_equip.transform);
                    mesh_equip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f) * mesh.transform.rotation;

                    EquipItem equip_item = obj_equip.AddComponent<EquipItem>();
                    equip_item.data = data;
                    data.inventory_max = 1;
                    data.equip_slot = equip_slot;
                    data.equip_side = equip_side;
                    data.durability_type = DurabilityType.UsageCount;

                    if (data.equip_slot == EquipSlot.Hand)
                    {
                        data.damage = 10;
                        data.weapon_type = WeaponType.WeaponMelee;
                    }
                    else
                    {
                        data.armor = 2;
                    }

                    actions.Add(settings.equip_action);

                    GameObject equip_prefab = CreatePrefab(obj_equip, file_prefab_equip);
                    data.equipped_prefab = equip_prefab;
                    DestroyImmediate(obj_equip);
                }

                actions.AddRange(settings.item_actions);
                data.actions = actions.ToArray();
				
				EditorUtility.SetDirty(data);
            }

            else if (type == CreateObjectType.Construction)
            {
                Buildable buildable = obj.AddComponent<Buildable>();
                Construction construct = obj.AddComponent<Construction>();
                ConstructionData data = CreateAsset<ConstructionData>(file_data);
                construct.data = data;
                buildable.build_audio = settings.build_audio;
                buildable.build_fx = settings.build_fx;
                uid.uid_prefix = "construction_";
                prefab = CreatePrefab(obj, file_prefab);
                data.construction_prefab = prefab;
                select.use_range = 2f;
				EditorUtility.SetDirty(data);
            }

            else if (type == CreateObjectType.Plant)
            {
                int nb_stages = stages_mesh != null && stages_mesh.Length > 0 ? stages_mesh.Length : 1;

                PlantData data = CreateAsset<PlantData>(file_data);

                List<GameObject> growth_prefabs = new List<GameObject>();

                for (int i = 0; i < nb_stages; i++)
                {
                    string stage_suffix = nb_stages > 1 ? "S" + (i + 1) : "";

                    GameObject obj_stage = i == 0 ? obj : CreateBasicObject(object_title + stage_suffix, stages_mesh[i]);
                    Selectable select_stage = obj_stage.GetComponent<Selectable>();
                    UniqueID auid = obj_stage.GetComponent<UniqueID>();
                    Buildable buildable = obj.AddComponent<Buildable>();
                    Plant plant = obj_stage.AddComponent<Plant>();
                    plant.data = data;
                    plant.gather_audio = settings.take_audio;
                    buildable.build_audio = settings.build_audio;
                    buildable.build_fx = settings.build_fx;
                    auid.uid_prefix = "plant_";
                    select_stage.use_range = 1f;
                    plant.growth_stage = i;

                    //Soil mesh
                    if (soil_mesh != null)
                    {
                        GameObject msh = Instantiate(soil_mesh, obj_stage.transform.position, soil_mesh.transform.rotation);
                        msh.name = "SoilMesh";
                        msh.transform.SetParent(obj_stage.transform);
                        msh.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    }

                    file_prefab = "Assets/" + settings.prefab_folder + "/" + file_title + stage_suffix + ".prefab";
                    prefab = CreatePrefab(obj_stage, file_prefab);
                    data.plant_prefab = prefab; //Set to last created
                    growth_prefabs.Add(prefab);
                }

                if (nb_stages > 1)
                    data.growth_stage_prefabs = growth_prefabs.ToArray();
				
				EditorUtility.SetDirty(data);
            }

            else if (type == CreateObjectType.Character)
            {
                Character character = obj.AddComponent<Character>();
                CharacterData data = CreateAsset<CharacterData>(file_data);
                character.data = data;
                uid.uid_prefix = "character_";
                prefab = CreatePrefab(obj, file_prefab);
                data.character_prefab = prefab;
                select.use_range = 2f;
                character.attack_audio = settings.attack_audio;
				EditorUtility.SetDirty(data);
            }

            else
            {
                prefab = CreatePrefab(obj, file_prefab);
            }

            if (prefab != null)
                Selection.activeObject = prefab;

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

        private GameObject CreateBasicObject(string otitle, GameObject mesh_prefab)
        {
            //Create object
            GameObject obj = new GameObject(otitle);
            obj.transform.position = FindPosition();

            //Mesh
            GameObject msh = Instantiate(mesh_prefab, obj.transform.position, mesh_prefab.transform.rotation);
            msh.name = otitle + "Mesh";
            msh.transform.SetParent(obj.transform);
            msh.transform.localPosition = new Vector3(0f, 0.2f, 0f);

            //Create rigid
            if (type == CreateObjectType.Construction || type == CreateObjectType.Character)
            {
                Rigidbody rigid = obj.AddComponent<Rigidbody>();
                rigid.useGravity = false;
                rigid.isKinematic = type == CreateObjectType.Construction;
                rigid.mass = 100f;
                rigid.drag = 5f;
                rigid.angularDrag = 5f;
                rigid.interpolation = type == CreateObjectType.Character ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
                rigid.constraints = RigidbodyConstraints.FreezeAll;
                if (type == CreateObjectType.Character)
                    rigid.constraints = RigidbodyConstraints.FreezeRotation;
            }

            //Collider
            SphereCollider collide = obj.AddComponent<SphereCollider>();
            collide.isTrigger = true;
            collide.radius = 0.4f;
            collide.center = new Vector3(0f, 0.2f, 0f);

            //Create selectable
            Selectable select = obj.AddComponent<Selectable>();
            select.generate_outline = true;
            select.outline_material = settings.outline;

            //Create UID
            obj.AddComponent<UniqueID>();

            //Create destruct
            if (type == CreateObjectType.Destructible || type == CreateObjectType.Plant || type == CreateObjectType.Construction || type == CreateObjectType.Character)
            {
                Destructible destruct = obj.AddComponent<Destructible>();
                destruct.death_fx = settings.death_fx;
                destruct.target_team = AttackTeam.Neutral;
                if (type == CreateObjectType.Construction)
                    destruct.target_team = AttackTeam.Ally;
                destruct.attack_melee_only = type != CreateObjectType.Character;
            }
            return obj;
        }

        private GameObject CreatePrefab(GameObject obj, string file)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(obj, file, InteractionMode.AutomatedAction);
            prefab.transform.position = Vector3.zero;
            return prefab;
        }

        private T CreateAsset<T>(string file) where T : CraftData
        {
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, file);
            asset.id = object_title.ToLower().Replace(" ", "_");
            asset.title = object_title;
            asset.craft_sound = settings.craft_audio;
            asset.icon = icon;
            return asset;
        }

        private Vector3 FindPosition()
        {
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                Camera cam = SceneView.lastActiveSceneView.camera;
                Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                Plane plane = new Plane(Vector3.up, 0f);
                float dist;
                bool success = plane.Raycast(ray, out dist);
                if (success)
                {
                    Vector3 pos = ray.origin + ray.direction * dist;
                    return new Vector3(pos.x, 0f, pos.z);
                }
            }
            return Vector3.zero;
        }

        void OnWizardCreate()
        {
            DoCreateObject();
        }

        void OnWizardUpdate()
        {
            helpString = "Use this tool to create a new prefab and its data file.";
        }
    }

}