using UnityEngine;
using UnityEngine.Rendering; // Diperlukan untuk komponen Volume
using UnityEngine.Rendering.Universal; // Diperlukan untuk DepthOfField di URP
using System.Collections.Generic; // Diperlukan untuk menggunakan List

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    public enum Chapter { Prologue, Denial, Anger, Bargaining, Depression, Acceptance, Epilogue }
    public enum GameState { Play, Pause, Cutscene }

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
            return; // Berhenti mengeksekusi kode di bawah jika objek dihancurkan
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Ambil semua efek blur saat scene pertama kali dimuat
        FindAllBlurVolumes();
    }

    private void Start()
    {
        // Pastikan saat game mulai, status blur disesuaikan dengan kondisi awal game state
        ApplyBlurState(currentState);
    }

    /// <summary>
    /// Mencari semua Volume di scene yang memiliki profile berisi efek DepthOfField (Blur)
    /// </summary>
    public void FindAllBlurVolumes()
    {
        activeBlurEffects.Clear();

        // Mencari semua komponen Volume yang aktif di dalam scene secara masif
        Volume[] allVolumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);

        foreach (Volume vol in allVolumes)
        {
            if (vol.profile != null && vol.profile.TryGet<DepthOfField>(out var dof))
            {
                activeBlurEffects.Add(dof);
            }
        }
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
        
        // Mengatur timeScale: Pause = 0, Play/Cutscene = 1
        Time.timeScale = (newState == GameState.Pause) ? 0 : 1;

        // Panggil fungsi untuk mengaktifkan/mematikan blur
        ApplyBlurState(newState);
    }

    /// <summary>
    /// Mengatur keaktifan efek blur pada semua kamera berdasarkan Game State saat ini
    /// </summary>
    private void ApplyBlurState(GameState state)
    {
        // Berjalan jika game dalam kondisi Cutscene ATAU Pause
        bool shouldBlur = (state == GameState.Cutscene || state == GameState.Pause);

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
