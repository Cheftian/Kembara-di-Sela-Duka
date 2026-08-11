using System.Collections;
using UnityEngine;

public class GlitchSprite : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    [Header("Sprite Settings")]
    [SerializeField] private Sprite[] glitchSprites;
    
    [Header("Timing Settings")]
    [SerializeField] private float minNormalTime = 1.0f;
    [SerializeField] private float maxNormalTime = 3.0f;
    [SerializeField] private float minGlitchDuration = 0.1f;
    [SerializeField] private float maxGlitchDuration = 0.4f;
    [SerializeField] private float glitchFrameRate = 0.05f;

    private Sprite originalSprite;
    private Coroutine glitchCoroutine;
    private bool isPlayerInside = false;
    private PlayerController activePlayer; 

    private void Start()
    {
        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetSpriteRenderer != null)
        {
            originalSprite = targetSpriteRenderer.sprite;
        }

        if (glitchSprites == null || glitchSprites.Length == 0)
        {
            Debug.LogError("Glitch Sprites belum dimasukkan ke dalam Inspector!", this);
        }
    }

    private void Update()
    {
        // Jika player mematikan dizzy-nya sendiri (berbalik arah ke kanan), hentikan efek visual objek
        if (isPlayerInside && activePlayer != null && !activePlayer.IsDizzy)
        {
            StopGlitchEffect();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isPlayerInside)
        {
            isPlayerInside = true;
            activePlayer = collision.GetComponent<PlayerController>();
        }
    }

    // KUNCI UTAMA: Mengecek kondisi input player secara realtime selama di dalam area
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && activePlayer != null)
        {
            // Jika player sedang tidak dizzy (karena habis balik kanan) tapi SEKARANG menekan tombol A (Kiri)
            if (!activePlayer.IsDizzy && Input.GetKey(KeyCode.A))
            {
                // Aktifkan kembali efek dizzy seketika!
                activePlayer.SetDizzyStatus(true);

                // Mulai ulang efek glitch pada sprite objek utama jika belum berjalan
                if (glitchCoroutine == null && targetSpriteRenderer != null)
                {
                    glitchCoroutine = StartCoroutine(GlitchRoutine());
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (activePlayer != null)
            {
                // 1. Matikan status pusing jika dia keluar area saat masih berjalan pusing ke kiri
                if (activePlayer.IsDizzy)
                {
                    activePlayer.SetDizzyStatus(false);
                }

                // 2. BARU: Paksa hapus memori pusing kiri agar saat berjalan ke kiri di luar area tidak pusing lagi
                activePlayer.ClearDizzyMemory(); 
            }

            StopGlitchEffect();
            isPlayerInside = false;
            activePlayer = null;
        }
    }

    private void StopGlitchEffect()
    {
        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
            glitchCoroutine = null;
        }
        
        if (targetSpriteRenderer != null)
        {
            targetSpriteRenderer.sprite = originalSprite;
        }
    }

    private IEnumerator GlitchRoutine()
    {
        while (isPlayerInside && targetSpriteRenderer != null && activePlayer != null && activePlayer.IsDizzy)
        {
            float normalDuration = Random.Range(minNormalTime, maxNormalTime);
            yield return new WaitForSeconds(normalDuration);

            if (!isPlayerInside || !activePlayer.IsDizzy) break;

            float glitchDuration = Random.Range(minGlitchDuration, maxGlitchDuration);
            float elapsedTime = 0f;

            while (elapsedTime < glitchDuration && isPlayerInside && activePlayer.IsDizzy)
            {
                int randomIndex = Random.Range(0, glitchSprites.Length);
                targetSpriteRenderer.sprite = glitchSprites[randomIndex];

                yield return new WaitForSeconds(glitchFrameRate);
                elapsedTime += glitchFrameRate;
            }

            targetSpriteRenderer.sprite = originalSprite;
        }
    }
}
