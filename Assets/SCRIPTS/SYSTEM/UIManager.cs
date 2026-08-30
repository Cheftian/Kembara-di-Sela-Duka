using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Pause Menu UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button backToMenuButton;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Key Objects Management")]
    [Tooltip("Masukkan semua GameObject yang bertindak sebagai Key (baik UI maupun Objek Dunia) ke sini.")]
    [SerializeField] private GameObject[] keyObjects;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (pausePanel != null) pausePanel.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void Update()
    {
        // Deteksi input tombol Escape untuk Pause/Resume
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SettingsMenuController.Instance != null && SettingsMenuController.Instance.IsSettingsOpen)
            {
                SettingsMenuController.Instance.CloseSettings();
                return;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Pause");
            }
            TogglePause();
        }
    }

    // Fungsi ini juga bisa dipanggil oleh tombol UI Pause (jika ada)
    public void TogglePause()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.currentState == GameManager.GameState.Play)
        {
            PauseGame();
        }
        else if (GameManager.Instance.currentState == GameManager.GameState.Pause)
        {
            ResumeGame();
        }
    }

    private void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        GameManager.Instance.SetGameState(GameManager.GameState.Pause);
    }

    private void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        GameManager.Instance.SetGameState(GameManager.GameState.Play);
    }

    private void ReturnToMainMenu()
    {
        // 1. Pastikan GameState kembali ke Play agar Time.timeScale normal sebelum pindah scene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Play);
        }

        // 2. Hubungkan ke SceneController lokal di scene saat ini untuk memicu animasi fade out
        if (SceneController.Instance != null)
        {
            Debug.Log($"[UIManager] Kembali ke Main Menu via SceneController. Target: {mainMenuSceneName}");
            SceneController.Instance.ChangeSceneByName(mainMenuSceneName);
        }
        else
        {
            // Pengaman cadangan jika SceneController tidak ditemukan di dalam scene gameplay saat ini
            Debug.LogWarning("[UIManager] SceneController.Instance tidak ditemukan! Melakukan load scene secara instan.");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }


    // --- MANAJEMEN DATA KUNCI (KEYS) ---

    // Dipanggil oleh SaveUIManager saat ExecuteSave
    public List<string> GetActiveKeys()
    {
        List<string> activeKeys = new List<string>();
        foreach (GameObject keyObj in keyObjects)
        {
            // Validasi apakah objek memiliki tag "Keys" dan sedang aktif
            if (keyObj != null && keyObj.CompareTag("Keys") && keyObj.activeInHierarchy)
            {
                activeKeys.Add(keyObj.name); 
            }
        }
        return activeKeys;
    }

    // Dipanggil oleh SceneController saat RestoreWorldState
    public void RestoreActiveKeys(List<string> savedKeys)
    {
        if (savedKeys == null) return;

        foreach (GameObject keyObj in keyObjects)
        {
            if (keyObj != null && keyObj.CompareTag("Keys"))
            {
                // Objek akan diaktifkan jika namanya ada di dalam data save
                bool shouldBeActive = savedKeys.Contains(keyObj.name);
                keyObj.SetActive(shouldBeActive);
            }
        }
    }
}