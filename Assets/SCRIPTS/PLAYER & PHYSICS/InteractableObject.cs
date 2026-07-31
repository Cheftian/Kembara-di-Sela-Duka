using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class InteractableObject : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private bool isSingleUse = false;
    [SerializeField] private float interactionRadius = 2.5f;
    
    private bool hasInteracted = false;
    private bool isPlayerInRange = false;

    [Header("Lock Settings")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private NarrationData lockedNarrationData;
    [Tooltip("All assigned objects must be active in the hierarchy to unlock this interaction.")]
    [SerializeField] private GameObject[] requiredKeyObjects;

    [Header("Triggers")]
    [SerializeField] private bool isMinigameTrigger = false;
    [SerializeField] private bool isSingleMinigame = false;
    [SerializeField] private GameObject minigameObject;
    [Space]
    [SerializeField] private bool isNarrativeTrigger = false;
    [SerializeField] private NarrationData narrationData;

    [Header("Object Toggling")]
    [SerializeField] private GameObject[] objectsToEnable;
    [SerializeField] private GameObject[] objectsToDisable;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(interactionRadius * 2, interactionRadius);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play)
            return;

        if (isPlayerInRange && !hasInteracted)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                StartCoroutine(InteractionSequence());
            }
        }
    }

    private bool CheckUnlockCondition()
    {
        if (requiredKeyObjects == null || requiredKeyObjects.Length == 0) return false;

        foreach (GameObject keyObj in requiredKeyObjects)
        {
            if (keyObj == null || !keyObj.activeInHierarchy)
            {
                return false; 
            }
        }
        return true; 
    }

    private IEnumerator InteractionSequence()
    {
        GameManager.Instance.SetGameState(GameManager.GameState.Cutscene);

        // Cari PlayerController di dalam scene
        PlayerController player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();

        // Lock validation process
        if (isLocked)
        {
            if (CheckUnlockCondition())
            {
                isLocked = false; 
            }
            else
            {
                if (lockedNarrationData != null)
                {
                    NarrationManager.Instance.PlayNarration(lockedNarrationData);
                }
                else
                {
                    GameManager.Instance.SetGameState(GameManager.GameState.Play);
                }
                yield break; 
            }
        }

        // Execute Minigame (Hanya jalankan sekuens SIT jika kondisi objek adalah minigame trigger)
        if (isMinigameTrigger && minigameObject != null)
        {
            if (player != null)
            {
                // 1. Jalankan animasi Sit dan tunggu sampai klip selesai
                yield return StartCoroutine(player.PlayAnimationAndWait("Sit"));
            }

            BaseMinigame minigameScript = minigameObject.GetComponent<BaseMinigame>();
            
            if (minigameScript != null)
            {
                minigameScript.SetupMinigame(this);
            }
            else
            {
                Debug.LogWarning($"Object {minigameObject.name} tidak memiliki script turunan BaseMinigame!");
            }
            
            // 2. Tampilkan panel minigame setelah animasi Sit selesai
            minigameObject.SetActive(true);
            yield break; 
        }

        // Execute Narration (Untuk trigger narasi biasa, tidak menjalankan sekuens duduk)
        if (isNarrativeTrigger && narrationData != null)
        {
            NarrationManager.Instance.PlayNarration(narrationData);
        }

        if (isSingleUse && !isMinigameTrigger)
        {
            if (!isNarrativeTrigger) GameManager.Instance.SetGameState(GameManager.GameState.Play);
            gameObject.SetActive(false);
            yield break;
        }

        if (!isNarrativeTrigger && !isMinigameTrigger) 
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Play);
        }
    }

    public void CompleteMinigame(bool puzzleIsSolved)
    {
        StartCoroutine(CompleteMinigameSequence(puzzleIsSolved));
    }

    private IEnumerator CompleteMinigameSequence(bool puzzleIsSolved)
{
    // Jika puzzle berhasil diselesaikan, eksekusi pertukaran objek
    if (puzzleIsSolved)
    {
        foreach (GameObject obj in objectsToEnable) if (obj != null) obj.SetActive(true);
        foreach (GameObject obj in objectsToDisable) if (obj != null) obj.SetActive(false);
    }

    // Pastikan panel minigame sudah nonaktif total di layar sebelum animasi berdiri dimulai
    if (minigameObject != null)
    {
        minigameObject.SetActive(false);
    }

    PlayerController player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
    
    if (player != null)
    {
        yield return StartCoroutine(player.PlayAnimationAndWait("Stand"));
        
        // Kembalikan parameter animator ke setelan Idle dasar
        player.ResetToIdleState();
    }

    // Ubah Game State kembali ke bermain normal SETELAH seluruh sekuens berdiri selesai total
    GameManager.Instance.SetGameState(GameManager.GameState.Play);
    
    // Jika minigame selesai dan disetel sekali pakai, matikan objek interaksi ini
    if (isSingleMinigame && puzzleIsSolved)
    {
        gameObject.SetActive(false);
    }
}

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) isPlayerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) isPlayerInRange = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
