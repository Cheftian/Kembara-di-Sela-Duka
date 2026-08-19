using UnityEngine;
using UnityEngine.Rendering; // Diperlukan untuk komponen Volume
using UnityEngine.Rendering.Universal; // Diperlukan untuk DepthOfField di URP
using UnityEngine.SceneManagement; // Diperlukan untuk mendeteksi pergantian scene
using System.Collections.Generic; // Diperlukan untuk menggunakan List

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    public enum Chapter { Prologue, Denial, Anger, Bargaining, Depression, Acceptance, Epilogue }
    public enum GameState { Play, Pause, Cutscene, Interacted }

    [Header("Game Status")]
    public Chapter currentChapter = Chapter.Prologue;
    public GameState currentState = GameState.Play;

    [Header("Player Data")]
    public int memoriesCollected = 0;

    // Menggunakan List untuk menyimpan semua efek DepthOfField yang ditemukan di scene
    private List<DepthOfField> activeBlurEffects = new List<DepthOfField>();

    private void Awake()
    {
        // Singleton Setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return; 
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Daftarkan listener sceneLoaded saat objek aktif
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Cabut listener sceneLoaded saat objek nonaktif/hancur untuk mencegah memory leak
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // KUNCI UTAMA: Otomatis berjalan SETIAP KALI SCENE SELESAI DIMUAT
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Mendeteksi scene baru dimuat: {scene.name}. Meriset efek blur...");

        // 1. Bersihkan referensi efek blur dari scene lama yang sudah hancur
        FindAllBlurVolumes();

        // 2. Paksa status permainan kembali ke PLAY saat pergantian scene selesai
        currentState = GameState.Play;
        Time.timeScale = 1f;

        // 3. Paksa SEMUA efek blur di scene baru untuk mati total (false)
        ApplyBlurState(GameState.Play);
    }

    /// <summary>
    /// Mencari semua Volume di scene yang memiliki profile berisi efek DepthOfField (Blur)
    /// </summary>
    public void FindAllBlurVolumes()
    {
        activeBlurEffects.Clear();

        // Mencari semua komponen Volume yang aktif di dalam scene baru
        Volume[] allVolumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);

        foreach (Volume vol in allVolumes)
        {
            if (vol.profile != null && vol.profile.TryGet<DepthOfField>(out var dof))
            {
                activeBlurEffects.Add(dof);
            }
        }
        Debug.Log($"[GameManager] Berhasil mendeteksi {activeBlurEffects.Count} efek blur (Depth of Field) di scene ini.");
    }

    public void ChangeChapter(Chapter newChapter)
    {
        if (currentChapter == newChapter) return;

        currentChapter = newChapter;
        Debug.Log($"Chapter Changed to: {currentChapter}");
    }

    public void AddMemory()
    {
        memoriesCollected++;
        Debug.Log($"Memories Collected: {memoriesCollected}");
    }

    public void SetGameState(GameState newState)
    {
        currentState = newState;
        
        // Mengatur timeScale: Pause = 0, state lainnya = 1
        Time.timeScale = (newState == GameState.Pause) ? 0 : 1;

        if (newState == GameState.Cutscene)
        {
            // Mencari komponen PlayerController yang aktif di scene saat ini
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                // Memanggil fungsi reset bawaan Anda untuk mematikan IsWalking, IsRunning, dll.
                player.ResetToIdleState();
                
                // Paksa memutar animasi Idle default agar visualnya langsung berubah seketika
                Animator playerAnimator = player.GetComponentInChildren<Animator>();
                if (playerAnimator != null)
                {
                    playerAnimator.Play("Idle", 0, 0f);
                }
            }
        }

        // Panggil fungsi untuk mengaktifkan/mematikan blur
        ApplyBlurState(newState);
    }

    /// <summary>
    /// Mengatur keaktifan efek blur pada semua kamera berdasarkan Game State saat ini
    /// </summary>
    private void ApplyBlurState(GameState state)
    {
        // Blur aktif saat game dalam kondisi Cutscene, Interacted, atau Pause
        bool shouldBlur = (state == GameState.Cutscene || state == GameState.Interacted || state == GameState.Pause);

        // Lakukan perulangan untuk mengaktifkan/mematikan efek pada semua volume yang terdaftar
        foreach (DepthOfField dof in activeBlurEffects)
        {
            if (dof != null)
            {
                dof.active = shouldBlur;
            }
        }
    }
}
