using UnityEngine;

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
    }
}