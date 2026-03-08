using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject loadMenuPanel; // Bisa dihubungkan dengan SaveLoadPanel dari SaveUIManager

    [Header("Main Menu Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button exitButton;

    [Header("Load Menu Buttons")]
    [SerializeField] private Button backToMainButton; // Tombol kembali dari panel Load ke Main Menu

    [Header("New Game Settings")]
    [Tooltip("Nama scene pertama yang akan dimuat saat Play ditekan.")]
    [SerializeField] private string newGameSceneName = "Level_01";

    private void Awake()
    {
        // Memastikan kondisi awal: Main Menu aktif, Load Menu nonaktif
        mainMenuPanel.SetActive(true);
        if (loadMenuPanel != null) loadMenuPanel.SetActive(false);

        // Mendaftarkan fungsi ke masing-masing tombol
        if (playButton != null) playButton.onClick.AddListener(StartNewGame);
        if (loadButton != null) loadButton.onClick.AddListener(OpenLoadMenu);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
        
        if (backToMainButton != null) backToMainButton.onClick.AddListener(CloseLoadMenu);
    }

    private void StartNewGame()
    {
        // Menonaktifkan tombol agar tidak ditekan dua kali (mencegah double-loading)
        playButton.interactable = false;

        // Menggunakan SceneController untuk memuat game baru jika sudah tersedia
        if (SceneController.Instance != null)
        {
            GameData newGameData = new GameData();
            newGameData.currentScene = newGameSceneName;
            SceneController.Instance.LoadSavedGame(newGameData); 
        }
        else
        {
            // Fallback jika SceneController belum di-setup
            SceneManager.LoadScene(newGameSceneName);
        }
    }

    private void OpenLoadMenu()
    {
        // Menyembunyikan panel Main Menu
        mainMenuPanel.SetActive(false);
        
        // Memunculkan panel Load. 
        // Terintegrasi dengan SaveUIManager yang sudah dibuat sebelumnya.
        if (SaveUIManager.Instance != null)
        {
            SaveUIManager.Instance.OpenLoadMenu();
        }
        else if (loadMenuPanel != null)
        {
            loadMenuPanel.SetActive(true);
        }
    }

    private void CloseLoadMenu()
    {
        // Menutup panel Load (melalui SaveUIManager agar rapi)
        if (SaveUIManager.Instance != null)
        {
            SaveUIManager.Instance.CloseMenu();
        }
        else if (loadMenuPanel != null)
        {
            loadMenuPanel.SetActive(false);
        }
        
        // Memunculkan kembali panel Main Menu
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }

    private void ExitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
    }
}