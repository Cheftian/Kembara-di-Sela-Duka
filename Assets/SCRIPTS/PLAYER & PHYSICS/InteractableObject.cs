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

    [Header("Custom Interaction Animations")]
    [Tooltip("Nama animasi yang diputar SAAT interaksi dimulai (misal: Sit, Inspect, Bow)")]
    [SerializeField] private string startAnimationName = "";
    [Tooltip("Nama animasi yang diputar SETELAH interaksi selesai (misal: Stand, Idle)")]
    [SerializeField] private string endAnimationName = "";

    private NotificationTrigger notificationTrigger;

    private void Start()
    {
        notificationTrigger = GetComponent<NotificationTrigger>();
    }

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
                hasInteracted = true; 
                HideAndDisableNotification();
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
        PlayerController player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();

        if (isLocked)
        {
            // Periksa apakah syarat kunci (item di hirarki) sudah terpenuhi
            if (CheckUnlockCondition())
            {
                isLocked = false; // Kunci terbuka! Sequence langsung lanjut ke bawah otomatis
            }
            else
            {
                // JIKA MASIH TERKUNCI: Mainkan narasi terkunci
                if (lockedNarrationData != null)
                {
                    NarrationManager.Instance.PlayNarration(lockedNarrationData);
                    
                    yield return null; // Tunggu 1 frame agar panel narasi aktif

                    // Tunggu hingga panel narasi terkunci benar-benar ditutup oleh Player
                    while (NarrationManager.Instance.IsNarrating)
                    {
                        yield return null;
                    }
                }
                
                // Kembalikan game state ke mode bermain normal setelah narasi selesai
                GameManager.Instance.SetGameState(GameManager.GameState.Play);

                // BARU & UTAMA: Reset status interaksi dan munculkan kembali notifikasi
                // Ini membuat Player bisa menekan S lagi di objek ini meskipun statusnya masih isLocked
                hasInteracted = false; 
                ShowAndEnableNotification();
                
                yield break; // Hentikan di sini (jangan lanjut ke minigame/narasi utama)
            }
        }


        yield return StartCoroutine(PlayInteractionAnimation(player));

        if (isMinigameTrigger && minigameObject != null)
        {
            BaseMinigame minigameScript = minigameObject.GetComponent<BaseMinigame>();
            if (minigameScript != null) minigameScript.SetupMinigame(this);
            minigameObject.SetActive(true);
        }

        if (isNarrativeTrigger && narrationData != null)
        {
            // DIUBAH: Baris ExecuteObjectToggling() di sini dihapus agar tidak aktif di awal
            
            // Mainkan narasi melalui manajer
            NarrationManager.Instance.PlayNarration(narrationData);
            yield return null;

            // Tunggu sampai panel narasi benar-benar ditutup penuh oleh Player
            while (NarrationManager.Instance.IsNarrating)
            {
                yield return null;
            }

            // BARU: Eksekusi Object Toggling TEPAT SETELAH narasi selesai ditutup
            if (!isMinigameTrigger)
            {
                ExecuteObjectToggling();
                
                yield return StartCoroutine(PlayEndInteractionAnimation(player));
                GameManager.Instance.SetGameState(GameManager.GameState.Play);

                if (!isSingleUse) 
                {
                    hasInteracted = false; 
                    ShowAndEnableNotification();
                }
            }
        }
        
        // 4. JALANKAN INTERAKSI BIASA (Jika tidak mencentang minigame maupun narasi)
        if (!isNarrativeTrigger && !isMinigameTrigger) 
        {
            ExecuteObjectToggling();
            yield return StartCoroutine(PlayEndInteractionAnimation(player));
            GameManager.Instance.SetGameState(GameManager.GameState.Play);
            
            if (!isSingleUse) 
            {
                hasInteracted = false; 
                ShowAndEnableNotification();
            }
        }

        // Logika Sekali Pakai untuk non-minigame
        if (isSingleUse && !isMinigameTrigger)
        {
            if (!isNarrativeTrigger) GameManager.Instance.SetGameState(GameManager.GameState.Play);
            gameObject.SetActive(false);
            yield break;
        }
    }


    public void CompleteMinigame(bool puzzleIsSolved)
    {
        StartCoroutine(CompleteMinigameSequence(puzzleIsSolved));
    }

    private IEnumerator CompleteMinigameSequence(bool puzzleIsSolved)
    {
        if (puzzleIsSolved) ExecuteObjectToggling();
        if (minigameObject != null) minigameObject.SetActive(false);

        PlayerController player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
        yield return StartCoroutine(PlayEndInteractionAnimation(player));

        GameManager.Instance.SetGameState(GameManager.GameState.Play);
        
        if (isSingleMinigame && puzzleIsSolved)
        {
            gameObject.SetActive(false);
        }
        else
        {
            // BARU: Buka kembali kunci input agar player bisa menekan S lagi nanti
            hasInteracted = false; 
            ShowAndEnableNotification();
        }
    }

    private IEnumerator PlayInteractionAnimation(PlayerController player)
    {
        if (player != null && !string.IsNullOrEmpty(startAnimationName))
        {
            yield return StartCoroutine(player.PlayAnimationAndWait(startAnimationName));
        }
    }

    private IEnumerator PlayEndInteractionAnimation(PlayerController player)
    {
        if (player != null && !string.IsNullOrEmpty(endAnimationName))
        {
            yield return StartCoroutine(player.PlayAnimationAndWait(endAnimationName));
            player.ResetToIdleState();
        }
    }

    private void ExecuteObjectToggling()
    {
        if (objectsToEnable != null)
        {
            foreach (GameObject obj in objectsToEnable) if (obj != null) obj.SetActive(true);
        }
        
        if (objectsToDisable != null)
        {
            foreach (GameObject obj in objectsToDisable) if (obj != null) obj.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) isPlayerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (!isSingleUse)
            {
                hasInteracted = false; 
            }
            HideAndDisableNotification(); 
        }
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
    private void HideAndDisableNotification()
    {
        if (notificationTrigger != null)
        {
            if (notificationTrigger.notification != null)
            {
                notificationTrigger.notification.Hide();
            }
            notificationTrigger.enabled = false;
        }
    }

    private void ShowAndEnableNotification()
    {
        if (notificationTrigger != null)
        {
            notificationTrigger.enabled = true;
            if (isPlayerInRange && notificationTrigger.notification != null)
            {
                notificationTrigger.notification.Show();
            }
        }
    }

}
