using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    private void Awake()
    {
        // Memastikan hanya ada satu SceneController yang bertahan antar scene
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Dipanggil oleh SaveUIManager saat memilih Load Game
    public void LoadSavedGame(GameData data)
    {
        if (data == null || string.IsNullOrEmpty(data.currentScene))
        {
            Debug.LogError("Data save kosong atau tidak memiliki nama scene tujuan!");
            return;
        }

        StartCoroutine(LoadSceneAndRestoreData(data));
    }

    private IEnumerator LoadSceneAndRestoreData(GameData data)
    {
        // Opsional: Di sini Tian bisa memunculkan UI Loading Screen

        // 1. Mulai memuat scene secara asinkron
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(data.currentScene);
        
        // Menahan eksekusi kode hingga scene selesai dimuat sepenuhnya
        yield return new WaitUntil(() => asyncLoad.isDone);

        // 2. Menerapkan data ke scene yang baru
        RestoreWorldState(data);

        // Opsional: Di sini Tian bisa menyembunyikan UI Loading Screen
    }

    private void RestoreWorldState(GameData data)
    {
        // 1. Memulihkan Data GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentChapter = data.savedChapter;
            GameManager.Instance.memoriesCollected = data.savedMemories;
            
            // Logika opsional: Memanggil fungsi ChangeChapter jika diperlukan event khusus saat load
            // GameManager.Instance.ChangeChapter(data.savedChapter);
        }

        // 2. Memposisikan Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = data.playerPosition;
        }

        // 3. Memulihkan status aktif/nonaktif InteractableObject
        InteractableObject[] allInteractables = FindObjectsByType<InteractableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (InteractableObject obj in allInteractables)
        {
            UniqueID uid = obj.GetComponent<UniqueID>();
            if (uid != null && !string.IsNullOrEmpty(uid.ID))
            {
                int savedIndex = data.savedObjects.FindIndex(x => x.objectID == uid.ID);
                
                if (savedIndex != -1)
                {
                    obj.gameObject.SetActive(data.savedObjects[savedIndex].isActive);
                }
            }
        }

        if (UIManager.Instance != null && data.collectedKeys != null)
        {
            UIManager.Instance.RestoreActiveKeys(data.collectedKeys);
        }

        // 4. Memulihkan state game menjadi Play
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Play);
        }

        Debug.Log($"Data permainan berhasil dipulihkan. Chapter saat ini: {GameManager.Instance?.currentChapter}");
    }
}