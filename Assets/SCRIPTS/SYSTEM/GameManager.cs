using UnityEngine;
using UnityEngine.Rendering; // Diperlukan untuk komponen Volume
using UnityEngine.Rendering.Universal; // Diperlukan untuk DepthOfField di URP

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

    [Header("Post Processing / Blur Settings")]
    [Tooltip("Masukkan GameObject Global Volume atau Volume khusus Blur ke sini")]
    [SerializeField] private Volume postProcessVolume;
    
    // Variabel internal (Sudah diperbaiki tanpa spasi)
    private DepthOfField depthOfField;

private void Awake()
{
    // Singleton Setup
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
    }
    else
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // PERBAIKAN: Jika objek penampung memiliki lebih dari 1 Volume
    if (postProcessVolume != null)
    {
        // Mengambil semua komponen Volume yang menempel pada objek tersebut
        Volume[] allVolumes = postProcessVolume.GetComponents<Volume>();

        foreach (Volume vol in allVolumes)
        {
            // Mencari Volume mana yang memiliki profile berisi efek DepthOfField
            if (vol.profile != null && vol.profile.TryGet<DepthOfField>(out var dof))
            {
                depthOfField = dof;
                
                // Opsional: ganti referensi postProcessVolume utama ke Volume yang benar
                postProcessVolume = vol; 
                break; 
            }
        }
    }
}


    private void Start()
    {
        // Pastikan saat game mulai, status blur disesuaikan dengan kondisi awal game state
        ApplyBlurState(currentState);
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
        
        // Contoh logika sederhana: pause physics jika GameState = Pause
        Time.timeScale = (newState == GameState.Pause) ? 0 : 1;

        // Panggil fungsi untuk mengaktifkan/mematikan blur
        ApplyBlurState(newState);
    }

    /// <summary>
    /// Mengatur keaktifan efek blur berdasarkan Game State saat ini
    /// </summary>
    private void ApplyBlurState(GameState state)
    {
        if (depthOfField == null) return;

        // Mengaktifkan efek blur (Depth of Field) HANYA jika masuk ke state Cutscene
        if (state == GameState.Cutscene)
        {
            depthOfField.active = true;
        }
        else // Jika kembali ke state Play atau Pause, matikan efek blurnya
        {
            depthOfField.active = false;
        }
    }
}
