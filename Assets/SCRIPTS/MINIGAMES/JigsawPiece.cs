using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class JigsawPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Snap Settings")]
    [SerializeField] private RectTransform targetSlot;
    [SerializeField] private float snapTolerance = 50f;

    [Header("Immersive Juiciness")]
    [SerializeField] private Image pieceImage;
    [SerializeField] private Color successColorFlash = Color.white;
    [SerializeField] private float shakeIntensity = 5f;
    [SerializeField] private float shakeDuration = 0.15f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private JigsawMinigame parentMinigame;
    
    private Vector2 initialPosition;
    private Vector2 dragOffset; // Menyimpan selisih jarak kursor dengan titik tengah objek
    public bool IsLocked { get; private set; } = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        parentMinigame = GetComponentInParent<JigsawMinigame>();
        
        if (pieceImage == null) pieceImage = GetComponent<Image>();
        initialPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        if (!IsLocked)
        {
            rectTransform.anchoredPosition = initialPosition;
        }
    }

    // Fungsi pembantu untuk JigsawMinigame menyimpan posisi setelah diacak di awal
    public void SetInitialPosition(Vector2 pos)
    {
        initialPosition = pos;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsLocked) return;
        if (parentMinigame != null && !parentMinigame.CanPlay) return;

        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;
        
        // Paksa piece ke layer paling depan saat mulai di-drag
        transform.SetAsLastSibling(); 

        // Menghitung offset agar objek tidak langsung "melompat" ke tengah kursor saat mulai di-drag
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            dragOffset = rectTransform.anchoredPosition - localPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsLocked) return;
        if (parentMinigame != null && !parentMinigame.CanPlay) return;

        // Pergerakan presisi mengikuti mouse di semua jenis resolusi UI Canvas
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsLocked) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        CheckSnapPosition();
    }

    private void CheckSnapPosition()
    {
        if (targetSlot == null) return;

        float distance = Vector2.Distance(rectTransform.anchoredPosition, targetSlot.anchoredPosition);
        
        if (distance <= snapTolerance)
        {
            SnapToTarget();
        }
        else
        {
            // SAAT DILETAKKAN DI MANAPUN (Gagal Snap):
            // Kita perbarui posisi awal (initialPosition) menjadi posisi jatuhnya yang baru.
            // Dengan begini, potongan akan menetap di sana dan tidak kembali ke posisi lama.
            initialPosition = rectTransform.anchoredPosition;
        }
    }

    private void SnapToTarget()
    {
        rectTransform.anchoredPosition = targetSlot.anchoredPosition;
        IsLocked = true;

        // Paksa piece ke layer paling belakang setelah sukses tertata
        transform.SetAsFirstSibling();
        
        // Jalankan efek visual kilatan dan guncangan
        StartCoroutine(PlaySuccessJuiceEffect());

        if (parentMinigame != null)
        {
            parentMinigame.CheckWinCondition();
        }
    }

    private IEnumerator PlaySuccessJuiceEffect()
    {
        Vector3 originalScale = transform.localScale;
        Color originalColor = pieceImage != null ? pieceImage.color : Color.white;

        // Efek Denyut & Kilatan Visual
        transform.localScale = originalScale * 1.2f;
        if (pieceImage != null) pieceImage.color = successColorFlash;

        float elapsed = 0f;
        Vector2 snapPos = rectTransform.anchoredPosition;

        // Efek Guncangan Layar Sederhana
        while (elapsed < shakeDuration)
        {
            float randomX = Random.Range(-shakeIntensity, shakeIntensity);
            float randomY = Random.Range(-shakeIntensity, shakeIntensity);
            
            rectTransform.anchoredPosition = snapPos + new Vector2(randomX, randomY);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Kembalikan ke posisi target presisi, skala, dan warna semula
        rectTransform.anchoredPosition = snapPos;
        transform.localScale = originalScale;
        if (pieceImage != null) pieceImage.color = originalColor;
    }
}
