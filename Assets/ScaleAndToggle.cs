using System.Collections;
using UnityEngine;

public class ScaleAndToggle : MonoBehaviour
{
    [Header("Idle Pulse Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionScaleMultiplier = 1.5f;
    [SerializeField] private float duration = 2f;

    [Header("Toggle Settings")]
    [SerializeField] private GameObject[] objectsToToggle;

    private Vector3 originalScale;
    private bool isInteracting = false;
    private bool hasInteractedBefore = false;

    void Start()
    {
        // Menyimpan skala awal objek
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Cek interaksi tombol S
        if (Input.GetKeyDown(KeyCode.S) && !isInteracting)
        {
            StartCoroutine(InteractRoutine());
        }

        // Jalankan efek membesar/mengecil otomatis jika tidak sedang interaksi
        if (!isInteracting)
        {
            HandleIdlePulse();
        }
    }

    // Fungsi untuk membuat objek membesar/mengecil otomatis (Idle)
    private void HandleIdlePulse()
    {
        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = originalScale + new Vector3(scaleOffset, scaleOffset, 0);
    }

    // Coroutine untuk menangani efek interaksi tombol S
    private IEnumerator InteractRoutine()
    {
        isInteracting = true;

        // Picu object toggling HANYA pada interaksi pertama kali
        if (!hasInteractedBefore)
        {
            ToggleArrayObjects();
            hasInteractedBefore = true;
        }

        // Mengubah skala menjadi lebih besar sebesar multiplier (x)
        Vector3 targetScale = originalScale * interactionScaleMultiplier;
        transform.localScale = targetScale;

        // Tunggu selama durasi yang ditentukan
        yield return new WaitForSeconds(duration);

        // Mengembalikan skala ke normal secara instan sebelum kembali ke mode idle
        transform.localScale = originalScale;
        
        isInteracting = false;
    }

    // Fungsi untuk mengaktifkan/menonaktifkan objek di dalam array
    private void ToggleArrayObjects()
    {
        if (objectsToToggle == null || objectsToToggle.Length == 0) return;

        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
            {
                // Jika aktif menjadi nonaktif, jika nonaktif menjadi aktif
                obj.SetActive(!obj.activeSelf);
            }
        }
    }
}
