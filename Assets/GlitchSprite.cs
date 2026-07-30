using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GlitchSprite : MonoBehaviour
{
    [Header("Sprite Settings")]
    [SerializeField] private Sprite[] glitchSprites;
    
    [Header("Timing Settings")]
    [SerializeField] private float minNormalTime = 1.0f;
    [SerializeField] private float maxNormalTime = 3.0f;
    [SerializeField] private float minGlitchDuration = 0.1f;
    [SerializeField] private float maxGlitchDuration = 0.4f;
    [SerializeField] private float glitchFrameRate = 0.05f;

    private SpriteRenderer spriteRenderer;
    private Sprite originalSprite;
    private Coroutine glitchCoroutine;
    private bool isPlayerInside = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSprite = spriteRenderer.sprite;

        if (glitchSprites == null || glitchSprites.Length == 0)
        {
            Debug.LogError("Glitch Sprites belum dimasukkan ke dalam Inspector!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isPlayerInside)
        {
            isPlayerInside = true;
            
            // Tambahan: Beritahu player untuk aktifkan animasi dizzy
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                player.SetDizzyStatus(true);
            }

            glitchCoroutine = StartCoroutine(GlitchRoutine());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;
            
            // Tambahan: Kembalikan animasi walk player menjadi normal
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                player.SetDizzyStatus(false);
            }

            if (glitchCoroutine != null)
            {
                StopCoroutine(glitchCoroutine);
            }
            
            spriteRenderer.sprite = originalSprite;
        }
    }

    private IEnumerator GlitchRoutine()
    {
        while (isPlayerInside)
        {
            float normalDuration = Random.Range(minNormalTime, maxNormalTime);
            yield return new WaitForSeconds(normalDuration);

            if (!isPlayerInside) break;

            float glitchDuration = Random.Range(minGlitchDuration, maxGlitchDuration);
            float elapsedTime = 0f;

            while (elapsedTime < glitchDuration && isPlayerInside)
            {
                int randomIndex = Random.Range(0, glitchSprites.Length);
                spriteRenderer.sprite = glitchSprites[randomIndex];

                yield return new WaitForSeconds(glitchFrameRate);
                elapsedTime += glitchFrameRate;
            }

            spriteRenderer.sprite = originalSprite;
        }
    }
}
