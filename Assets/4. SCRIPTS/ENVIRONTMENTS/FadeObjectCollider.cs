using UnityEngine;

public class FadeSpriteCollider2D : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Sprite Renderer dari objek yang ingin di-fade. Kosongkan jika berada di GameObject yang sama.")]
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    [Header("Fade Settings")]
    [Tooltip("Target opasitas maksimal saat player di dalam (0 sampai 1).")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 1f;
    [Tooltip("Kecepatan transisi fade.")]
    [SerializeField] private float fadeSpeed = 2f;

    private bool isPlayerInside = false;
    private float currentAlpha = 0f;

    void Start()
    {
        // Jika targetSpriteRenderer tidak diisi di Inspector, ambil dari GameObject ini
        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetSpriteRenderer != null)
        {
            // Set alpha awal menjadi 0 saat permainan dimulai
            SetAlpha(0f);
        }
        else
        {
            Debug.LogError("Komponen SpriteRenderer tidak ditemukan pada objek ini!", this);
        }
    }

    void Update()
    {
        if (targetSpriteRenderer == null) return;

        // Tentukan target alpha berdasarkan keberadaan player
        float targetAlpha = isPlayerInside ? maxAlpha : 0f;

        // Jika nilai alpha belum mencapai target, lakukan transisi secara perlahan
        if (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            SetAlpha(currentAlpha);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }

    private void SetAlpha(float alpha)
    {
        currentAlpha = alpha;
        Color spriteColor = targetSpriteRenderer.color;
        spriteColor.a = currentAlpha;
        targetSpriteRenderer.color = spriteColor;
    }
}
