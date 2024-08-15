using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace SurvivalEngine.EditorTool
{
    /// <summary>
    /// Use this tool to easily dupplicate a CraftData object and all links
    /// </summary>

    public class DuplicateObject : ScriptableWizard
    {
        [Header("New Object")]
        public CraftData source;
        public string object_title;

        private Dictionary<int, string> copied_prefabs = new Dictionary<int, string>();

        [MenuItem("Survival Engine/Duplicate Object", priority = 2)]
        static void ScriptableWizardMenu()
        {
            ScriptableWizard.DisplayWizard<DuplicateObject>("DuplicateObject", "DuplicateObject");
        }

        void DoDuplicateObject()
        {
            if (source == null)
            {
                Debug.LogError("A source must be assigned!");
                return;
            }

            if (string.IsNullOrEmpty(object_title.Trim()))
            {
                Debug.LogError("Title can't be blank");
                return;
            }
;
            copied_prefabs.Clear();

            if (source is ItemData)
            {
                ItemData nitem = CopyAsset<ItemData>((ItemData)source, object_title);

                if (nitem != null && nitem.item_prefab != null)
                {
                    GameObject nprefab = CopyPrefab(nitem.item_prefab, object_title);
                    nitem.item_prefab = nprefab;
                    Item item = nprefab.GetComponent<Item>();
                    if (item != null)
                        item.data = nitem;
                }

                if (nitem != null && nitem.equipped_prefab != null)
                {
                    GameObject nprefab = CopyPrefab(nitem.equipped_prefab, object_title + "Equip");
                    nitem.equipped_prefab = nprefab;
                    EquipItem item = nprefab.GetComponent<EquipItem>();
                    if (item != null)
                        item.data = nitem;
                }

                Selection.activeObject = nitem;
            }

            if (source is CharacterData)
            {
                CharacterData nitem = CopyAsset<CharacterData>((CharacterData)source, object_title);

                if (nitem != null && nitem.character_prefab != null)
                {
                    GameObject nprefab = CopyPrefab(nitem.character_prefab, object_title);
                    nitem.character_prefab = nprefab;
                    Character character = nprefab.GetComponent<Character>();
                    if (character != null)
                        character.data = nitem;
                }

                Selection.activeObject = nitem;
            }

            if (source is ConstructionData)
            {
                ConstructionData nitem = CopyAsset<ConstructionData>((ConstructionData)source, object_title);

                if (nitem != null && nitem.construction_prefab != null)
                {
                    GameObject nprefab = CopyPrefab(nitem.construction_prefab, object_title);
                    nitem.construction_prefab = nprefab;
                    Construction construct = nprefab.GetComponent<Construction>();
                    if (construct != null)
                        construct.data = nitem;
                }

                Selection.activeObject = nitem;
            }

            if (source is PlantData)
            {
                PlantData nitem = CopyAsset<PlantData>((PlantData)source, object_title);

                if (nitem != null)
                {
                    if (nitem.growth_stage_prefabs.Length == 0 && nitem.plant_prefab != null)
                    {
                        GameObject nprefab = CopyPrefab(nitem.plant_prefab, object_title);
                        nitem.plant_prefab = nprefab;
                        Plant plant = nprefab.GetComponent<Plant>();
                        if (plant != null)
                            plant.data = nitem;
                    }
                    else
                    {
                        for (int i = 0; i < nitem.growth_stage_prefabs.Length; i++)
                        {
                            GameObject sprefab = CopyPrefab(nitem.growth_stage_prefabs[i], object_title + "S" + (i + 1));
                            nitem.growth_stage_prefabs[i] = sprefab;
                            nitem.plant_prefab = sprefab; 
                            Plant plant_stage = sprefab.GetComponent<Plant>();
                            if (plant_stage != null)
                                plant_stage.data = nitem;
                        }
                    }
                }

                Selection.activeObject = nitem;
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }

        private GameObject CopyPrefab(GameObject prefab, string title)
        {
            if (prefab != null)
            {
                //Fast access to already copied prefab
                if (copied_prefabs.ContainsKey(prefab.GetInstanceID()))
                {
                    string cpath = copied_prefabs[prefab.GetInstanceID()];
                    GameObject cprefab = AssetDatabase.LoadAssetAtPath<GameObject>(cpath);
                    if(cprefab != null)
                        return cprefab;
                }

                //Otherwise copy
                string path = AssetDatabase.GetAssetPath(prefab);
                string folder = Path.GetDirectoryName(path);
                string ext = Path.GetExtension(path);
                string filename = title.Replace(" ", "").Replace("/", "");
                string npath = folder + "/" + filename + ext;

                if (!Directory.Exists(folder))
                {
                    Debug.LogError("Folder does not exist: " + folder);
                    return null;
                }

                if (File.Exists(npath))
                {
                    Debug.LogError("File already exists: " + npath);
                    return null;
                }

                AssetDatabase.CopyAsset(path, npath);
                GameObject nprefab = AssetDatabase.LoadAssetAtPath<GameObject>(npath);
                if (nprefab != null)
                {
                    nprefab.name = filename;
                    copied_prefabs[prefab.GetInstanceID()] = npath;
                    return nprefab;
                }
            }
            return null;
        }

        private T CopyAsset<T>(T asset, string title) where T : CraftData
        {
            if (asset != null)
            {
                string path = AssetDatabase.GetAssetPath(asset);
                string folder = Path.GetDirectoryName(path);
                string ext = Path.GetExtension(path);
                string filename = title.Replace(" ", "").Replace("/", "");
                string fileid = title.Trim().Replace(" ", "_").ToLower();
                string npath = folder + "/" + filename + ext;

                if (!Directory.Exists(folder))
                {
                    Debug.LogError("Folder does not exist: " + folder);
                    return null;
                }

                if (File.Exists(npath))
                {
                    Debug.LogError("File already exists: " + npath);
                    return null;
                }

                AssetDatabase.CopyAsset(path, npath);
                T nasset = AssetDatabase.LoadAssetAtPath<T>(npath);
                if (nasset != null)
                {
                    nasset.name = filename;
                    nasset.title = title;
                    nasset.id = fileid;
                    return nasset;
                }
            }
            return null;
        }

        void OnWizardCreate()
        {
            DoDuplicateObject();
        }

        void OnWizardUpdate()
        {
            helpString = "Use this tool to duplicate a prefab and its data file.";
        }
    }

}