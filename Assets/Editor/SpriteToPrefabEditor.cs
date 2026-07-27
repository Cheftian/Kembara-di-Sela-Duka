using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteToPrefabEditor : EditorWindow
{
    [MenuItem("Tools/Convert Sprites to Prefabs")]
    public static void Convert()
    {
        // 1. Ambil objek gambar yang sedang Anda pilih di Project Window
        Object[] selectedObjects = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Silakan pilih file Spritesheet terlebih dahulu di folder Project!", "OK");
            return;
        }

        // 2. Buat folder tujuan untuk menyimpan prefab
        string targetFolder = "Assets/GeneratedPrefabs";
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        int count = 0;

        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            // Ambil semua sub-sprite dari spritesheet ini
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    // Buat GameObject sementara di memori
                    GameObject go = new GameObject(sprite.name);
                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;

                    // Simpan sebagai Prefab ke folder Assets/GeneratedPrefabs
                    string prefabPath = $"{targetFolder}/{sprite.name}.prefab";
                    PrefabUtility.SaveAsPrefabAsset(go, prefabPath);

                    // Hapus GameObject sementara agar tidak menumpuk di memori
                    DestroyImmediate(go);
                    count++;
                }
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Sukses", $"{count} Prefab berhasil dibuat di folder {targetFolder}!", "OK");
    }
}
