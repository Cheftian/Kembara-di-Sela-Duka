using UnityEngine;
using System.IO;

public static class SaveSystem
{
    // Menggunakan persistentDataPath yang aman untuk semua platform di Unity 6
    private static string GetPath(int slot)
    {
        return Application.persistentDataPath + "/abovian_save_" + slot + ".json";
    }

    public static void SaveGame(GameData data, int slot)
    {
        string path = GetPath(slot);
        string json = JsonUtility.ToJson(data, true); // Parameter true untuk format JSON yang rapi
        File.WriteAllText(path, json);
        Debug.Log("Game Saved to: " + path);
    }

    public static GameData LoadGame(int slot)
    {
        string path = GetPath(slot);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<GameData>(json);
        }
        return null;
    }

    public static bool HasSave(int slot)
    {
        return File.Exists(GetPath(slot));
    }
}