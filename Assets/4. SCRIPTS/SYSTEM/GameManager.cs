using UnityEngine;
using UnityEngine.Rendering; 
using UnityEngine.Rendering.Universal; 
using UnityEngine.SceneManagement; 
using System.Collections.Generic; 
using System;// Diperlukan untuk menggunakan Action

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum Chapter { Prologue, Denial, Anger, Bargaining, Depression, Acceptance, Epilogue }
    public enum GameState { Play, Pause, Cutscene, Interacted }

    [Header("Game Status")]
    public Chapter currentChapter = Chapter.Prologue;
    public GameState currentState = GameState.Play;

    [Header("Player Data")]
    public int memoriesCollected = 0;

    // EVENT BARU: Menyebarkan status game state baru ke script lain yang berlangganan
    public static event Action<GameState> OnGameStateChanged;

    private List<DepthOfField> activeBlurEffects = new List<DepthOfField>();

    private void Awake()
    {
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Mendeteksi scene baru dimuat: {scene.name}. Meriset efek blur...");

        FindAllBlurVolumes();

        currentState = GameState.Play;
        Time.timeScale = 1f;

        ApplyBlurState(GameState.Play);
        
        // Pemicu event saat scene baru selesai dimuat agar semua UI tersinkronisasi ke mode Play
        OnGameStateChanged?.Invoke(GameState.Play);
    }

    public void FindAllBlurVolumes()
    {
        activeBlurEffects.Clear();
        Volume[] allVolumes = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);


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
        Time.timeScale = (newState == GameState.Pause) ? 0 : 1;

        if (newState == GameState.Cutscene)
        {
            PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();

            if (player != null)
            {
                player.ResetToIdleState();
                Animator playerAnimator = player.GetComponentInChildren<Animator>();
                if (playerAnimator != null)
                {
                    playerAnimator.Play("Idle", 0, 0f);
                }
            }
        }

        ApplyBlurState(newState);

        // PICU EVENT: Beritahu semua objek (termasuk panel objectives) bahwa state telah berubah
        OnGameStateChanged?.Invoke(newState);
    }

    private void ApplyBlurState(GameState state)
    {
        bool shouldBlur = (state == GameState.Cutscene || state == GameState.Interacted || state == GameState.Pause);

        foreach (DepthOfField dof in activeBlurEffects)
        {
            if (dof != null)
            {
                dof.active = shouldBlur;
            }
        }
    }
}
