using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class NarrationTrigger : MonoBehaviour
{
    public enum TriggerMode
    {
        OnEnable,
        OnPlayerEnter,
        OnInteracted
    }

    [Header("Trigger Settings")]
    [Tooltip("Pilih bagaimana narasi ini akan dipicu.")]
    [SerializeField] private TriggerMode triggerMode = TriggerMode.OnPlayerEnter;
    
    [Tooltip("Jika true, narasi hanya akan muncul satu kali seumur hidup objek ini.")]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Narration Data")]
    [SerializeField] private NarrationData narrationData;

    [Header("Object Toggle After Interaction")]
    [SerializeField] private GameObject[] objectsToActivate;
    [SerializeField] private GameObject[] objectsToDeactivate;

    private bool hasTriggered = false;
    private bool isPlayerInside = false;
    private PlayerController activePlayer;

    private void Awake()
    {
        // Memastikan collider berfungsi sebagai trigger/sensor tanpa menghalangi fisik karakter
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (triggerMode == TriggerMode.OnEnable)
        {
            // Menggunakan Coroutine untuk memberikan jeda 1 frame agar Manager siap
            StartCoroutine(ExecuteWithDelay());
        }
    }

    private IEnumerator ExecuteWithDelay()
    {
        yield return null; // Tunggu 1 frame
        ExecuteNarration();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerMode == TriggerMode.OnPlayerEnter)
        {
            if (other.CompareTag("Player"))
            {
                ExecuteNarration(other.GetComponentInParent<PlayerController>());
            }
        }
        else if (triggerMode == TriggerMode.OnInteracted && other.CompareTag("Player"))
        {
            isPlayerInside = true;
            activePlayer = other.GetComponentInParent<PlayerController>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (triggerMode == TriggerMode.OnInteracted && other.CompareTag("Player"))
        {
            isPlayerInside = false;
            activePlayer = null;
        }
    }

    private void Update()
    {
        if (triggerMode == TriggerMode.OnInteracted && isPlayerInside && Input.GetKeyDown(KeyCode.S))
        {
            ExecuteNarration(activePlayer);
        }
    }

    private void ExecuteNarration(PlayerController player = null)
    {
        if (narrationData == null) return;
        if (triggerOnlyOnce && hasTriggered) return;

        // Validasi: Jika pemicunya adalah Player Enter, pastikan Elara sedang dalam state Play.
        // Jika pemicunya OnEnable, validasi ini dilewati karena objek mungkin di-enable saat cutscene lain sedang berjalan.
        if (triggerMode == TriggerMode.OnPlayerEnter || triggerMode == TriggerMode.OnInteracted)
        {
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play) 
                return;
        }

        hasTriggered = true;

        if (player != null && player.IsDizzy && IsPlayerInsideGlitch(player))
        {
            player.PlayNarrationWithSit(narrationData);
        }
        else
        {
            NarrationManager.Instance.PlayNarration(narrationData);
        }

        ToggleObjects();

        // Jika hanya boleh dipicu sekali, matikan komponen agar tidak membebani memori
        if (triggerOnlyOnce)
        {
            GetComponent<BoxCollider2D>().enabled = false;
            this.enabled = false;
        }
    }

    private void ToggleObjects()
    {
        if (objectsToActivate != null)
        {
            foreach (GameObject obj in objectsToActivate)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        if (objectsToDeactivate != null)
        {
            foreach (GameObject obj in objectsToDeactivate)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
    }

    private bool IsPlayerInsideGlitch(PlayerController player)
    {
        GlitchSprite[] glitchSprites = FindObjectsByType<GlitchSprite>(FindObjectsSortMode.None);

        foreach (GlitchSprite glitchSprite in glitchSprites)
        {
            if (glitchSprite.IsPlayerInside(player)) return true;
        }

        return false;
    }
}