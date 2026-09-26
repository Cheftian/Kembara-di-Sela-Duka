using UnityEngine;

public class GuideTrigger : MonoBehaviour
{
    public enum TriggerType { OnTriggerEnter, OnEnable }

    [Header("Trigger Settings")]
    [Tooltip("Pilih kapan guide ini akan mulai dipicu")]
    [SerializeField] private TriggerType activationType = TriggerType.OnTriggerEnter;

    [Tooltip("Masukkan ID Guide yang sesuai dengan daftar di GuideManager")]
    [SerializeField] private string guideIDToTrigger;
    
    [Tooltip("Centang jika guide ini hanya boleh muncul sekali sepanjang game")]
    [SerializeField] private bool triggerOnlyOnce = true;
    
    [Header("Collision Settings (Hanya untuk OnTriggerEnter)")]
    [Tooltip("Tag dari objek player")]
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered = false;
    private bool isWaitingForPlayState = false;

    private void OnEnable()
    {
        // Berlangganan ke event perubahan state di GameManager
        GameManager.OnGameStateChanged += HandleGameStateChanged;

        if (activationType == TriggerType.OnEnable)
        {
            AttemptTrigger();
        }
    }

    private void OnDisable()
    {
        // Batalkan langganan event saat objek dinonaktifkan untuk mencegah memory leak
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activationType == TriggerType.OnTriggerEnter && other.CompareTag(playerTag))
        {
            AttemptTrigger();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activationType == TriggerType.OnTriggerEnter && other.CompareTag(playerTag))
        {
            AttemptTrigger();
        }
    }

    private void AttemptTrigger()
    {
        if (triggerOnlyOnce && hasTriggered) return;

        // Cek state saat ini di GameManager
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Play)
        {
            // Jika state sudah 'Play', langsung eksekusi guide
            ProcessTrigger();
        }
        else
        {
            // Jika state BUKAN 'Play' (misal sedang Cutscene/Pause), masuk mode menunggu
            isWaitingForPlayState = true;
            Debug.Log($"[GuideTrigger] GameState sedang tidak Play. Guide '{guideIDToTrigger}' ditunda sampai state kembali Play.");
        }
    }

    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        // Jika sedang dalam antrean menunggu dan GameState berubah menjadi Play
        if (isWaitingForPlayState && newState == GameManager.GameState.Play)
        {
            isWaitingForPlayState = false; // Reset status menunggu
            ProcessTrigger();
        }
    }

    private void ProcessTrigger()
    {
        if (triggerOnlyOnce && hasTriggered) return;

        if (GuideManager.Instance != null)
        {
            GuideManager.Instance.ShowGuide(guideIDToTrigger);
            hasTriggered = true;

            if (triggerOnlyOnce)
            {
                this.enabled = false;
            }
        }
        else
        {
            Debug.LogWarning($"GuideManager Instance tidak ditemukan saat memicu ID '{guideIDToTrigger}'!");
        }
    }
}
