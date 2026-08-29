using UnityEngine;

public class ObjectiveTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        OnSceneLoaded,
        OnGameObjectEnabled,
        OnPlayerTriggerEnter,
        OnInteracted // Pilihan baru: Tekan tombol saat di dalam collider
    }

    [Header("Trigger Settings")]
    [SerializeField] private TriggerType triggerType = TriggerType.OnPlayerTriggerEnter;
    [Tooltip("Jika dicentang, trigger ini hanya akan berfungsi sekali saja selama game berjalan")]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Objective Data to Play")]
    [SerializeField] private ObjectiveData objectiveToPlay;

    [Header("Player & Input Settings")]
    [Tooltip("Tag dari Game Object Player kamu")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Tombol interaksi yang harus ditekan (Hanya untuk tipe OnInteracted)")]
    [SerializeField] private KeyCode interactionKey = KeyCode.S;

    private bool hasTriggered = false;
    private bool isPlayerInside = false; // Melacak apakah player sedang berada di dalam collider

    private void Start()
    {
        if (triggerType == TriggerType.OnSceneLoaded)
        {
            ExecuteObjectiveTrigger();
        }
    }

    private void OnEnable()
    {
        if (triggerType == TriggerType.OnGameObjectEnabled)
        {
            if (triggerOnlyOnce && hasTriggered) return;
            ExecuteObjectiveTrigger();
        }
    }

    private void Update()
    {
        // Deteksi input S hanya jika tipenya OnInteracted dan player berada di dalam collider
        if (triggerType == TriggerType.OnInteracted && isPlayerInside)
        {
            if (Input.GetKeyDown(interactionKey))
            {
                ExecuteObjectiveTrigger();
            }
        }
    }

    // --- DETEKSI COLLIDER 3D ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (triggerType == TriggerType.OnPlayerTriggerEnter)
            {
                ExecuteObjectiveTrigger();
            }
            else if (triggerType == TriggerType.OnInteracted)
            {
                isPlayerInside = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && triggerType == TriggerType.OnInteracted)
        {
            isPlayerInside = false;
        }
    }

    // --- DETEKSI COLLIDER 2D (Jika game kamu 2D) ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            if (triggerType == TriggerType.OnPlayerTriggerEnter)
            {
                ExecuteObjectiveTrigger();
            }
            else if (triggerType == TriggerType.OnInteracted)
            {
                isPlayerInside = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && triggerType == TriggerType.OnInteracted)
        {
            isPlayerInside = false;
        }
    }
    private void ExecuteObjectiveTrigger()
    {
        if (triggerOnlyOnce && hasTriggered) return;

        if (objectiveToPlay == null)
        {
            Debug.LogWarning($"[ObjectiveTrigger] {gameObject.name} terpicu tetapi tidak ada ObjectiveData!", gameObject);
            return;
        }

        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.PlayObjective(objectiveToPlay);
            hasTriggered = true;
            
            if (triggerOnlyOnce) isPlayerInside = false; 

            Debug.Log($"[ObjectiveTrigger] Berhasil mengirim objektif: '{objectiveToPlay.name}'", gameObject);
        }
    }


    public void ResetTrigger()
    {
        hasTriggered = false;
        isPlayerInside = false;
    }
}
