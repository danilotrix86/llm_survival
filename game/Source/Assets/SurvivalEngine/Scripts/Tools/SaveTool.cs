using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Script to write a class to the disk, or to read a file containing class from the disk
    /// </summary>

    [System.Serializable]
    public class SaveTool
    {
        //Load any file to a class, make sure the class is marked with [System.Serializable]
        public static T LoadFile<T>(string filename) where T : class
        {
            T data = null;
            string fullpath = Application.persistentDataPath + "/" + filename;
            if (IsValidFilename(filename) && File.Exists(fullpath))
            {
                FileStream file = null;
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    file = File.Open(fullpath, FileMode.Open);
                    data = (T)bf.Deserialize(file);
                    file.Close();
                }
                catch (System.Exception e) { Debug.Log("Error Loading Data " + e); if (file != null) file.Close(); }
            }
            return data;
        }

        //Save any class to a file, make sure the class is marked with [System.Serializable]
        public static void SaveFile<T>(string filename, T data) where T : class
        {
            if (IsValidFilename(filename))
            {
                FileStream file = null;
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    string fullpath = Application.persistentDataPath + "/" + filename;
                    file = File.Create(fullpath);
                    bf.Serialize(file, data);
                    file.Close();
                }
                catch (System.Exception e) { Debug.Log("Error Saving Data " + e); if (file != null) file.Close(); }
            }
        }

        public static void DeleteFile(string filename)
        {
            string fullpath = Application.persistentDataPath + "/" + filename;
            if (File.Exists(fullpath))
                File.Delete(fullpath);
        }

        //Return all save files
        public static List<string> GetAllSave(string extension = "")
        {
            List<string> saves = new List<string>();
            string[] files = Directory.GetFiles(Application.persistentDataPath);
            foreach (string file in files)
            {
                if (file.EndsWith(extension))
                {
                    string filename = Path.GetFileName(file);
                    if (!saves.Contains(filename))
                        saves.Add(filename);
                }
            }
            return saves;
        }

        public static bool DoesFileExist(string filename)
        {
            string fullpath = Application.persistentDataPath + "/" + filename;
            return IsValidFilename(filename) && File.Exists(fullpath);
        }

        public static bool IsValidFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return false; //Filename cant be blank

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (filename.Contains(c.ToString()))
                    return false; //Dont allow any special characters
            }
            return true;
        }
    }

}