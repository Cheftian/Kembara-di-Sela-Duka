using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class JigsawPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Snap Settings")]
    [SerializeField] private RectTransform targetSlot;
    [SerializeField] private float snapTolerance = 50f;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private JigsawMinigame parentMinigame;
    
    private Vector2 initialPosition;
    public bool IsLocked { get; private set; } = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Mencari Canvas teratas untuk perhitungan skala layar
        canvas = GetComponentInParent<Canvas>();
        parentMinigame = GetComponentInParent<JigsawMinigame>();
        
        initialPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        // Reset posisi jika minigame ditutup dan dibuka kembali sebelum selesai
        if (!IsLocked)
        {
            rectTransform.anchoredPosition = initialPosition;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsLocked) return;
        if (parentMinigame != null && !parentMinigame.CanPlay) return;

        // Memberikan efek visual transparan saat ditarik dan memindahkannya ke lapisan terdepan
        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling(); 
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsLocked) return;
        if (parentMinigame != null && !parentMinigame.CanPlay) return;

        // Memindahkan kepingan sesuai gerakan mouse, disesuaikan dengan resolusi layar
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsLocked) return;

        // Mengembalikan opasitas dan kemampuan deteksi klik
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        CheckSnapPosition();
    }

    private void CheckSnapPosition()
    {
        if (targetSlot == null) return;

        // Menghitung jarak antara posisi kepingan saat ini dengan slot target
        float distance = Vector2.Distance(rectTransform.anchoredPosition, targetSlot.anchoredPosition);
        
        if (distance <= snapTolerance)
        {
            SnapToTarget();
        }
    }

    private void SnapToTarget()
    {
        rectTransform.anchoredPosition = targetSlot.anchoredPosition;
        IsLocked = true;
        
        if (parentMinigame != null)
        {
            parentMinigame.CheckWinCondition();
        }
    }
}