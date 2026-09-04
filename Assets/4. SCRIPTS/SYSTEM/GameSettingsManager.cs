using UnityEngine;
using UnityEngine.UI;

public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    private const string LANGUAGE_KEY = "SelectedLanguage";
    
    // true = English, false = Indonesia (bisa disesuaikan dengan toggle Anda)
    private bool isEnglish = false; 

    public bool IsEnglish => isEnglish;

    private void Awake()
    {
        // Standar Singleton Pattern agar object tidak duplikat saat pindah scene
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Berikan delay sedikit atau sinkronisasi di awal game
        ApplyLanguageToCurrentManager();
    }

    // Fungsi ini dipanggil secara otomatis setiap kali Scene Baru dimuat
    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Otomatis sinkronisasi bahasa saat masuk ke scene baru
        ApplyLanguageToCurrentManager();
    }

    /// <summary>
    /// Fungsi utama untuk mengubah bahasa dari Toggle UI
    /// </summary>
    public void SetLanguage(bool inputIsEnglish)
    {
        isEnglish = inputIsEnglish;
        
        // Simpan ke memory lokal (1 = English, 0 = Indonesia)
        PlayerPrefs.SetInt(LANGUAGE_KEY, isEnglish ? 1 : 0);
        PlayerPrefs.Save();

        ApplyLanguageToCurrentManager();
    }

    /// <summary>
    /// Menghubungkan data pengaturan global ke NarrationManager di scene aktif
    /// </summary>
    public void ApplyLanguageToCurrentManager()
    {
        // Integrasi dengan NarrationManager (Bawaan lama)
        if (NarrationManager.Instance != null)
        {
            NarrationManager.Instance.ToggleLanguage(isEnglish);
        }

        // INTEGRASI BARU: Integrasi dengan ObjectiveManager
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ToggleLanguage(isEnglish);
        }
    }

    private void LoadSettings()
    {
        // Default awal jika pemain baru pertama kali buka game adalah Indonesia (0)
        int savedLanguage = PlayerPrefs.GetInt(LANGUAGE_KEY, 0);
        isEnglish = (savedLanguage == 1);
    }
}
