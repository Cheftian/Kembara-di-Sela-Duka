using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class CSVToScriptableObjectConverter : EditorWindow
{
    [MenuItem("ZebaStudio/Import Narration from CSV")]
    public static void ImportCSV()
    {
        string filePath = EditorUtility.OpenFilePanel("Pilih File CSV Narasi", "Assets", "csv");
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

        // Deteksi pemisah kolom
        string firstLine = lines[0];
        char delimiter = firstLine.Contains(";") ? ';' : ',';
        Debug.Log($"[Info] Mendeteksi pemisah kolom yang digunakan: '{delimiter}'");

        Dictionary<string, List<NarrationData.DialogueStep>> narrationGroups = new Dictionary<string, List<NarrationData.DialogueStep>>();
        int totalRowsImported = 0;
        int rejectedRows = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // Memisahkan kolom dengan metode kustom yang aman dari gangguan tanda petik
            List<string> columns = SplitCSVLine(line, delimiter);

            // DIUBAH: Sekarang mengecek minimal ada 5 kolom (File, Karakter, Ekspresi, EN, ID)
            if (columns.Count >= 5)
            {
                string targetFileName = columns[0].Trim();
                if (string.IsNullOrEmpty(targetFileName))
                {
                    rejectedRows++;
                    continue;
                }

                // DIUBAH: Memasukkan data ke dialogueEN (kolom 4) dan dialogueID (kolom 5)
                NarrationData.DialogueStep step = new NarrationData.DialogueStep
                {
                    characterName = columns[1].Trim(),
                    expressionName = columns[2].Trim(),
                    dialogueEN = columns[3].Replace("\\n", "\n").Trim(),
                    dialogueID = columns[4].Replace("\\n", "\n").Trim()
                };

                if (!narrationGroups.ContainsKey(targetFileName))
                {
                    narrationGroups[targetFileName] = new List<NarrationData.DialogueStep>();
                }

                narrationGroups[targetFileName].Add(step);
                totalRowsImported++;
            }
            else
            {
                rejectedRows++;
            }
        }

        if (totalRowsImported == 0)
        {
            Debug.LogError($"[Gagal Import] Baris dialog terbaca 0! Terdeteksi {rejectedRows} baris tidak valid. " +
                           $"Pastikan struktur CSV Anda memiliki minimal 5 kolom (NamaFile, Karakter, Ekspresi, DialogueEN, DialogueID).");
            return;
        }

        int createdCount = 0;
        int updatedCount = 0;

        foreach (var group in narrationGroups)
        {
            string fileName = group.Key;
            List<NarrationData.DialogueStep> steps = group.Value;
            
            string savePath = Path.Combine(targetFolder, $"{fileName}.asset").Replace("\\", "/"); 

            NarrationData asset = AssetDatabase.LoadAssetAtPath<NarrationData>(savePath);

            if (asset != null)
            {
                Undo.RecordObject(asset, "Update Narration Data via CSV");
                asset.dialogueSteps = steps.ToArray();
                EditorUtility.SetDirty(asset);
                updatedCount++;
            }
            else
            {
                asset = ScriptableObject.CreateInstance<NarrationData>();
                asset.dialogueSteps = steps.ToArray();
                AssetDatabase.CreateAsset(asset, savePath);
                createdCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=cyan><b>[Import Selesai]</b></color> Berhasil memproses total <b>{totalRowsImported}</b> baris dialog.\n" +
                  $"Folder Tujuan: <i>{targetFolder}</i>\n" +
                  $"🆕 File Baru: <color=green><b>{createdCount}</b></color> | 🔄 Diperbarui: <color=yellow><b>{updatedCount}</b></color> | ❌ Ditolak: <color=red><b>{rejectedRows}</b></color>");
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
