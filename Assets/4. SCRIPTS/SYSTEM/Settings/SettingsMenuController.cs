using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    public static SettingsMenuController Instance { get; private set; }

    [Header("UI Panels & Buttons")]
    [Tooltip("Tarik GameObject Panel Settings Anda ke sini.")]
    [SerializeField] private GameObject settingsPanel;

    [Tooltip("Tarik Tombol Settings Utama Anda ke sini.")]
    [SerializeField] private Button settingsButton;

    [Tooltip("Tarik Tombol Close/Back di dalam Panel Settings ke sini (Opsional).")]
    [SerializeField] private Button closeButton;

    [Header("Main Menu Buttons to Hide")]
    [Tooltip("Masukkan semua tombol menu utama yang ingin disembunyikan saat panel settings terbuka (misal: Tombol Play, Exit, dll).")]
    [SerializeField] private GameObject[] menuButtonsToToggle;

    private bool isSettingsOpen = false;

    public bool IsSettingsOpen => isSettingsOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Pastikan status awal panel settings adalah mati saat game dimulai
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // Daftarkan fungsi klik ke tombol-tombol terkait
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(ToggleSettings);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ToggleSettings);
        }
    }

    public void ToggleSettings()
    {
        SetSettingsState(!isSettingsOpen);
    }

    public void CloseSettings()
    {
        SetSettingsState(false);
    }

    private void SetSettingsState(bool open)
    {
        isSettingsOpen = open;

        // 1. Atur keaktifan Panel Settings
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(isSettingsOpen);
        }

        // 2. Sembunyikan atau munculkan kembali tombol menu utama lainnya
        // Jika settings terbuka, tombol lain dimatikan (false). Jika settings ditutup, tombol lain dinyalakan (true).
        bool shouldShowMenuButtons = !isSettingsOpen;

        foreach (GameObject buttonObj in menuButtonsToToggle)
        {
            if (buttonObj != null)
            {
                buttonObj.SetActive(shouldShowMenuButtons);
            }
        }

        Debug.Log($"[SettingsMenu] Status Panel Settings Terbuka: {isSettingsOpen}");
    }
}
