using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class CSVToObjectiveSOConverter : EditorWindow
{
    [MenuItem("ZebaStudio/Import Objectives from CSV")]
    public static void ImportCSV()
    {
        string filePath = EditorUtility.OpenFilePanel("Pilih File CSV Objektif", "Assets", "csv");
        if (string.IsNullOrEmpty(filePath)) return;

        string targetFolder = Path.GetDirectoryName(filePath);
        if (targetFolder != null && targetFolder.Contains("Assets"))
        {
            targetFolder = targetFolder.Substring(targetFolder.IndexOf("Assets"));
        }
        else
        {
            targetFolder = "Assets";
        }

        List<string> lineList = new List<string>();
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using (StreamReader sr = new StreamReader(fs))
            {
                while (!sr.EndOfStream)
                {
                    lineList.Add(sr.ReadLine());
                }
            }
        }
        string[] lines = lineList.ToArray();

        if (lines.Length <= 1)
        {
            Debug.LogError("File CSV kosong atau hanya berisi header!");
            return;
        }

        string firstLine = lines[0];
        char delimiter = firstLine.Contains(";") ? ';' : ',';
        Debug.Log($"[Info] Mendeteksi pemisah kolom yang digunakan: '{delimiter}'");

        int createdCount = 0;
        int updatedCount = 0;
        int rejectedRows = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            List<string> columns = SplitCSVLine(line, delimiter);

            // Memastikan minimal ada 4 kolom (NamaFile, ObjectiveID, ObjectiveEN, ObjectiveIDN)
            if (columns.Count >= 4)
            {
                string targetFileName = columns[0].Trim();
                if (string.IsNullOrEmpty(targetFileName))
                {
                    rejectedRows++;
                    continue;
                }

                string savePath = Path.Combine(targetFolder, $"{targetFileName}.asset").Replace("\\", "/");
                ObjectiveData asset = AssetDatabase.LoadAssetAtPath<ObjectiveData>(savePath);

                if (asset != null)
                {
                    // Update asset yang sudah ada
                    Undo.RecordObject(asset, "Update Objective Data via CSV");
                    asset.objectiveID = columns[1].Trim();
                    asset.objectiveEN = columns[2].Replace("\\n", "\n").Trim();
                    asset.objectiveIDN = columns[3].Replace("\\n", "\n").Trim();
                    EditorUtility.SetDirty(asset);
                    updatedCount++;
                }
                else
                {
                    // Buat asset baru jika belum ada
                    asset = ScriptableObject.CreateInstance<ObjectiveData>();
                    asset.objectiveID = columns[1].Trim();
                    asset.objectiveEN = columns[2].Replace("\\n", "\n").Trim();
                    asset.objectiveIDN = columns[3].Replace("\\n", "\n").Trim();
                    AssetDatabase.CreateAsset(asset, savePath);
                    createdCount++;
                }
            }
            else
            {
                rejectedRows++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=lime><b>[Import Objektif Selesai]</b></color>\n" +
                  $"Folder Tujuan: <i>{targetFolder}</i>\n" +
                  $"🆕 File Baru Terbuat: <color=green><b>{createdCount}</b></color> | 🔄 Diperbarui: <color=yellow><b>{updatedCount}</b></color> | ❌ Ditolak: <color=red><b>{rejectedRows}</b></color>");
    }

    private static List<string> SplitCSVLine(string line, char delimiter)
    {
        List<string> result = new List<string>();
        string[] rawSplits = line.Split(delimiter);
        
        foreach (string raw in rawSplits)
        {
            string cleaned = raw.Trim();
            if (cleaned.StartsWith("\"") && cleaned.EndsWith("\"") && cleaned.Length >= 2)
            {
                cleaned = cleaned.Substring(1, cleaned.Length - 2);
            }
            cleaned = cleaned.Replace("\"\"", "\"");
            result.Add(cleaned);
        }
        return result;
    }
}
