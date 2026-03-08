using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class NarrationTrigger : MonoBehaviour
{
    public enum TriggerMode
    {
        OnEnable,
        OnPlayerEnter
    }

    [Header("Trigger Settings")]
    [Tooltip("Pilih bagaimana narasi ini akan dipicu.")]
    [SerializeField] private TriggerMode triggerMode = TriggerMode.OnPlayerEnter;
    
    [Tooltip("Jika true, narasi hanya akan muncul satu kali seumur hidup objek ini.")]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Narration Data")]
    [SerializeField] private NarrationData narrationData;

    private bool hasTriggered = false;

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
            ExecuteNarration();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerMode == TriggerMode.OnPlayerEnter)
        {
            if (other.CompareTag("Player"))
            {
                ExecuteNarration();
            }
        }
    }

    private void ExecuteNarration()
    {
        if (narrationData == null) return;
        if (triggerOnlyOnce && hasTriggered) return;

        // Validasi: Jika pemicunya adalah Player Enter, pastikan Elara sedang dalam state Play.
        // Jika pemicunya OnEnable, validasi ini dilewati karena objek mungkin di-enable saat cutscene lain sedang berjalan.
        if (triggerMode == TriggerMode.OnPlayerEnter)
        {
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Play) 
                return;
        }

        hasTriggered = true;
        NarrationManager.Instance.PlayNarration(narrationData);

        // Jika hanya boleh dipicu sekali, matikan komponen agar tidak membebani memori
        if (triggerOnlyOnce)
        {
            GetComponent<BoxCollider2D>().enabled = false;
            this.enabled = false;
        }
    }
}